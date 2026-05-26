#!/bin/bash
# ============================================
#  StockTV – Raspberry Pi Image Builder
#
#  Erstellt ein fertiges .img.xz zum Flashen
#  mit dem Raspberry Pi Imager.
#
#  Voraussetzungen (Ubuntu/Debian):
#    sudo apt-get install \
#      qemu-user-static binfmt-support \
#      kpartx parted e2fsprogs curl xz-utils
#
#  Aufruf (als root oder mit sudo):
#    sudo bash build-image.sh
#
#  Umgebungsvariablen:
#    STOCKTV_VERSION  – Versionsnummer (default: "dev")
#
#  Ausgabe:
#    stocktv-rpi-<version>.img.xz
# ============================================

set -euo pipefail

# ---- Konfiguration ----------------------------------------
PI_USER="pi"
PI_PASSWORD="stocktv"
HOSTNAME_NEW="stocktv"
SERVICE_NAME="stocktv"
APP_DIR="/opt/stocktv"
APP_BINARY="StockTvBlazor"
IMAGE_SIZE_GB=4
VERSION="${STOCKTV_VERSION:-dev}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PUBLISH_DIR="$SCRIPT_DIR/publish"
OUTPUT_IMAGE="$SCRIPT_DIR/stocktv-rpi-${VERSION}.img"
WORK_DIR="$(mktemp -d /tmp/stocktv-image-XXXXXX)"

# Farben
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'

# ---- Cleanup bei Exit -------------------------------------
LOOP_DEV=""
cleanup() {
    set +e
    echo -e "\n${YELLOW}Cleanup...${NC}"
    for fs in proc sys dev/pts dev boot/firmware boot; do
        mountpoint -q "$WORK_DIR/mnt/$fs" 2>/dev/null && umount -l "$WORK_DIR/mnt/$fs"
    done
    mountpoint -q "$WORK_DIR/mnt" 2>/dev/null && umount -l "$WORK_DIR/mnt"
    [ -n "$LOOP_DEV" ] && losetup -d "$LOOP_DEV" 2>/dev/null
    rm -rf "$WORK_DIR"
}
trap cleanup EXIT

# -----------------------------------------------------------
echo ""
echo "=========================================="
echo "  StockTV Raspberry Pi Image Builder"
echo "  Version: $VERSION"
echo "=========================================="
echo ""

# Root-Check
[ "$EUID" -ne 0 ] && { echo -e "${RED}Fehler: sudo erforderlich.${NC}"; exit 1; }

# Publish-Verzeichnis prüfen
[ ! -f "$PUBLISH_DIR/$APP_BINARY" ] && {
    echo -e "${RED}Fehler: $PUBLISH_DIR/$APP_BINARY nicht gefunden.${NC}"
    echo "Bitte zuerst publish-rpi.ps1 ausführen."
    exit 1
}

# Tools prüfen
for cmd in qemu-aarch64-static kpartx parted resize2fs losetup curl xz; do
    command -v "$cmd" &>/dev/null || {
        echo -e "${RED}Fehler: '$cmd' fehlt.${NC}"
        echo "  sudo apt-get install qemu-user-static binfmt-support kpartx parted e2fsprogs curl xz-utils"
        exit 1
    }
done

# ---- 1. Raspberry Pi OS Lite 64-bit herunterladen ---------
PIIMG_XZ="$WORK_DIR/raspios.img.xz"
echo "Lade Raspberry Pi OS Lite 64-bit (Bookworm) herunter..."
curl -L --progress-bar \
    -o "$PIIMG_XZ" \
    "https://downloads.raspberrypi.org/raspios_lite_arm64_latest"

echo "Entpacke Image..."
xz -d "$PIIMG_XZ"
PIIMG="$WORK_DIR/raspios.img"

# ---- 2. Image auf Zielgröße erweitern ---------------------
echo "Erweitere Image auf ${IMAGE_SIZE_GB}GB..."
truncate -s ${IMAGE_SIZE_GB}G "$PIIMG"

# ---- 3. Loop-Device einrichten ----------------------------
echo "Setze Loop-Device auf..."
LOOP_DEV=$(losetup --find --show --partscan "$PIIMG")
echo "Loop-Device: $LOOP_DEV"
sleep 1

# Partition 2 (rootfs) auf den gesamten freien Platz erweitern
parted -s "$PIIMG" resizepart 2 100%
partprobe "$LOOP_DEV" 2>/dev/null || true
sleep 1

e2fsck -f -y "${LOOP_DEV}p2" || true
resize2fs "${LOOP_DEV}p2"

# ---- 4. Partitionen mounten --------------------------------
MNT="$WORK_DIR/mnt"
mkdir -p "$MNT"
mount "${LOOP_DEV}p2" "$MNT"

# Bookworm: Boot-Partition liegt unter /boot/firmware
mkdir -p "$MNT/boot/firmware"
mount "${LOOP_DEV}p1" "$MNT/boot/firmware"

# Pseudo-Filesysteme
mount --bind /dev     "$MNT/dev"
mount --bind /dev/pts "$MNT/dev/pts"
mount --bind /sys     "$MNT/sys"
mount -t proc proc    "$MNT/proc"

# DNS für chroot
cp /etc/resolv.conf "$MNT/etc/resolv.conf"

# QEMU für ARM64-Emulation
cp /usr/bin/qemu-aarch64-static "$MNT/usr/bin/"

# ---- 5. App-Dateien ins Image kopieren ---------------------
echo "Kopiere StockTV App nach $APP_DIR..."
mkdir -p "$MNT$APP_DIR"
cp -r "$PUBLISH_DIR/"* "$MNT$APP_DIR/"
chmod +x "$MNT$APP_DIR/$APP_BINARY"
mkdir -p "$MNT$APP_DIR/_config" "$MNT$APP_DIR/_logs"
touch "$MNT$APP_DIR/.kiosk"

# ---- 6. Konfigurationsdateien schreiben -------------------

# systemd: StockTV App-Dienst
cat > "$MNT/etc/systemd/system/${SERVICE_NAME}.service" <<EOF
[Unit]
Description=StockTV Punkteanzeige
After=network-online.target
Wants=network-online.target

[Service]
WorkingDirectory=$APP_DIR
ExecStart=$APP_DIR/$APP_BINARY
Restart=always
RestartSec=5
TimeoutStopSec=15
User=$PI_USER
Environment=ASPNETCORE_URLS=http://+:8080
Environment=ASPNETCORE_ENVIRONMENT=Production
# PUBLIC_HOST=192.168.x.x einkommentieren bei mehreren Interfaces:
# Environment=PUBLIC_HOST=192.168.1.xx

[Install]
WantedBy=multi-user.target
EOF

# systemd: Auto-Login auf tty1 (X11 braucht einen eingeloggten User)
mkdir -p "$MNT/etc/systemd/system/getty@tty1.service.d"
cat > "$MNT/etc/systemd/system/getty@tty1.service.d/autologin.conf" <<EOF
[Service]
ExecStart=
ExecStart=-/sbin/agetty --autologin $PI_USER --noclear %I
EOF

# .bash_profile: X11 automatisch starten wenn auf tty1
cat > "$MNT/home/$PI_USER/.bash_profile" <<'BASHEOF'
[ -f ~/.bashrc ] && . ~/.bashrc
if [ -z "$DISPLAY" ] && [ "$(tty)" = "/dev/tty1" ]; then
    startx -- -nocursor 2>/tmp/xorg.log
fi
BASHEOF

# .xinitrc: Kiosk-Modus – Bildschirmschoner aus, Chromium im Vollbild
cat > "$MNT/home/$PI_USER/.xinitrc" <<'XINITEOF'
#!/bin/bash
# Bildschirmschoner und Energieverwaltung deaktivieren
xset -dpms
xset s off
xset s noblank

# Minimaler Windowmanager (openbox statt openbox-session: kein xdg-autostart, kein PyXDG noetig)
openbox &

# Chromium Singleton-Lock entfernen (verhindert Fehler nach unsauberem Shutdown)
rm -f ~/.config/chromium/Singleton*

# Warte bis StockTV HTTP-Anfragen beantwortet (max. 60 Sekunden)
for i in $(seq 1 30); do
    curl -s http://localhost:8080 >/dev/null 2>&1 && break
    sleep 2
done

# Chromium-Binary ermitteln (Paketname je nach Pi-OS-Version)
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
chmod +x "$MNT/home/$PI_USER/.xinitrc"

# Hostname
echo "$HOSTNAME_NEW" > "$MNT/etc/hostname"
sed -i "s/raspberrypi/$HOSTNAME_NEW/g" "$MNT/etc/hosts" 2>/dev/null || true

# SSH via sentinel-Datei auf Boot-Partition aktivieren
touch "$MNT/boot/firmware/ssh"

# firstrun.sh entfernen
rm -f "$MNT/boot/firmware/firstrun.sh"
sed -i 's| systemd.run=[^ ]*||g; s| systemd.run_success_action=[^ ]*||g' \
    "$MNT/boot/firmware/cmdline.txt" 2>/dev/null || true

# ---- 7. chroot: Pakete + System konfigurieren -------------
echo ""
echo "Konfiguriere System im chroot (dauert einige Minuten)..."
echo ""

chroot "$MNT" /usr/bin/qemu-aarch64-static /bin/bash <<CHROOT
set -e
export DEBIAN_FRONTEND=noninteractive

# pi-User anlegen oder korrigieren
# In Pi OS Bookworm existiert 'pi' bereits als System-Account mit nologin-Shell
if ! id -u $PI_USER &>/dev/null; then
    useradd -m -s /bin/bash \
        -G sudo,video,audio,input,dialout,plugdev,netdev \
        $PI_USER
else
    usermod -s /bin/bash \
        -G sudo,video,audio,input,dialout,plugdev,netdev \
        $PI_USER
fi
echo "${PI_USER}:${PI_PASSWORD}" | chpasswd

# Pakete installieren
apt-get update -qq
apt-get install -y --no-install-recommends \
    xserver-xorg \
    x11-xserver-utils \
    xinit \
    openbox \
    chromium \
    curl \
    libicu-dev \
    libssl3 \
    zlib1g

# Berechtigungen setzen
chown -R ${PI_USER}:${PI_USER} $APP_DIR
chown ${PI_USER}:${PI_USER} \
    /home/${PI_USER}/.bash_profile \
    /home/${PI_USER}/.xinitrc

# Tastaturlayout vorausfüllen (verhindert interaktiven Wizard beim ersten Boot)
cat > /etc/default/keyboard << 'KBEOF'
XKBMODEL="pc105"
XKBLAYOUT="de"
XKBVARIANT=""
XKBOPTIONS=""
BACKSPACE="guess"
KBEOF
DEBIAN_FRONTEND=noninteractive dpkg-reconfigure keyboard-configuration

# Konsolentastaturlayout setzen — verhindert systemd-firstboot --prompt-keymap
echo "KEYMAP=de" > /etc/vconsole.conf

# Console-Setup vorausfüllen
cat > /etc/default/console-setup << 'CSEOF'
ACTIVE_CONSOLES="/dev/tty[1-6]"
CHARMAP="UTF-8"
VIDEOMODE=
FONT=
FONTFACE=
FONTSIZE=
CODESET="Lat15"
CSEOF
DEBIAN_FRONTEND=noninteractive dpkg-reconfigure console-setup

# Cloud-init deaktivieren — sonst überschreibt es beim ersten Boot die autologin-Konfiguration
touch /etc/cloud/cloud-init.disabled

# Avahi deaktivieren — belegt Port 5353 und blockiert StockTV-mDNS
# ln -sf statt systemctl: systemctl wird im Chroot ignoriert
ln -sf /dev/null /etc/systemd/system/avahi-daemon.service
ln -sf /dev/null /etc/systemd/system/avahi-daemon.socket

# userconfig.service maskieren — verhindert Konflikt mit dem im Chroot angelegten pi-User
# (userconf.txt ist primäre Absicherung; Maske verhindert rename-user-Fehler)
ln -sf /dev/null /etc/systemd/system/userconfig.service

# StockTV-Dienst aktivieren — ln -sf statt systemctl (funktioniert im Chroot)
mkdir -p /etc/systemd/system/multi-user.target.wants
ln -sf /etc/systemd/system/${SERVICE_NAME}.service \
    /etc/systemd/system/multi-user.target.wants/${SERVICE_NAME}.service

# Autologin auf tty1 aktivieren — getty@tty1.service in getty.target.wants eintragen
mkdir -p /etc/systemd/system/getty.target.wants
ln -sf /usr/lib/systemd/system/getty@.service \
    /etc/systemd/system/getty.target.wants/getty@tty1.service

# APT-Cache bereinigen
apt-get clean
rm -rf /var/lib/apt/lists/*
CHROOT

# ---- 8. Cleanup im Image ----------------------------------
echo "Finalisiere Image..."
rm -f "$MNT/usr/bin/qemu-aarch64-static"
rm -f "$MNT/etc/resolv.conf"

# Unmount (in umgekehrter Reihenfolge)
umount "$MNT/proc"
umount "$MNT/dev/pts"
umount "$MNT/dev"
umount "$MNT/sys"
umount "$MNT/boot/firmware"
umount "$MNT"
losetup -d "$LOOP_DEV"
LOOP_DEV=""

# ---- 9. Fertiges Image kopieren und komprimieren ----------
echo "Kopiere Image..."
cp "$PIIMG" "$OUTPUT_IMAGE"

echo "Komprimiere mit xz (läuft parallel, dauert einige Minuten)..."
xz -T0 "$OUTPUT_IMAGE"

FINAL_IMAGE="${OUTPUT_IMAGE}.xz"

echo ""
echo "=========================================="
echo -e "  ${GREEN}Image erfolgreich erstellt!${NC}"
echo ""
echo "  Datei:    $FINAL_IMAGE"
echo "  Größe:    $(du -sh "$FINAL_IMAGE" | cut -f1)"
echo ""
echo "  ---- Zugangsdaten ----"
echo "  SSH-User:     $PI_USER"
echo "  SSH-Passwort: $PI_PASSWORD"
echo "  Hostname:     $HOSTNAME_NEW"
echo "  Web-UI:       http://<ip>:8080"
echo ""
echo "  Flashen mit Raspberry Pi Imager:"
echo "  'Use custom' → $FINAL_IMAGE wählen"
echo "=========================================="
