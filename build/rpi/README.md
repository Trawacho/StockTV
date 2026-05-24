# build/rpi — Raspberry Pi Build & Deploy

Dieser Ordner enthält alle Skripte, um StockTV für den Raspberry Pi zu bauen,
als fertiges Image bereitzustellen oder direkt auf einem Pi zu installieren.

---

## Dateien

### [publish-rpi.ps1](publish-rpi.ps1)

**PowerShell-Skript (Windows)** — Baut das Projekt für `linux-arm64` und erstellt
entweder ein Release-Zip oder deployt direkt auf einen laufenden Pi per SSH/SCP.

**Nur bauen (Zip für GitHub Release):**
```powershell
.\publish-rpi.ps1
```
Ausgabe: `build/rpi/publish/` + `build/rpi/stocktv-rpi.zip`

**Bauen + direkt auf Pi deployen:**
```powershell
.\publish-rpi.ps1 -PiHost 192.168.1.xx
```

**Erstinstallation** (legt systemd-Dienst an):
```powershell
.\publish-rpi.ps1 -PiHost 192.168.1.xx -Install
```

**Parameter:**

| Parameter      | Standard    | Beschreibung                          |
|----------------|-------------|---------------------------------------|
| `-PiHost`      | *(leer)*    | IP-Adresse des Pi. Ohne diesen Parameter wird nur gebaut. |
| `-PiUser`      | `pi`        | SSH-Benutzername                      |
| `-SudoPass`    | `stocktv`   | sudo-Passwort auf dem Pi              |
| `-RemoteDir`   | `/opt/stocktv` | Zielverzeichnis auf dem Pi         |
| `-ServiceName` | `stocktv`   | Name des systemd-Dienstes             |
| `-Install`     | *(Switch)*  | Erstinstallation: systemd-Dienst anlegen und aktivieren |

**Voraussetzungen:** .NET SDK installiert, SSH-Zugriff auf den Pi (per Key oder Passwort).

---

### [build-image.sh](build-image.sh)

**Bash-Skript (Linux/Ubuntu)** — Erstellt ein fertiges, flashbares Raspberry Pi OS Image
mit vorinstalliertem StockTV, Kiosk-Modus (Chromium + OpenBox) und aktiviertem SSH.
Wird normalerweise automatisch von GitHub Actions ausgeführt.

**Voraussetzungen (Ubuntu/Debian):**
```bash
sudo apt-get install qemu-user-static binfmt-support kpartx parted e2fsprogs curl xz-utils
```

**Aufruf (als root):**
```bash
# Zuerst publish-rpi.ps1 ausführen, damit build/rpi/publish/ befüllt ist
sudo bash build-image.sh
```

**Mit Versionsnummer:**
```bash
sudo STOCKTV_VERSION=v0.1 bash build-image.sh
```

**Ablauf:**
1. Lädt Raspberry Pi OS Lite 64-bit (Bookworm) herunter
2. Erweitert das Image auf 4 GB
3. Kopiert die StockTV-App nach `/opt/stocktv/`
4. Konfiguriert im chroot (QEMU ARM64-Emulation):
   - pi-User mit Passwort `stocktv`
   - X11, Chromium, OpenBox (Kiosk-Modus)
   - systemd-Dienst `stocktv` (autostart)
   - Auto-Login auf tty1 → startet X11 → startet Chromium auf `http://localhost:8080`
   - Deutsches Tastaturlayout vorbelegt
   - cloud-init deaktiviert (verhindert Setup-Wizard beim ersten Boot)
   - avahi-daemon deaktiviert (verhindert Konflikt mit StockTV-mDNS auf Port 5353)
   - SSH aktiviert
5. Komprimiert das Image mit `xz`

**Ausgabe:** `build/rpi/stocktv-rpi-<version>.img.xz`

**Zugangsdaten im Image:**
- SSH-User: `pi` / Passwort: `stocktv`
- Hostname: `stocktv`
- Web-UI nach dem Start: `http://<ip>:8080`

---

### [install.sh](install.sh)

**Bash-Skript** — Installiert oder aktualisiert StockTV auf einem bestehenden
Raspberry Pi OS (64-bit). Lädt automatisch das neueste Release von GitHub.

**Auf dem Pi ausführen:**
```bash
curl -sSL https://raw.githubusercontent.com/Trawacho/StockTV2/main/build/rpi/install.sh | bash
```

**Oder nach manuellem Download:**
```bash
chmod +x install.sh && ./install.sh
```

**Was das Skript macht:**
- Prüft ARM64-Architektur
- Installiert benötigte System-Bibliotheken (`libicu-dev`, `libssl3`, `zlib1g`)
- Lädt das neueste `stocktv-rpi.zip` von GitHub Releases herunter
- Kopiert die App nach `/opt/stocktv/`
- Legt beim ersten Aufruf den systemd-Dienst an und aktiviert ihn
- Startet den Dienst und prüft ob er läuft

**Eignet sich für:**
- Erstinstallation auf einem frischen Raspberry Pi OS
- Updates auf eine neue Version (Dienst wird kurz gestoppt, dann neu gestartet)

---

### [../../.github/workflows/release.yml](../../.github/workflows/release.yml)

**GitHub Actions Workflow** — Unified Release-Workflow für alle Plattformen.
Baut Windows x64, Linux x64 und das Raspberry Pi Image parallel und veröffentlicht
alles in einem einzigen GitHub Release.

**Auslöser:**

| Ereignis | Beschreibung |
|---|---|
| `git push v*` (Tag) | Automatischer Build aller Plattformen + GitHub Release |
| `workflow_dispatch` | Manueller Start — kein Release, nur Artefakte |

**Was der Workflow macht:**
1. Ermittelt die Versionsnummer aus dem Tag oder der manuellen Eingabe
2. Baut parallel: Windows x64, Linux x64, Raspberry Pi Image
3. Lädt alle Artefakte für 7 Tage hoch (auch ohne Release verfügbar)
4. Erstellt bei Tag-Pushes ein GitHub Release mit allen vier Dateien

**Tag setzen und Build anstoßen:**
```powershell
git tag v1.0.0
git push origin v1.0.0
```

**Manuell starten (ohne Release):**
GitHub → Repository → Actions → "Release" → "Run workflow"

---

## Typischer Workflow (Raspberry Pi)

```
Windows (Entwickler)          GitHub Actions (automatisch bei Tag)
─────────────────────         ──────────────────────────────────
publish-rpi.ps1               build-image.sh
      │                             │
      ▼                             ▼
  publish/           →        stocktv-rpi-vX.Y.img.xz
  stocktv-rpi.zip    →        GitHub Release
                                    │
                              Endnutzer flasht Image
                              → Pi startet sofort im Kiosk
                                    │
                              oder: install.sh auf
                              bestehendem Pi OS
```

Der vollständige Release-Prozess für alle Plattformen (Windows, Linux, Raspberry Pi)
ist in [CONTRIBUTING.md](../../CONTRIBUTING.md) beschrieben.
