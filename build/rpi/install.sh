#!/bin/bash
# ============================================
#  StockTV - Installation / Update Script
#  Raspberry Pi (arm64)
# ============================================
#
#  Aufruf:
#    curl -sSL https://raw.githubusercontent.com/Trawacho/StockTV2/main/build/install.sh | bash
#
#  Oder nach manuellem Download:
#    chmod +x install.sh && ./install.sh

set -e

REPO="Trawacho/StockTV2"
ASSET_NAME="stocktv-rpi.zip"
INSTALL_DIR="/opt/stocktv"
SERVICE_NAME="stocktv"

# Farben
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo ""
echo "=========================================="
echo "  StockTV Installation / Update"
echo "=========================================="
echo ""

# --- Root-Prüfung ---
if [ "$EUID" -eq 0 ]; then
    SUDO=""
else
    SUDO="sudo"
fi

# --- Architektur prüfen ---
ARCH=$(uname -m)
if [ "$ARCH" != "aarch64" ]; then
    echo -e "${RED}Fehler: Falsche Architektur '$ARCH'. Benoetigt wird aarch64 (64-Bit).${NC}"
    echo "Bitte Raspberry Pi OS 64-Bit installieren."
    exit 1
fi

# --- Tools prüfen ---
for cmd in curl unzip; do
    if ! command -v $cmd &>/dev/null; then
        echo -e "${YELLOW}Installiere $cmd...${NC}"
        $SUDO apt-get install -y $cmd -qq
    fi
done

# --- System-Bibliotheken fuer .NET (self-contained) ---
echo "Pruefe System-Bibliotheken..."
$SUDO apt-get install -y --no-install-recommends libicu-dev libssl3 zlib1g -qq

# --- Neueste Release-URL ermitteln ---
echo "Suche neueste Version auf GitHub..."

DOWNLOAD_URL=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" \
    | grep "browser_download_url" \
    | grep "$ASSET_NAME" \
    | cut -d '"' -f 4)

if [ -z "$DOWNLOAD_URL" ]; then
    echo -e "${RED}Fehler: Kein Release gefunden auf github.com/$REPO${NC}"
    echo "Bitte prüfen ob ein Release mit '$ASSET_NAME' existiert."
    exit 1
fi

VERSION=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" \
    | grep '"tag_name"' | cut -d '"' -f 4)

echo -e "Gefunden: ${GREEN}$VERSION${NC}"

# --- Temporaeres Verzeichnis ---
TMPDIR=$(mktemp -d)
trap "rm -rf $TMPDIR" EXIT

# --- Download ---
echo "Lade $ASSET_NAME herunter..."
curl -L --progress-bar -o "$TMPDIR/$ASSET_NAME" "$DOWNLOAD_URL"

# --- Entpacken ---
echo "Entpacke..."
unzip -q "$TMPDIR/$ASSET_NAME" -d "$TMPDIR/app"

# --- Erster Start oder Update? ---
FIRST_INSTALL=false
if [ ! -d "$INSTALL_DIR" ]; then
    FIRST_INSTALL=true
fi

# --- Laufenden Dienst stoppen ---
if systemctl is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
    echo "Stoppe laufenden Dienst..."
    $SUDO systemctl stop "$SERVICE_NAME"
fi

# --- Dateien installieren ---
echo "Installiere nach $INSTALL_DIR..."
$SUDO mkdir -p "$INSTALL_DIR"
$SUDO cp -r "$TMPDIR/app/"* "$INSTALL_DIR/"
$SUDO chmod +x "$INSTALL_DIR/StockTvBlazor"
$SUDO mkdir -p "$INSTALL_DIR/_config" "$INSTALL_DIR/_logs"

# Besitzer setzen (aktueller User oder pi)
APP_USER="${SUDO_USER:-$(whoami)}"
$SUDO chown -R "$APP_USER:$APP_USER" "$INSTALL_DIR"

# --- Systemd-Dienst installieren (nur beim ersten Mal) ---
if [ "$FIRST_INSTALL" = true ]; then
    echo "Installiere systemd-Dienst..."

    cat > "$TMPDIR/$SERVICE_NAME.service" <<EOF
[Unit]
Description=StockTV Punkteanzeige
After=network.target

[Service]
WorkingDirectory=$INSTALL_DIR
ExecStart=$INSTALL_DIR/StockTvBlazor
Restart=always
RestartSec=5
User=$APP_USER
Environment=ASPNETCORE_URLS=http://+:8080
Environment=ASPNETCORE_ENVIRONMENT=Production
# PUBLIC_HOST setzen, damit das zentrale System die richtige IP bekommt:
# Environment=PUBLIC_HOST=192.168.1.xx

[Install]
WantedBy=multi-user.target
EOF

    $SUDO cp "$TMPDIR/$SERVICE_NAME.service" "/etc/systemd/system/$SERVICE_NAME.service"
    $SUDO systemctl daemon-reload
    $SUDO systemctl enable "$SERVICE_NAME"

    echo ""
    echo -e "${YELLOW}Hinweis: Nur bei mehreren Netzwerk-Interfaces (WLAN + LAN)${NC}"
    echo "  falls das zentrale System den Pi nicht findet:"
    echo "  sudo nano /etc/systemd/system/$SERVICE_NAME.service"
    echo "  Zeile einkommentieren: Environment=PUBLIC_HOST=<IP-des-Pi>"
    echo "  Danach: sudo systemctl daemon-reload && sudo systemctl restart $SERVICE_NAME"
    echo ""
fi

# --- Dienst starten ---
$SUDO systemctl start "$SERVICE_NAME"

# --- Prüfen ob Dienst wirklich läuft ---
sleep 3
if ! systemctl is-active --quiet "$SERVICE_NAME"; then
    echo ""
    echo -e "${RED}Warnung: Dienst ist nicht aktiv. Letzte Log-Zeilen:${NC}"
    $SUDO journalctl -u "$SERVICE_NAME" --no-pager -n 20
    echo ""
    echo "Moegliche Ursache: fehlende System-Bibliothek."
    echo "Bitte pruefen: sudo journalctl -u $SERVICE_NAME -n 50"
    exit 1
fi

# --- IP ermitteln ---
PI_IP=$(hostname -I | awk '{print $1}')

echo ""
echo "=========================================="
if [ "$FIRST_INSTALL" = true ]; then
    echo -e "  ${GREEN}Installation abgeschlossen! ($VERSION)${NC}"
else
    echo -e "  ${GREEN}Update abgeschlossen! ($VERSION)${NC}"
fi
echo "  Web-UI: http://$PI_IP:8080"
echo "=========================================="
echo ""
