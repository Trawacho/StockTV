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

**Erstinstallation** (ZIP entpacken, dann im entpackten Verzeichnis ausführen):
```powershell
# Als Administrator:
.\install-service.ps1
```

**Mit eigenem Installationsverzeichnis:**
```powershell
.\install-service.ps1 -InstallDir "D:\StockTV" -Port 8080
```

**Dienst deinstallieren:**
```powershell
.\install-service.ps1 -Uninstall
```

| Parameter      | Standard     | Beschreibung |
|----------------|--------------|---|
| `-SourceDir`   | Skript-Verzeichnis | Quellverzeichnis mit den App-Dateien |
| `-InstallDir`  | `C:\StockTV` | Zielverzeichnis |
| `-ServiceName` | `StockTV`    | Name des Windows-Dienstes |
| `-Port`        | `8080`       | HTTP-Port |
| `-Uninstall`   | *(Switch)*   | Dienst stoppen und löschen |

**Voraussetzung:** PowerShell als Administrator.

---

## Manueller Installationsablauf (ohne WinRM)

1. `publish-windows.ps1` auf dem Entwicklungsrechner ausführen → erzeugt `stocktv-windows-x64.zip`
2. ZIP auf den Zielrechner kopieren und entpacken (z.B. nach `C:\StockTV-Install\`)
3. PowerShell als Administrator öffnen und ausführen:
   ```powershell
   cd C:\StockTV-Install
   .\install-service.ps1
   ```
4. Web-UI im Browser öffnen: `http://localhost:8080`

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
