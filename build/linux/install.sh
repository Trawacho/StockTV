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
#
#  Eigener bzw. zufaelliger Schluessel fuer die REST-Schnittstelle (Port 8098):
#    curl -sSL .../install.sh | bash -s -- --api-key "eigener-schluessel"
#    curl -sSL .../install.sh | bash -s -- --generate-api-key

set -e

REPO="Trawacho/StockTV"
ASSET_NAME="stocktv-linux-x64.zip"
INSTALL_DIR="/opt/stocktv"
SERVICE_NAME="stocktv"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# --- Parameter ---
# Schluessel der REST-Schnittstelle (Port 8098). Vorbelegt, damit alle Geraete gleich
# eingerichtet sind und niemand pro Geraet einen Wert nachhalten muss (wie bei StockTvKiosk).
# Der Preis dafuer: wer ihn von einem Geraet kennt, kommt auf alle. Er ist eine Huerde gegen
# versehentliche Zugriffe, kein Geraeteschutz. Pro Geraet aendern geht mit --generate-api-key,
# --api-key oder spaeter ueber POST /api/v1/config/api-key.
# Wird nur in eine NEUE Geraete-Konfiguration geschrieben - eine vorhandene behaelt ihren.
API_KEY="Secret4StockTV?"
GENERATE_API_KEY=false
while [ $# -gt 0 ]; do
    case "$1" in
        --api-key)          API_KEY="${2:?--api-key braucht einen Wert}"; shift 2 ;;
        --generate-api-key) GENERATE_API_KEY=true; shift ;;
        # Nicht abbrechen: ein Update ueber die /setup-Seite darf an einem Tippfehler nicht scheitern.
        *) echo -e "${YELLOW}Unbekannter Parameter ignoriert: $1${NC}"; shift ;;
    esac
done

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

# --- Geraete-Konfiguration mit API-Schluessel ---
# Ohne Schluessel startet die REST-Schnittstelle nicht (sie lauscht auf 0.0.0.0). Nur wenn es
# noch keine Datei gibt: eine vorhandene behaelt ihren Schluessel, auch einen ueber die
# Schnittstelle gewechselten.
DEVICE_CONFIG="$INSTALL_DIR/_config/stocktv.device.json"
LEGACY_CONFIG="$INSTALL_DIR/_config/stocktv.config.json"
API_KEY_WRITTEN=false
API_KEY_GENERATED=false
if [ -f "$DEVICE_CONFIG" ]; then
    echo "Geraete-Konfiguration vorhanden - der API-Schluessel bleibt unveraendert."
else
    if [ "$GENERATE_API_KEY" = true ] || [ -z "$API_KEY" ]; then
        API_KEY="$(head -c 24 /dev/urandom | base64)"
        API_KEY_GENERATED=true
    fi

    # Eine fruehere Einzeldatei teilt StockTV beim Start zwar selbst auf - aber nur, solange es
    # keine Geraetedatei gibt. Die hier angelegte verhindert das, deshalb die beiden
    # Geraete-Werte daraus selbst uebernehmen. Ohne jq: Leerraum entfernen und nach dem
    # abgeschalteten Wert suchen - beide Schluessel kommen in der Altdatei nur einmal vor.
    FILE_LOGGING=true
    NETWORK=true
    if [ -f "$LEGACY_CONFIG" ]; then
        LEGACY_FLAT=$(tr -d '[:space:]' < "$LEGACY_CONFIG")
        case "$LEGACY_FLAT" in *'"FileLoggingEnabled":false'*) FILE_LOGGING=false ;; esac
        case "$LEGACY_FLAT" in *'"Network":{"Enabled":false'*) NETWORK=false ;; esac
        echo "  Protokollierung und Netzwerk aus der bisherigen Konfiguration uebernommen."
    fi

    # Fuer JSON maskieren: Backslash und Anfuehrungszeichen.
    API_KEY_JSON="${API_KEY//\\/\\\\}"
    API_KEY_JSON="${API_KEY_JSON//\"/\\\"}"

    $SUDO tee "$DEVICE_CONFIG" >/dev/null <<DEVICEEOF
{
  "FileLoggingEnabled": $FILE_LOGGING,
  "Network": {
    "Enabled": $NETWORK
  },
  "RestApi": {
    "Enabled": true,
    "BindAddress": "0.0.0.0",
    "Port": 8098,
    "ApiKey": "$API_KEY_JSON",
    "ApiKeyHeader": "X-Api-Key",
    "SwaggerEnabled": true,
    "SwaggerRoute": "swagger"
  }
}
DEVICEEOF
    API_KEY_WRITTEN=true
    echo "Geraete-Konfiguration mit API-Schluessel angelegt: $DEVICE_CONFIG"
fi

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
echo "  REST:   http://$SERVER_IP:8098/swagger"
if [ "$API_KEY_WRITTEN" = true ]; then
    echo ""
    echo -e "  ${YELLOW}API-Schluessel: ${API_KEY}${NC}"
    if [ "$API_KEY_GENERATED" = true ]; then
        echo -e "  ${YELLOW}Jetzt notieren - nach dem ersten Speichern steht er nur noch${NC}"
        echo -e "  ${YELLOW}verschluesselt in der Datei und laesst sich nicht mehr ablesen.${NC}"
    else
        echo -e "  ${YELLOW}Das ist der vorbelegte Schluessel - auf allen so eingerichteten Geraeten${NC}"
        echo -e "  ${YELLOW}derselbe. Pro Geraet aendern:${NC}"
        echo "    curl -X POST -H \"X-Api-Key: ${API_KEY}\" -H \"Content-Type: application/json\" \\"
        echo "         -d '{\"newKey\":\"...\"}' http://localhost:8098/api/v1/config/api-key"
    fi
fi
echo "=========================================="
echo ""
