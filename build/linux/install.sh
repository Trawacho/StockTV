#!/bin/bash
# ============================================
#  StockTV - Installation / Update Script
#  Linux x64 (Ubuntu/Debian)
# ============================================
#
#  Aufruf:
#    curl -sSL https://raw.githubusercontent.com/Trawacho/StockTV/main/build/linux/install.sh | bash
#
#  Oder nach manuellem Download:
#    chmod +x install.sh && ./install.sh

set -e

REPO="Trawacho/StockTV"
ASSET_NAME="stocktv-linux-x64.zip"
INSTALL_DIR="/opt/stocktv"
SERVICE_NAME="stocktv"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo ""
echo "=========================================="
echo "  StockTV Installation / Update (Linux)"
echo "=========================================="
echo ""

# Root-Prüfung
if [ "$EUID" -eq 0 ]; then
    SUDO=""
else
    SUDO="sudo"
fi

# Architektur prüfen
ARCH=$(uname -m)
if [ "$ARCH" != "x86_64" ]; then
    echo -e "${RED}Fehler: Falsche Architektur '$ARCH'. Benoetigt wird x86_64.${NC}"
    echo "Fuer Raspberry Pi bitte stocktv-rpi.zip verwenden."
    exit 1
fi

# Tools prüfen
for cmd in curl unzip; do
    if ! command -v $cmd &>/dev/null; then
        echo -e "${YELLOW}Installiere $cmd...${NC}"
        $SUDO apt-get install -y $cmd -qq
    fi
done

# System-Bibliotheken für .NET (self-contained)
echo "Pruefe System-Bibliotheken..."
$SUDO apt-get install -y --no-install-recommends libicu-dev libssl3 zlib1g -qq

# Neueste Release-URL ermitteln
echo "Suche neueste Version auf GitHub..."

DOWNLOAD_URL=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" \
    | grep "browser_download_url" \
    | grep "$ASSET_NAME" \
    | cut -d '"' -f 4)

if [ -z "$DOWNLOAD_URL" ]; then
    echo -e "${RED}Fehler: Kein Release gefunden auf github.com/$REPO${NC}"
    echo "Bitte pruefen ob ein Release mit '$ASSET_NAME' existiert."
    exit 1
fi

VERSION=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" \
    | grep '"tag_name"' | cut -d '"' -f 4)

echo -e "Gefunden: ${GREEN}$VERSION${NC}"

TMPDIR=$(mktemp -d)
trap "rm -rf $TMPDIR" EXIT

echo "Lade $ASSET_NAME herunter..."
curl -L --progress-bar -o "$TMPDIR/$ASSET_NAME" "$DOWNLOAD_URL"

echo "Entpacke..."
unzip -q "$TMPDIR/$ASSET_NAME" -d "$TMPDIR/app"

FIRST_INSTALL=false
if [ ! -d "$INSTALL_DIR" ]; then
    FIRST_INSTALL=true
fi

# Laufenden Dienst stoppen
if systemctl is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
    echo "Stoppe laufenden Dienst..."
    $SUDO systemctl stop "$SERVICE_NAME"
fi

# Dateien installieren
echo "Installiere nach $INSTALL_DIR..."
$SUDO mkdir -p "$INSTALL_DIR"
$SUDO cp -r "$TMPDIR/app/"* "$INSTALL_DIR/"
$SUDO chmod +x "$INSTALL_DIR/StockTvBlazor"
$SUDO mkdir -p "$INSTALL_DIR/_config" "$INSTALL_DIR/_logs"

APP_USER="${SUDO_USER:-$(whoami)}"
$SUDO chown -R "$APP_USER:$APP_USER" "$INSTALL_DIR"

# Systemd-Dienst installieren (nur beim ersten Mal)
if [ "$FIRST_INSTALL" = true ]; then
    echo "Installiere systemd-Dienst..."

    cat > "$TMPDIR/$SERVICE_NAME.service" <<EOF
[Unit]
Description=StockTV Punkteanzeige
After=network-online.target
Wants=network-online.target

[Service]
WorkingDirectory=$INSTALL_DIR
ExecStart=$INSTALL_DIR/StockTvBlazor
Restart=always
RestartSec=5
TimeoutStopSec=15
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
fi

# Dienst starten
$SUDO systemctl start "$SERVICE_NAME"

sleep 3
if ! systemctl is-active --quiet "$SERVICE_NAME"; then
    echo ""
    echo -e "${RED}Warnung: Dienst ist nicht aktiv. Letzte Log-Zeilen:${NC}"
    $SUDO journalctl -u "$SERVICE_NAME" --no-pager -n 20
    exit 1
fi

SERVER_IP=$(hostname -I | awk '{print $1}')

echo ""
echo "=========================================="
if [ "$FIRST_INSTALL" = true ]; then
    echo -e "  ${GREEN}Installation abgeschlossen! ($VERSION)${NC}"
else
    echo -e "  ${GREEN}Update abgeschlossen! ($VERSION)${NC}"
fi
echo "  Web-UI: http://$SERVER_IP:8080"
echo "=========================================="
echo ""
