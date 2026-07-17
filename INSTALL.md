# StockTV – Installation

Installationsanleitung für Raspberry Pi, Windows und Docker.

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
- [Docker](#docker)
  - [Voraussetzungen (Docker)](#voraussetzungen-docker)
  - [Installation mit Docker Compose](#installation-mit-docker-compose)
  - [Mehrere Bahnen auf einem Rechner](#mehrere-bahnen-auf-einem-rechner)
  - [Update (Docker)](#update-docker)
  - [Hinweise (Docker)](#hinweise-docker)
- [Konfiguration nach der Installation](#konfiguration-nach-der-installation)
  - [Bahnnummer einstellen](#bahnnummer-einstellen)
  - [Netzwerk-Empfehlungen](#netzwerk-empfehlungen)
  - [Verwendung von Tablets](#verwendung-von-tablets)
  - [Einstellung der Themes](#einstellung-der-themes)

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

**Feste IP-Adresse einrichten** (empfohlen für stabilen Betrieb) — mit `nmtui`:

```bash
sudo nmtui
```

1. **Edit a connection** auswählen
2. Die kabelgebundene Verbindung auswählen (z. B. *Wired connection 1*) → **Edit**
3. Bei **IPv4 CONFIGURATION** von `Automatic` auf `Manual` umstellen → **Show**
4. **Addresses** (z. B. `192.168.1.xx/24`) und **Gateway** (z. B. `192.168.1.1`) eintragen
5. Bei **DNS servers** zwei Einträge hinzufügen — das Standardgateway und einen öffentlichen DNS-Server als Fallback, z. B. `192.168.1.1` und `1.1.1.1`
6. **OK** → **Back**
7. **Activate a connection** auswählen, die Verbindung deaktivieren und wieder aktivieren (oder Pi neu starten), damit die Änderung wirkt
8. **Quit**

> Eine laufende SSH-Verbindung kann dabei kurz getrennt werden.

**Hostnamen anpassen** — ebenfalls mit `nmtui`:

```bash
sudo nmtui
```

1. **Set system hostname** auswählen
2. Neuen Hostnamen eingeben (z. B. `stocktv-bahn1`) → **OK**
3. **Quit**
4. Pi neu starten, damit die Änderung überall (z. B. mDNS) greift:

```bash
sudo reboot
```

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

## Docker

StockTV läuft auch als Docker-Container (`linux/amd64` und `linux/arm64`).

Das Image ist veröffentlicht unter:

```text
ghcr.io/trawacho/stocktv:latest
```

Alternativ kann eine feste Version verwendet werden, z.B. `ghcr.io/trawacho/stocktv:v1.7.0`
(siehe [GitHub Releases](https://github.com/Trawacho/StockTV/releases)).

### Voraussetzungen (Docker)

- Docker Engine (mit Compose-Plugin) auf einem Linux-Host — `macvlan` (siehe unten) braucht
  direkten Zugriff auf eine physische Netzwerkkarte des Hosts. Auf Docker Desktop
  (Windows/Mac) funktioniert `macvlan` nur eingeschränkt bzw. gar nicht, dort stattdessen
  klassisches Port-Mapping (`ports:` statt `networks:` im Beispiel unten) verwenden.
- Kein GitHub-Login nötig — das Image ist öffentlich auf GitHub Container Registry (GHCR)

### Installation mit Docker Compose

Statt einzelne Ports zu veröffentlichen (`-p ...`), bekommt der Container über ein
**`macvlan`-Netzwerk** eine eigene IP-Adresse im LAN — wie ein eigenständiges Gerät.
Dadurch sind automatisch alle Ports (`8080`, `4747`, `4748` und mDNS auf `5353`) direkt
unter dieser IP erreichbar, ohne sie einzeln zu mappen.

1. Docker-Netzwerk einmalig anlegen:

   ```bash
   docker network create -d macvlan \
     --subnet=192.168.1.0/24 \
     --gateway=192.168.1.1 \
     -o parent=eth0 \
     stocktv_lan
   ```

   `eth0` durch die Netzwerkkarte des Docker-Hosts ersetzen (z.B. `eth0`, `enp2s0`, `ens18` —
   mit `ip link` ermitteln), `subnet`/`gateway` an das eigene Netz anpassen.

2. Verzeichnis anlegen und `docker-compose.yml` mit folgendem Inhalt erstellen:

   ```yaml
   networks:
     stocktv_lan:
       external: true

   services:
     stocktv:
       image: ghcr.io/trawacho/stocktv:latest
       container_name: StockTVBahn1
       hostname: StockTVBahn1
       networks:
         stocktv_lan:
           ipv4_address: 192.168.1.50
       environment:
         - ASPNETCORE_ENVIRONMENT=Production
         - ASPNETCORE_URLS=http://+:8080
       volumes:
         - ./_config:/app/_config
         - ./_logs:/app/_logs
       restart: always
   ```

   `ipv4_address` auf eine freie, feste IP im Subnetz setzen (außerhalb des DHCP-Bereichs
   des Routers, damit sie nicht doppelt vergeben wird).

3. Container starten:

   ```bash
   docker compose up -d
   ```

4. Web-UI im Browser öffnen: `http://192.168.1.50:8080`

**Alternative ohne Compose** (`docker run`, Netzwerk vorher wie oben mit `docker network create` anlegen):

```bash
docker run -d --name StockTVBahn1 --hostname StockTVBahn1 \
  --network stocktv_lan --ip 192.168.1.50 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v $(pwd)/_config:/app/_config \
  -v $(pwd)/_logs:/app/_logs \
  --restart always \
  ghcr.io/trawacho/stocktv:latest
```

### Mehrere Bahnen auf einem Rechner

Statt eines eigenen PCs pro Bahn kann auch **ein einziger Rechner** (Mini-PC, NAS, Server im
Vereinsheim …) alle Bahnen versorgen — pro Bahn läuft dort einfach ein weiterer Container mit
eigener `macvlan`-IP. Das ändert aber, wie Eingabe und Anzeige vor Ort funktionieren müssen:

- **Eingabe zwingend per Tablet.** Da an der Bahn selbst kein PC mit Ziffernblock mehr steht,
  muss die Eingabe über [ein Tablet](#verwendung-von-tablets) erfolgen. Jedes Tablet ruft dazu
  die `/input`-Seite unter der IP *seines* Bahn-Containers auf, z.B.
  `http://192.168.1.51:8080/input` für Bahn 1, `http://192.168.1.52:8080/input` für Bahn 2 usw.
- **Anzeige über Smart-TV statt Monitor am PC.** Auf jeder Bahn zeigt stattdessen ein
  Smart-TV (oder ein kleiner Player wie Fire TV Stick, Chromecast mit Google TV o.ä.) die
  Punkteanzeige — mit einer Browser-App im Kiosk-/Vollbild-Modus, die auf
  `http://<IP-der-Bahn>:8080` zeigt (analog zum Kiosk-Modus bei Raspberry Pi/Windows). Wie ein
  Kiosk-Browser auf dem jeweiligen Smart-TV eingerichtet wird, hängt stark vom Hersteller/Modell
  ab (z.B. eine Kiosk-Browser-App aus dem TV-App-Store oder eine Business-/Signage-Funktion im
  TV-Menü) — dafür gibt es keine einheitliche Anleitung.

**Beispiel `docker-compose.yml` für 4 Bahnen auf einem Host:**

```yaml
networks:
  stocktv_lan:
    external: true

services:
  stocktvBahn1:
    image: ghcr.io/trawacho/stocktv:latest
    container_name: StockTVBahn1
    hostname: StockTVBahn1
    networks:
      stocktv_lan:
        ipv4_address: 192.168.1.51
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    volumes:
      - ./bahn1/_config:/app/_config
      - ./bahn1/_logs:/app/_logs
    restart: always

  stocktvBahn2:
    image: ghcr.io/trawacho/stocktv:latest
    container_name: StockTVBahn2
    hostname: StockTVBahn2
    networks:
      stocktv_lan:
        ipv4_address: 192.168.1.52
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    volumes:
      - ./bahn2/_config:/app/_config
      - ./bahn2/_logs:/app/_logs
    restart: always

  # stocktvBahn3, stocktvBahn4 analog mit eigener ipv4_address und eigenem Volume-Verzeichnis
```

Wichtig pro Bahn: eigener Service-Block, eigene `ipv4_address`, eigener
`container_name`/`hostname` und ein eigenes `_config`/`_logs`-Verzeichnis — sonst teilen sich
mehrere Bahnen dieselbe Konfiguration und überschreiben sich gegenseitig. Nach
`docker compose up -d` laufen alle Container parallel auf dem Host; die Bahnnummer wird
anschließend für jeden Container einzeln über die Einstellungsseite gesetzt, siehe
[Bahnnummer einstellen](#bahnnummer-einstellen).

### Update (Docker)

```bash
docker compose pull
docker compose up -d
```

Bei `docker run` entsprechend `docker pull ghcr.io/trawacho/stocktv:latest` gefolgt von
einem Neuerstellen des Containers.

### Hinweise (Docker)

- **Eigene IP statt Port-Mapping:** Da der Container per `macvlan` eine eigene IP im LAN
  bekommt, entfällt eine `ports:`-Sektion komplett — alle Ports sind unter dieser IP genauso
  erreichbar wie bei einer nativen Installation.
- **Host erreicht Container ggf. nicht direkt:** Durch eine Docker-Eigenheit von `macvlan`
  kann der Docker-Host selbst standardmäßig nicht mit dem Container kommunizieren (andere
  Geräte im Netzwerk schon). Zum Testen der Web-UI ein anderes Gerät im Netzwerk verwenden,
  oder bei Bedarf ein zusätzliches macvlan-Shim-Interface auf dem Host einrichten.
- **Pro Spielbahn ein Container:** Läuft ein einzelner Host für alle Bahnen, siehe
  [Mehrere Bahnen auf einem Rechner](#mehrere-bahnen-auf-einem-rechner) — dort auch die
  Hinweise zu Tablet-Eingabe und Smart-TV-Anzeige.
- **Volumes:** `_config` und `_logs` unbedingt als Volumes mounten, sonst gehen Einstellungen
  und Log-Dateien beim Neuerstellen des Containers verloren.
- **Bahnnummer:** Wird wie bei den anderen Plattformen über die Einstellungsseite in der
  App gesetzt, siehe [Konfiguration nach der Installation](#konfiguration-nach-der-installation).

---

## Konfiguration nach der Installation

### Bahnnummer einstellen

Die Bahnnummer wird über die Einstellungsseite der App gesetzt.  
Einstellungsseite öffnen: Auf der Punkteanzeige **5× Enter drücken** (bei Punktestand 0).

### Netzwerk-Empfehlungen

**Nur Ethernet verwenden.** Es wird davon abgeraten, die StockTV PCs per WLAN zu verbinden. Aus Stabilitätsgründen ist die Verbindung mit Netzwerkkabel klar zu bevorzugen. Verbindet also die StockTV PCs per Netzwerkkabel mit einem Switch oder Router (z.B. FritzBox).
Wenn WLAN an dem Router aktiv ist, kann dies für die Tablets gerne verwendet werden. Stellt aber sicher, dass ihr eine sehr gute WLAN-Verfügbarkeit habt.

### Verwendung von Tablets

Ab der Version v1.7.0 kann zur Eingabe auch ein Tablet verwendet werden (anstatt Ziffernblock). Hierzu verwendet man auf dem Tablet am besten eine Kiosk-App, die einen Browser in Vollbild ohne Adresszeile oder sonstiges zeigt. Diese kann man meist absichern, sodass Anwender die App nicht aus Versehen beenden oder verlassen.
Als URL muss dann die IP-Adresse von StockTV auf der genutzten Bahn genutzt werden, z.B. `http://192.168.1.50:8080/input`.

### Einstellung der Themes

Ab der Version v1.7.0 können die Farben für die Darstellung selbst angepasst werden. Es können eigene Themes erstellt und wahlweise als aktives Theme ausgewählt werden — anstelle der beiden mitgelieferten Standard-Themes „Hell" und „Dunkel", die weiterhin als Basis für ein eigenes Theme dienen können.
Nutzt dafür einen Rechner, der im selben Netzwerk wie die StockTV-PCs ist, und öffnet im Browser die Themes-Seite, z.B. `http://192.168.1.50:8080/themes`.
