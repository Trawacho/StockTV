# build/windows — Windows Build & Deploy

Skripte zum Bauen und Installieren von StockTV als **Windows Service** auf einem Windows-Rechner (x64).

---

## Dateien

### [publish-windows.ps1](publish-windows.ps1)

**PowerShell-Skript (Windows)** — Baut das Projekt für `win-x64` und erstellt ein Release-Zip oder deployt direkt per WinRM (PowerShell Remoting).

**Nur bauen:**
```powershell
.\publish-windows.ps1
```
Ausgabe: `build/windows/publish/` + `build/windows/stocktv-windows-x64.zip`

**Bauen + direkt deployen (WinRM muss auf Zielrechner aktiv sein):**
```powershell
.\publish-windows.ps1 -TargetHost 192.168.1.xx
```

**Erstinstallation (inkl. Service-Anlage):**
```powershell
.\publish-windows.ps1 -TargetHost 192.168.1.xx -Install
```

| Parameter      | Standard        | Beschreibung |
|----------------|-----------------|---|
| `-TargetHost`  | *(leer)*        | Ziel-IP/-Hostname. Ohne diesen Parameter wird nur gebaut. |
| `-TargetUser`  | *(leer)*        | Windows-Benutzername auf dem Zielrechner (optional) |
| `-InstallDir`  | `C:\StockTV`    | Installationsverzeichnis |
| `-ServiceName` | `StockTV`       | Name des Windows-Dienstes |
| `-Install`     | *(Switch)*      | Erstinstallation: Windows-Dienst anlegen |

---

### [install-service.ps1](install-service.ps1)

**Lokales PowerShell-Skript** — Läuft direkt auf dem Zielrechner (als Administrator). Installiert oder aktualisiert StockTV als Windows Service.

**Neueste Version automatisch von GitHub laden und installieren:**
```powershell
# Als Administrator:
.\install-service.ps1 -Download
```

**Erstinstallation aus lokal entpacktem ZIP:**
```powershell
# Als Administrator (im entpackten Verzeichnis):
.\install-service.ps1
```

**Mit eigenem Installationsverzeichnis:**
```powershell
.\install-service.ps1 -Download -InstallDir "D:\StockTV" -Port 8080
```

**Dienst deinstallieren:**
```powershell
.\install-service.ps1 -Uninstall
```

| Parameter      | Standard     | Beschreibung |
|----------------|--------------|---|
| `-Download`    | *(Switch)*   | Neueste Version automatisch von GitHub Releases laden |
| `-Kiosk`          | *(Switch)*      | Kiosk-Modus aktivieren: Browser startet bei Anmeldung automatisch |
| `-KioskUser`      | `stocktv-kiosk` | Name des lokalen Kiosk-Benutzers |
| `-KioskPassword`  | `stocktv`       | Passwort des Kiosk-Benutzers |
| `-SourceDir`   | Skript-Verzeichnis | Quellverzeichnis mit den App-Dateien (ohne `-Download`) |
| `-InstallDir`  | `C:\StockTV` | Zielverzeichnis |
| `-ServiceName` | `StockTV`    | Name des Windows-Dienstes |
| `-Port`        | `8080`       | HTTP-Port |
| `-Uninstall`   | *(Switch)*   | Dienst + Kiosk-Task stoppen und löschen |

**Voraussetzung:** PowerShell als Administrator.

---

## Installationsablauf

### Variante A — Automatisch von GitHub (empfohlen)

Nur `install-service.ps1` auf den Zielrechner kopieren, dann als Administrator:

```powershell
.\install-service.ps1 -Download
```

Mit Kiosk-Modus:

```powershell
.\install-service.ps1 -Download -Kiosk
```

Das Skript lädt automatisch das neueste `stocktv-windows-x64.zip` von GitHub Releases,
entpackt es in ein temporäres Verzeichnis und installiert den Windows-Dienst.

### Variante B — Manuelles ZIP (ohne Internetzugang)

1. `stocktv-windows-x64.zip` von [GitHub Releases](https://github.com/Trawacho/StockTV2/releases) herunterladen
2. ZIP auf den Zielrechner kopieren und entpacken (z.B. nach `C:\StockTV-Install\`)
3. PowerShell als Administrator öffnen und ausführen:
   ```powershell
   cd C:\StockTV-Install
   .\install-service.ps1
   ```
4. Web-UI im Browser öffnen: `http://localhost:8080`

### Variante C — Direkt vom Entwicklungsrechner (WinRM)

```powershell
.\publish-windows.ps1 -TargetHost 192.168.1.xx -Install
```

---

## Kiosk-Modus

Mit `-Kiosk` öffnet Edge oder Chrome bei jeder Benutzeranmeldung automatisch im Vollbild ohne
jede Browserleiste (`--kiosk`). Ideal für TV-Displays, auf denen kein manueller Start notwendig sein soll.

**Was der Kiosk-Modus einrichtet:**

- Lokaler Windows-Benutzer `stocktv-kiosk` (Passwort wird beim Setup abgefragt)
- Autologin in der Registry — der Kiosk-Benutzer meldet sich nach jedem Reboot automatisch an
- `C:\StockTV\start-kiosk.ps1` — wartet bis der StockTV-Dienst erreichbar ist, startet dann den Browser
- `C:\StockTV\kiosk-profile\` — Browser-Profil mit deaktivierten Übersetzungs- und Benachrichtigungs-Dialogen
- Windows Scheduled Task `StockTV Kiosk` — führt das Skript bei Anmeldung jedes Benutzers aus (Gruppe `Users`)
- Sentinel-Datei `C:\StockTV\.kiosk` — merkt sich, dass Kiosk aktiv ist; bei Updates wird er automatisch neu eingerichtet

**Unterstützte Browser** (in dieser Reihenfolge gesucht): Microsoft Edge, Google Chrome.

**Kiosk nachträglich aktivieren:**

```powershell
.\install-service.ps1 -Kiosk
```

**Kiosk deaktivieren** (ohne Dienst zu entfernen): Task manuell löschen und `.kiosk` entfernen, oder:

```powershell
.\install-service.ps1 -Uninstall
.\install-service.ps1 -Download    # neu installieren ohne -Kiosk
```

---

## Dienstverwaltung

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

---

## Hinweise

- StockTV läuft auf Port **8080** (HTTP). HTTPS ist nicht konfiguriert.
- Konfiguration liegt in `C:\StockTV\_config\stocktv.config.json`.
- Der Dienst startet automatisch mit Windows.
- `UseWindowsService()` ist in `Program.cs` aktiv — auf Nicht-Windows-Systemen oder bei direktem Start als Konsolen-App hat das keinen Einfluss.
