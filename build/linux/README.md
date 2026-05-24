# build/linux — Linux x64 Build & Deploy

Skripte zum Bauen und Installieren von StockTV auf einem **Linux x64-System** (Ubuntu/Debian).

Für Raspberry Pi bitte den Ordner [../rpi/](../rpi/) verwenden.

---

## Dateien

### [publish-linux.ps1](publish-linux.ps1)

**PowerShell-Skript (Windows)** — Baut das Projekt für `linux-x64` und erstellt ein Release-Zip oder deployt direkt per SSH/SCP.

**Nur bauen:**
```powershell
.\publish-linux.ps1
```
Ausgabe: `build/linux/publish/` + `build/linux/stocktv-linux-x64.zip`

**Bauen + direkt deployen:**
```powershell
.\publish-linux.ps1 -TargetHost 192.168.1.xx -TargetUser meinuser
```

**Erstinstallation (inkl. systemd-Dienst):**
```powershell
.\publish-linux.ps1 -TargetHost 192.168.1.xx -TargetUser meinuser -Install
```

| Parameter      | Standard     | Beschreibung |
|----------------|--------------|---|
| `-TargetHost`  | *(leer)*     | IP/Hostname des Zielservers |
| `-TargetUser`  | `stocktv`    | SSH-Benutzername |
| `-SudoPass`    | *(leer)*     | sudo-Passwort (optional, wenn nicht per Key-Auth) |
| `-RemoteDir`   | `/opt/stocktv` | Zielverzeichnis |
| `-ServiceName` | `stocktv`    | Name des systemd-Dienstes |
| `-Install`     | *(Switch)*   | Erstinstallation: systemd-Dienst anlegen |

---

### [install.sh](install.sh)

**Bash-Skript** — Installiert oder aktualisiert StockTV auf einem Linux x64-System.
Lädt automatisch das neueste Release von GitHub.

**Auf dem Zielserver ausführen:**
```bash
curl -sSL https://raw.githubusercontent.com/Trawacho/StockTV2/main/build/linux/install.sh | bash
```

**Oder nach manuellem Download:**
```bash
chmod +x install.sh && ./install.sh
```

**Was das Skript macht:**
- Prüft x86_64-Architektur
- Installiert benötigte System-Bibliotheken (`libicu-dev`, `libssl3`, `zlib1g`)
- Lädt `stocktv-linux-x64.zip` vom neuesten GitHub Release
- Installiert nach `/opt/stocktv/`
- Legt beim ersten Aufruf den systemd-Dienst an und aktiviert ihn
- Startet den Dienst und prüft ob er läuft

---

## Dienstverwaltung

```bash
# Status
systemctl status stocktv

# Starten / Stoppen / Neu starten
sudo systemctl start stocktv
sudo systemctl stop stocktv
sudo systemctl restart stocktv

# Logs in Echtzeit
journalctl -u stocktv -f

# Datei-Logs
ls /opt/stocktv/_logs/
```

---

## Systemvoraussetzungen

| Anforderung | Details |
|---|---|
| Architektur | x86_64 (64-bit) |
| Betriebssystem | Ubuntu 22.04+ / Debian 11+ |
| Pakete | `libicu-dev`, `libssl3`, `zlib1g` (werden von `install.sh` automatisch installiert) |
| Port | 8080 (HTTP, eingehend freigeben) |

---

## Hinweise

- Konfiguration liegt in `/opt/stocktv/_config/stocktv.config.json`
- Logs unter `/opt/stocktv/_logs/`
- Der Dienst startet automatisch nach einem Neustart
- Für mehrere Netzwerk-Interfaces (z.B. WLAN + LAN): `Environment=PUBLIC_HOST=<gewünschte-IP>` in der Service-Datei eintragen
