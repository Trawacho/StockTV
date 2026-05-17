# StockTV – Installation auf dem Raspberry Pi

## Voraussetzungen

- Raspberry Pi 3, 4 oder 5 mit **64-Bit Betriebssystem** (Raspberry Pi OS 64-bit)
- Internetverbindung am Pi
- SSH-Zugang zum Pi (oder direkt Tastatur/Monitor)

> **32-Bit OS wird nicht unterstützt.** Bei einem älteren Image prüfen:  
> `uname -m` → muss `aarch64` ausgeben (nicht `armv7l`)

---

## Installation (einmaliger Erstaufruf)

SSH-Verbindung zum Pi herstellen, dann folgenden Befehl ausführen:

```bash
curl -sSL https://raw.githubusercontent.com/Trawacho/StockTV2/main/build/rpi/install.sh | bash
```

Das Script:
- lädt automatisch die neueste Version herunter
- installiert die App unter `/opt/stocktv/`
- richtet einen Systemdienst ein, der beim Start automatisch mitläuft
- startet die App sofort

Nach der Installation ist die Web-Oberfläche erreichbar unter:  
**http://\<IP-des-Pi\>:8080**

---

## Konfiguration nach der Installation

### Bahnnummer einstellen

Die Bahnnummer wird über die Einstellungsseite der App gesetzt.  
Einstellungsseite öffnen: Auf der Punkteanzeige **5× Enter drücken** (bei Punktestand 0).

### Netzwerk-Erkennung

Das zentrale Verwaltungsprogramm findet den Pi automatisch per mDNS – keine zusätzliche Konfiguration nötig.

> **Nur bei mehreren aktiven Netzwerk-Interfaces** (z.B. WLAN und LAN gleichzeitig) kann es vorkommen, dass die falsche IP gemeldet wird.
>
> **Empfehlung: WLAN deaktivieren** wenn der Pi per LAN-Kabel angeschlossen ist:
> ```bash
> sudo raspi-config
> ```
> → *System Options* → *Wireless LAN* → SSID leer lassen und bestätigen
>
> Alternativ dauerhaft per Konfigurationsdatei:
> ```bash
> echo "dtoverlay=disable-wifi" | sudo tee -a /boot/firmware/config.txt
> sudo reboot
> ```
>
> Falls trotzdem nötig, kann die IP manuell eingetragen werden:
> ```bash
> sudo nano /etc/systemd/system/stocktv.service
> ```
> Zeile einkommentieren und IP eintragen:
> ```
> Environment=PUBLIC_HOST=192.168.1.xx
> ```
> Danach: `sudo systemctl daemon-reload && sudo systemctl restart stocktv`

---

## Browser automatisch starten (Kiosk-Modus)

Damit der Pi beim Start die Punkteanzeige automatisch im Vollbild öffnet, wird Chromium im Kiosk-Modus als Autostart eingerichtet.

> Voraussetzung: **Raspberry Pi OS mit Desktop** (nicht Lite).  
> Der StockTV-Dienst startet automatisch über systemd — der Browser wartet auf ihn.

### 1. Automatische Anmeldung aktivieren

```bash
sudo raspi-config
```
→ *System Options* → *Boot / Auto Login* → **Desktop Autologin**

### 2. Wrapper-Script erstellen

Das Script wartet bis StockTV bereit ist, dann öffnet es Chromium:

```bash
mkdir -p ~/bin
cat > ~/bin/stocktv-kiosk.sh << 'EOF'
#!/bin/bash
# Bildschirm nicht abschalten
xset s off
xset -dpms
xset s noblank

# Warten bis StockTV erreichbar ist
until curl -s http://localhost:8080 > /dev/null 2>&1; do
    sleep 1
done

# Chromium im Kiosk-Modus starten
chromium-browser \
    --kiosk \
    --noerrdialogs \
    --disable-infobars \
    --disable-session-crashed-bubble \
    http://localhost:8080
EOF
chmod +x ~/bin/stocktv-kiosk.sh
```

### 3. Autostart einrichten

```bash
mkdir -p ~/.config/autostart
cat > ~/.config/autostart/stocktv-kiosk.desktop << 'EOF'
[Desktop Entry]
Type=Application
Name=StockTV Kiosk
Exec=/home/pi/bin/stocktv-kiosk.sh
EOF
```

> Falls der Benutzername nicht `pi` ist, den Pfad `/home/pi/bin/...` entsprechend anpassen.

Nach einem Neustart öffnet sich Chromium automatisch mit der Punkteanzeige:

```bash
sudo reboot
```

---

## Update auf neue Version

Derselbe Befehl wie bei der Installation – das Script erkennt automatisch ob es ein Update ist:

```bash
curl -sSL https://raw.githubusercontent.com/Trawacho/StockTV2/main/build/rpi/install.sh | bash
```

---

## Nützliche Befehle

```bash
# Status des Dienstes prüfen
sudo systemctl status stocktv

# App neu starten
sudo systemctl restart stocktv

# App stoppen
sudo systemctl stop stocktv

# Protokoll (Logs) anzeigen
sudo journalctl -u stocktv -f

# App-Logs anzeigen (detaillierter)
ls /opt/stocktv/_logs/
```

---

## Fehlerbehebung

**Web-UI nicht erreichbar:**
```bash
sudo systemctl status stocktv
sudo journalctl -u stocktv --no-pager -n 50
```

**Falsches Betriebssystem (32-Bit):**  
Raspberry Pi OS 64-Bit neu installieren (Raspberry Pi Imager → OS wählen → „Raspberry Pi OS (64-bit)").

**Port 8080 belegt:**  
```bash
sudo ss -tlnp | grep 8080
```
