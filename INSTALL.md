# StockTV – Installation

Installationsanleitung für Raspberry Pi und Windows.

## Inhaltsverzeichnis

- [Raspberry Pi](#raspberry-pi)
  - [Methode 1: Fertiges Image flashen](#methode-1-fertiges-image-flashen)
  - [Methode 2: Install-Script auf bestehendem Raspberry Pi OS Lite 64-bit](#methode-2-install-script-auf-bestehendem-raspberry-pi-os-lite-64-bit)
  - [Netzwerkkonfiguration (Raspberry Pi)](#netzwerkkonfiguration)
  - [Nützliche Befehle (Raspberry Pi)](#nützliche-befehle)
  - [Fehlerbehebung (Raspberry Pi)](#fehlerbehebung)
- [Windows](#windows)
  - [Methode 1: Automatisch von GitHub (empfohlen)](#methode-1-automatisch-von-github-empfohlen)
  - [Methode 2: Manuelles ZIP (ohne Internetzugang)](#methode-2-manuelles-zip-ohne-internetzugang)
  - [Kiosk-Modus (Windows)](#kiosk-modus-windows)
  - [Dienstverwaltung (Windows)](#dienstverwaltung-windows)
  - [Hinweise (Windows)](#hinweise-windows)
- [Konfiguration nach der Installation](#konfiguration-nach-der-installation)

---

## Raspberry Pi

Es gibt zwei Installationswege:

| | [Methode 1: Fertiges Image](#methode-1-fertiges-image-flashen) | [Methode 2: Install-Script](#methode-2-install-script-auf-bestehendem-raspberry-pi-os-lite-64-bit) |
|---|---|---|
| Voraussetzung | Leere SD-Karte | Laufendes Raspberry Pi OS Lite 64-bit |
| Kiosk-Modus | Immer enthalten | Optional (Script fragt nach) |
| Aufwand | Minimal | Minimal |

---

### Methode 1: Fertiges Image flashen

Das einfachste Vorgehen — Pi ist nach dem ersten Start sofort einsatzbereit.

#### 1. Image herunterladen

Das neueste Image gibt es unter:  
**https://github.com/Trawacho/StockTV/releases/latest**

Datei: `stocktv-rpi-vX.Y.img.xz`

#### 2. Image flashen

Mit dem [Raspberry Pi Imager](https://www.raspberrypi.com/software/):

1. **Operating System** → *Use custom* → heruntergeladene `.img.xz` auswählen
2. **Storage** → SD-Karte auswählen
3. Auf *Write* klicken — fertig.

> Keine weiteren Einstellungen im Imager nötig (kein SSH, kein WLAN konfigurieren —
> das Image hat SSH bereits aktiviert).

#### 3. Starten

SD-Karte in den Pi einlegen, Monitor anschließen, Strom anlegen.

Nach ca. 30–60 Sekunden öffnet sich Chromium automatisch mit der Punkteanzeige.

**Zugangsdaten:**

| | |
|---|---|
| SSH-User | `pi` |
| SSH-Passwort | `stocktv` |
| Hostname | `stocktv` |
| Web-UI | `http://<IP-des-Pi>:8080` |

---

### Methode 2: Install-Script auf bestehendem Raspberry Pi OS Lite 64-bit

Für einen Pi, auf dem bereits Raspberry Pi OS Lite 64-bit läuft.

#### Voraussetzungen

- **Raspberry Pi OS Lite 64-bit** (empfohlen — kein Desktop nötig, das Script installiert bei Kiosk-Aktivierung alle benötigten X11-Pakete selbst)
- Raspberry Pi Imager → *Raspberry Pi OS (other)* → *Raspberry Pi OS Lite (64-bit)*
- `uname -m` muss `aarch64` ausgeben (nicht `armv7l`)
- Internetverbindung am Pi
- SSH-Zugang oder Tastatur/Monitor

#### Installation

SSH-Verbindung herstellen und folgenden Befehl ausführen:

```bash
curl -sSL https://raw.githubusercontent.com/Trawacho/StockTV/main/build/rpi/install.sh | bash
```

Das Script fragt interaktiv:

```
Kiosk-Modus aktivieren (Autologin + Chromium auf diesem Geraet)? [j/N]
```

**Mit Kiosk (`j`):** Autologin auf tty1, Chromium startet nach dem Reboot automatisch im Vollbild.  
**Ohne Kiosk (`N`):** Nur der Hintergrunddienst wird eingerichtet, Web-UI unter `http://<IP>:8080`.

Das Script:
- lädt automatisch die neueste Version von GitHub herunter
- installiert die App unter `/opt/stocktv/`
- richtet einen systemd-Dienst ein, der beim Start automatisch mitläuft
- deaktiviert avahi-daemon (würde StockTV-mDNS auf Port 5353 blockieren)
- richtet bei Bedarf Autologin, X11 und Chromium-Kiosk ein
- startet die App sofort

#### Update

Derselbe Befehl — das Script erkennt automatisch ob es ein Update ist:

```bash
curl -sSL https://raw.githubusercontent.com/Trawacho/StockTV/main/build/rpi/install.sh | bash
```

Wurde der Kiosk-Modus beim ersten Install aktiviert, wird er beim Update automatisch
geprüft und bei Bedarf korrigiert (keine erneute Rückfrage).  
Wurde er nicht aktiviert, wird erneut gefragt (Nachholung möglich).

### Netzwerkkonfiguration

**WLAN dauerhaft deaktivieren** (empfohlen, siehe [Netzwerk-Empfehlungen](#netzwerk-empfehlungen)):

```bash
echo "dtoverlay=disable-wifi" | sudo tee -a /boot/firmware/config.txt
sudo reboot
```

**Feste IP-Adresse einrichten** (empfohlen für stabilen Betrieb):

```bash
# Vorhandene Verbindungen anzeigen (Verbindungsname notieren, z.B. "Wired connection 1")
nmcli con show

# Feste IP setzen — Verbindungsname, IP und Gateway anpassen
sudo nmcli con mod "Wired connection 1" \
    ipv4.method manual \
    ipv4.addresses "192.168.1.xx/24" \
    ipv4.gateway "192.168.1.1" \
    ipv4.dns "192.168.1.1"
sudo nmcli con up "Wired connection 1"
```

> Die Änderung wirkt sofort — eine laufende SSH-Verbindung trennt sich dabei.

### Nützliche Befehle

```bash
# Status des Dienstes prüfen
sudo systemctl status stocktv

# App neu starten
sudo systemctl restart stocktv

# Protokoll (Logs) live anzeigen
sudo journalctl -u stocktv -f

# App-Logs anzeigen
ls /opt/stocktv/_logs/
```

### Fehlerbehebung

**Web-UI nicht erreichbar:**
```bash
sudo systemctl status stocktv
sudo journalctl -u stocktv --no-pager -n 50
```

**Kiosk startet nicht nach Reboot:**
```bash
systemctl status getty@tty1
cat /etc/systemd/system/getty@tty1.service.d/autologin.conf
# Falls fehlend: install.sh erneut ausführen, Kiosk mit "j" bestätigen
```

**Falsches Betriebssystem (32-Bit):**  
Raspberry Pi OS 64-Bit neu installieren: Raspberry Pi Imager → *Raspberry Pi OS (64-bit)*.

**Port 8080 belegt:**
```bash
sudo ss -tlnp | grep 8080
```

---

## Windows

StockTV läuft auf Windows (x64) als **Windows-Dienst**. Es gibt zwei Installationswege:

| | [Methode 1: Automatisch](#methode-1-automatisch-von-github-empfohlen) | [Methode 2: Manuelles ZIP](#methode-2-manuelles-zip-ohne-internetzugang) |
|---|---|---|
| Voraussetzung | Internetzugang am Zielrechner | GitHub-Release-ZIP |
| Kiosk-Modus | Optional (`-Kiosk`) | Optional (nachträglich) |
| Aufwand | Minimal | Minimal |

---

### Methode 1: Automatisch von GitHub (empfohlen)

1. [`install-service.ps1`](https://raw.githubusercontent.com/Trawacho/StockTV/main/build/windows/install-service.ps1) herunterladen (Rechtsklick auf den Link → *Ziel speichern unter…*) und auf den Zielrechner kopieren.
2. PowerShell als Administrator öffnen, in das Verzeichnis mit der heruntergeladenen Datei wechseln und ausführen:

```powershell
.\install-service.ps1 -Download
```

Mit Kiosk-Modus:

```powershell
.\install-service.ps1 -Download -Kiosk
```

Das Skript lädt automatisch das neueste `stocktv-windows-x64.zip` von GitHub Releases,
entpackt es in ein temporäres Verzeichnis und installiert den Windows-Dienst.

> Falls die **ExecutionPolicy** das Starten verhindert, einmalig so aufrufen:
> ```powershell
> powershell.exe -ExecutionPolicy Bypass -File .\install-service.ps1 -Download
> ```

### Methode 2: Manuelles ZIP (ohne Internetzugang)

1. `stocktv-windows-x64.zip` von [GitHub Releases](https://github.com/Trawacho/StockTV/releases) herunterladen
2. ZIP auf den Zielrechner kopieren und entpacken (z.B. nach `C:\StockTV-Install\`)
3. PowerShell als Administrator öffnen und ausführen:
   ```powershell
   cd C:\StockTV-Install
   .\install-service.ps1
   ```
4. Web-UI im Browser öffnen: `http://localhost:8080`

### Kiosk-Modus (Windows)

Mit `-Kiosk` öffnet Edge oder Chrome bei jeder Benutzeranmeldung automatisch im Vollbild
ohne jede Browserleiste (`--kiosk`). Ideal für TV-Displays, auf denen kein manueller Start
notwendig sein soll. Unterstützte Browser (in dieser Reihenfolge gesucht): Microsoft Edge, Google Chrome.

**Was der Kiosk-Modus einrichtet:**

- Lokaler Windows-Benutzer `stocktv-kiosk` (Passwort wird beim Setup abgefragt)
- Autologin in der Registry — der Kiosk-Benutzer meldet sich nach jedem Reboot automatisch an
- `C:\StockTV\start-kiosk.ps1` — wartet bis der StockTV-Dienst erreichbar ist, startet dann den Browser
- `C:\StockTV\kiosk-profile\` — Browser-Profil mit deaktivierten Übersetzungs- und Benachrichtigungs-Dialogen
- Windows Scheduled Task `StockTV Kiosk` — führt das Skript bei Anmeldung jedes Benutzers aus
- Sentinel-Datei `C:\StockTV\.kiosk` — merkt sich, dass Kiosk aktiv ist; bei Updates wird er automatisch neu eingerichtet

**Kiosk nachträglich aktivieren:**

```powershell
.\install-service.ps1 -Kiosk
```

**Kiosk deaktivieren** (ohne Dienst zu entfernen):

```powershell
.\install-service.ps1 -Uninstall
.\install-service.ps1 -Download    # neu installieren ohne -Kiosk
```

### Dienstverwaltung (Windows)

```powershell
# Status anzeigen
Get-Service StockTV

# Starten / Stoppen / Neu starten
Start-Service StockTV
Stop-Service StockTV
Restart-Service StockTV

# Logs (Windows Event Log)
Get-EventLog -LogName Application -Source StockTV -Newest 20

# Oder: Datei-Logs unter
# C:\StockTV\_logs\
```

Dienst deinstallieren:

```powershell
.\install-service.ps1 -Uninstall
```

### Hinweise (Windows)

- StockTV läuft auf Port **8080** (HTTP). HTTPS ist nicht konfiguriert.
- Konfiguration liegt in `C:\StockTV\_config\stocktv.config.json`.
- Der Dienst startet automatisch mit Windows (`UseWindowsService()` in `Program.cs`).

---

## Konfiguration nach der Installation

### Bahnnummer einstellen

Die Bahnnummer wird über die Einstellungsseite der App gesetzt.  
Einstellungsseite öffnen: Auf der Punkteanzeige **5× Enter drücken** (bei Punktestand 0).

### Netzwerk-Empfehlungen

Das zentrale Verwaltungsprogramm findet die Geräte automatisch per mDNS — keine zusätzliche Konfiguration nötig.

**Nur Ethernet-Kabel verwenden.** WLAN wird nicht empfohlen: Wenn WLAN und LAN gleichzeitig aktiv sind, kann die falsche IP gemeldet werden.
