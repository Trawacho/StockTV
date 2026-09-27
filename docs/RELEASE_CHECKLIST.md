# Release Checkliste — StockTV

Vor jedem Release durchführen: alle 3 Plattformen (RPi, Windows, Linux) + Docker.

---

## Vorbereitung

- [ ] Alle Tests grün: `dotnet test StockTvBlazor.Tests/` (31+ Tests)
- [ ] Keine Compiler-Warnungen: `dotnet build StockTvBlazor/StockTvBlazor.csproj`
- [ ] `develop` Branch ist aktuell und alle Commits sind gepusht
- [ ] Git-Tag vorbereitet: `v<VERSION>` (z.B. `v1.9.0`)
- [ ] Offline-Update Bytegrenzen validiert:
  - `build/rpi/publish-rpi.ps1` laufen lassen
  - Größe von `stocktv-rpi.zip` prüfen
  - `MaxOfflineUpdateUploadBytes` in `StockTvBlazor/Services/UpdateService.cs` ausreichend? (Sollte `> zip-size + OfflineUpdateSafetyMarginBytes` sein)
  - Falls zu knapp: `MaxOfflineUpdateUploadBytes` erhöhen + neuen Commit

---

## Manuelles Testen auf echten Geräten

### Raspberry Pi (Kiosk)

- [ ] **Image bauen & flashen:**
  - `build/rpi/publish-rpi.ps1` ausführen → `stocktv-rpi.zip`
  - Image auf microSD flashen (BalenaEtcher empfohlen)
  - Pi booten
- [ ] **Kiosk-Modus verifizieren:**
  - [ ] Chromium startet automatisch nach Boot
  - [ ] http://localhost:8080 ladet die App
  - [ ] Web-UI ist vollständig sichtbar (kein Fehler-Overlay)
  - [ ] Numpad (Tastatur oder physisch) sendet Eingaben
- [ ] **Spielmodi testen:**
  - [ ] Training: Eingabe → Punkte aktualisieren sich
  - [ ] BestOf: Mehrere Spiele, Matchpunkte berechnet
  - [ ] Ziel: Alle 4 Disziplinen, Phasen-Reihenfolge OK
  - [ ] Ziel2: Rundenwechsel nach MaxVersucheGesamt, GesamtSumme korrekt
- [ ] **Netzwerk-Konfiguration (/setup):**
  - [ ] Nur erreichbar wenn kein Match laufen (`GameStateGuard.HasRecordedValues`)
  - [ ] Hostname setzen & verifizieren: `hostname` command auf Pi
  - [ ] DHCP setzen & Adresse prüfen: `ip addr`
  - [ ] Static IP setzen (z.B. 192.168.1.50/24) & verifizieren
  - [ ] Reboot via Setup-Seite, Pi startet neu
- [ ] **mDNS-Discovery:**
  - [ ] Von anderem Rechner: `avahi-browse -r _stockTV._tcp` (Linux) oder Bonjour Browser (Windows/Mac)
  - [ ] Service sichtbar mit richtiger IP und Port (4747, 4748)
- [ ] **Offline-Update:**
  - [ ] Alte Version laufen lassen (z.B. v1.8.0 simulieren)
  - [ ] Neue `stocktv-rpi.zip` auf /setup hochladen
  - [ ] Update startet, Pi rebootet
  - [ ] Neue Version läuft (mDNS `pkgVer` zeigt neue Version)

### Windows (Service + Kiosk)

- [ ] **Installation:**
  - [ ] `build/windows/publish-windows.ps1` ausführen
  - [ ] ZIP entpacken auf Ziel-Rechner
  - [ ] `install-service.ps1 -Install -Kiosk` ausführen (admin)
  - [ ] Service startet: `Get-Service StockTV | Start-Service`
  - [ ] http://localhost:8080 lädt die App
- [ ] **Kiosk-Modus:**
  - [ ] Nach Reboot: lokal angemeldete kiosk-user-Session
  - [ ] Edge/Chrome startet im Kiosk-Modus (vollbildschirm)
  - [ ] App ist verfügbar
- [ ] **Service-Verwaltung:**
  - [ ] `net stop StockTV` stoppt den Service
  - [ ] `net start StockTV` startet ihn wieder
  - [ ] Logs in Event Viewer prüfen (keine kritischen Fehler)
- [ ] **Spielmodi testen:** (wie RPi oben)

### Linux x64 (systemd)

- [ ] **Installation:**
  - [ ] `build/linux/publish-linux.ps1` ausführen
  - [ ] ZIP auf Linux-Rechner kopieren & entpacken
  - [ ] `sudo bash install.sh` ausführen
  - [ ] `systemctl status stocktv` zeigt "active (running)"
  - [ ] http://localhost:8080 lädt die App
- [ ] **systemd-Integration:**
  - [ ] `sudo systemctl stop stocktv` stoppt Service
  - [ ] `sudo systemctl start stocktv` startet ihn
  - [ ] `sudo systemctl restart stocktv` rebootet
  - [ ] Logs: `sudo journalctl -u stocktv -f` zeigen keine Fehler
- [ ] **Spielmodi testen:** (wie RPi oben)

### Docker (multi-Bahn Szenario)

- [ ] **Multi-Container starten:**
  - [ ] `docker-compose up -d` mit 4 Services (stocktvBahn1-4)
  - [ ] `docker ps` zeigt 4 Container als "Up"
  - [ ] Jeder Port (8080, 8081, 8082, 8083) lädt sein Bahn-Dashboard
- [ ] **Network-Isolation (optional):**
  - [ ] Falls `macvlan` konfiguriert: jeder Container hat separate IP
  - [ ] mDNS Discovery funktioniert für alle 4 Services
- [ ] **Spielmodi testen:** (wie RPi oben, für mind. 1 Bahn)

---

## Zentrale Verwaltung (extern)

Falls ein zentrales Verwaltungssystem den StockTV kontrolliert:

- [ ] **NetMQ-Kommunikation (Port 4747/4748):**
  - [ ] `GetResult` beantwortet (Score + Match-State)
  - [ ] `SetTeamNames` setzt Teamnamen korrekt
  - [ ] `SetTeilnehmer` (Ziel-Mode) funktioniert
  - [ ] `ResetResult` setzt Match zurück
- [ ] **mDNS-Discovery:**
  - [ ] Zentrale findet alle Bahnen via `_stockTV._tcp`
  - [ ] `pkgVer` zeigt korrekte App-Version
  - [ ] `osVer` zeigt korrekte Plattform (RPi/Windows/Linux/Docker)

---

## Release-Durchführung

Nur wenn alle obigen Punkte grün sind:

- [ ] Tag erstellen: `git tag -a v<VERSION> -m "Release v<VERSION>"`
- [ ] Tag pushen: `git push origin v<VERSION>`
- [ ] GitHub Release automatisch erstellt (via Workflow)
- [ ] Release-Workflow prüfen (.github/workflows/release.yml):
  - [ ] `build-docker` abgeschlossen (GHCR Push)
  - [ ] `build-rpi`, `build-windows`, `build-linux` abgeschlossen
  - [ ] `release` Job erstellt GitHub Release mit Assets
- [ ] GitHub Release manuell prüfen:
  - [ ] 3 ZIP-Dateien vorhanden (RPi, Windows, Linux)
  - [ ] Docker Image `ghcr.io/trawacho/stocktv:<VERSION>` gepusht
  - [ ] Release-Notes plausibel

---

## Post-Release

- [ ] Alle 3 Plattformen aktualisiert (oder geplant)
- [ ] Zentrale hat neue Version erkannt (mDNS, INSTALL.md Doku aktualisiert)
- [ ] Roadmap für nächste Version aktualisiert

---

## Bekannte Probleme & Workarounds

### RPi Offline-Update schlägt fehl
- Symptom: ZIP-Upload erfolgreich, aber Update startet nicht
- Check: `sudo systemctl status stocktv-update` — Job noch läuft?
- Workaround: Pi manuell rebootet, alte Version bleibt. Setup-Seite neu versuchen.

### Windows Kiosk startet nicht nach Reboot
- Symptom: Service läuft, aber keine Kiosk-Session
- Check: Autologon-Registr-Key: `HKLM\Software\Microsoft\Windows NT\CurrentVersion\Winlogon`
- Fix: `install-service.ps1 -Kiosk` nochmal laufen

### mDNS Service nicht sichtbar
- Symptom: avahi-browse zeigt StockTV nicht
- Check: `sudo systemctl status avahi-daemon` läuft?
- Workaround: `sudo systemctl restart avahi-daemon`

---

## Dokumentation nach Release

- [ ] Versionshistorie aktualisieren (CHANGELOG.md, falls vorhanden)
- [ ] Neue Funktionen in der Benutzer-Doku (wenn relevant)
- [ ] Deployment-Anleitung prüfen (INSTALL.md noch aktuell?)

