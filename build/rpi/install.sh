#!/bin/bash
# ============================================
#  StockTV - Installation / Update Script
#  Raspberry Pi (arm64)
# ============================================
#
#  Aufruf:
#    curl -sSL https://raw.githubusercontent.com/Trawacho/StockTV2/main/build/rpi/install.sh | bash
#
#  Oder nach manuellem Download:
#    chmod +x install.sh && ./install.sh

set -e

REPO="Trawacho/StockTV2"
ASSET_NAME="stocktv-rpi.zip"
INSTALL_DIR="/opt/stocktv"
KIOSK_SENTINEL="$INSTALL_DIR/.kiosk"
SERVICE_NAME="stocktv"

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
    echo "Bitte pruefen ob ein Release mit '$ASSET_NAME' existiert."
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

# --- Kiosk-Modus: Entscheidung treffen ---
# Sentinel vorhanden → Kiosk war beim First-Install aktiviert → immer einrichten
# Sentinel fehlt + First-Install → fragen
# Sentinel fehlt + Update → fragen (Nachholung moeglich)
SETUP_KIOSK=false
if [ -f "$KIOSK_SENTINEL" ]; then
    SETUP_KIOSK=true
    if [ "$FIRST_INSTALL" = false ]; then
        echo -e "${YELLOW}Kiosk-Modus ist aktiviert — wird geprueft und ggf. korrigiert.${NC}"
    fi
else
    echo ""
    read -r -p "Kiosk-Modus aktivieren (Autologin + Chromium auf diesem Geraet)? [j/N] " KIOSK_ANSWER
    if [[ "$KIOSK_ANSWER" =~ ^[jJyY]$ ]]; then
        SETUP_KIOSK=true
    fi
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

APP_USER="${SUDO_USER:-$(whoami)}"
$SUDO chown -R "$APP_USER:$APP_USER" "$INSTALL_DIR"

# --- Systemd-Dienst installieren (nur beim ersten Mal) ---
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

    echo ""
    echo -e "${YELLOW}Hinweis: Nur bei mehreren Netzwerk-Interfaces (WLAN + LAN)${NC}"
    echo "  falls das zentrale System den Pi nicht findet:"
    echo "  sudo nano /etc/systemd/system/$SERVICE_NAME.service"
    echo "  Zeile einkommentieren: Environment=PUBLIC_HOST=<IP-des-Pi>"
    echo "  Danach: sudo systemctl daemon-reload && sudo systemctl restart $SERVICE_NAME"
    echo ""
fi

# --- Kiosk-Modus einrichten / pruefen ---
if [ "$SETUP_KIOSK" = true ]; then
    echo ""
    echo "Kiosk-Modus wird eingerichtet..."

    APP_HOME=$(eval echo "~$APP_USER")

    # Pakete installieren (idempotent)
    $SUDO apt-get install -y --no-install-recommends \
        xserver-xorg x11-xserver-utils xinit openbox chromium -qq

    # Autologin-Drop-in
    AUTOLOGIN_DIR="/etc/systemd/system/getty@tty1.service.d"
    $SUDO mkdir -p "$AUTOLOGIN_DIR"
    $SUDO tee "$AUTOLOGIN_DIR/autologin.conf" > /dev/null <<EOF
[Service]
ExecStart=
ExecStart=-/sbin/agetty --autologin $APP_USER --noclear %I
EOF

    # getty@tty1 in getty.target.wants aktivieren
    $SUDO mkdir -p /etc/systemd/system/getty.target.wants
    $SUDO ln -sf /usr/lib/systemd/system/getty@.service \
        /etc/systemd/system/getty.target.wants/getty@tty1.service

    # .bash_profile: X11 automatisch starten wenn auf tty1
    $SUDO tee "$APP_HOME/.bash_profile" > /dev/null <<'BASHEOF'
[ -f ~/.bashrc ] && . ~/.bashrc
if [ -z "$DISPLAY" ] && [ "$(tty)" = "/dev/tty1" ]; then
    startx -- -nocursor 2>/tmp/xorg.log
fi
BASHEOF

    # .xinitrc: Kiosk-Modus
    $SUDO tee "$APP_HOME/.xinitrc" > /dev/null <<'XINITEOF'
#!/bin/bash
xset -dpms
xset s off
xset s noblank

openbox &

rm -f ~/.config/chromium/Singleton*

for i in $(seq 1 30); do
    curl -s http://localhost:8080 >/dev/null 2>&1 && break
    sleep 2
done

CHROMIUM=$(command -v chromium 2>/dev/null || command -v chromium-browser 2>/dev/null)

exec "$CHROMIUM" \
    --kiosk \
    --noerrdialogs \
    --disable-session-crashed-bubble \
    --disable-infobars \
    --disable-translate \
    --no-first-run \
    --disable-features=TranslateUI \
    --check-for-update-interval=31536000 \
    http://localhost:8080
XINITEOF

    $SUDO chmod +x "$APP_HOME/.xinitrc"
    $SUDO chown "$APP_USER:$APP_USER" "$APP_HOME/.bash_profile" "$APP_HOME/.xinitrc"

    # Sentinel anlegen
    $SUDO touch "$KIOSK_SENTINEL"
    $SUDO chown "$APP_USER:$APP_USER" "$KIOSK_SENTINEL"

    $SUDO systemctl daemon-reload

    echo -e "${GREEN}Kiosk-Modus eingerichtet.${NC}"
    echo -e "${YELLOW}Hinweis: Kiosk startet nach dem naechsten Reboot automatisch.${NC}"
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
