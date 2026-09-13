# StockTV E2E Tests — Dokumentation

**Status:** ✅ Phase 1–7, Phase 10 grün | 🚀 Phase 4–6 vollständig implementiert  
**Datum:** 2026-09-13  
**Framework:** xUnit + Playwright + NetMQ  
**Execution:** Sequenziell, ein AppFixture für alle Tests ([Collection("E2E Sequential")])

---

## 🚀 Schnelleinstieg

```powershell
cd C:\Users\daniel\source\repos\StockTV

# Alle Tests (11 Tests: Phase 1–7, Phase 10 grün)
dotnet test StockTvBlazor.E2ETests/

# Einzelne Phase
dotnet test StockTvBlazor.E2ETests/ --filter "Phase1"
dotnet test StockTvBlazor.E2ETests/ --filter "Phase7"
dotnet test StockTvBlazor.E2ETests/ --filter "Phase10"

# Mit Diagnostik
dotnet test StockTvBlazor.E2ETests/ -v diagnostic

# Tests mit Browser sichtbar (Headless deaktivieren)
$env:PLAYWRIGHT_HEADLESS = "false"
dotnet test StockTvBlazor.E2ETests/
```

**💡 Headless Modus:** Standardmäßig laufen Tests im Headless-Modus (kein Browser-Fenster). Um den Browser zu sehen, setze vor dem Test:
```powershell
$env:PLAYWRIGHT_HEADLESS = "false"
```

**Erwartung:**
```
✓ Phase 1: Training 15 Kehren
✓ Phase 2: Turnier 3 Spiele
✓ Phase 3: BestOf 3 Spiele
✓ Phase 4: Ziel 6 Kehren (6 Versuche pro Disziplin)
✓ Phase 5: Ziel 12 Kehren (12 Versuche pro Disziplin)
✓ Phase 6: Ziel2 2 Runden (6+6 Versuche pro Disziplin, automatischer Wechsel)
✓ Phase 7: Settings Navigation
✓ Phase 10: Settings Persistence (2 Tests)

Bestanden: 11, Übersprungen: 0, Dauer: ~6–8 Min
```

---

## ⚙️ Setup

### Voraussetzungen
- ✅ .NET 10.0 SDK
- ✅ Playwright Chromium (in `bin/Debug/net10.0/`)
- ✅ Ports frei: 5001 (HTTP), 4747/4748 (NetMQ)

### Initial Setup
```powershell
cd C:\Users\daniel\source\repos\StockTV
dotnet restore
cd StockTvBlazor.E2ETests\bin\Debug\net10.0
.\playwright.ps1 install
cd ..\..\..
dotnet test StockTvBlazor.E2ETests/
```

---

## 📋 Detaillierte Test-Beschreibungen

### ✅ Phase 1: Training — 15 Kehren, max 9 Punkte/Kehre

**Test:** `Phase1_Training_15Kehren()`

**Konfiguration:**
- Modus: 0 (Training)
- MaxPunkteProKehre: 9
- MaxKehrenProSpiel: 15

**Ablauf:**
1. Navigiert zu `/training`
2. Sendet `ResetResult` via NetMQ
3. **13 gültige Kehren:** Zufällige Werte 0–9, jede mit Bestätigung (+)
4. **Zufälliger Reset:** Bei Kehre 3–12 wird `-` gedrückt (letzte löschen) und erneut mit `+` bestätigt
5. **2 zusätzliche Kehren:** Weitere zufällige Werte

**Validierung nach jedem Turn:**
- Display-Punkte Links/Rechts korrekt aktualisiert
- Kehren-Zähler inkrementiert
- Delete/Reset funktioniert

**Logging:**
```
[12:34:56.789] Phase 1 | ✓ Kehre 1/15: 7 Punkte (Links)
[12:34:57.123] Phase 1 | ✓ Kehre 2/15: 5 Punkte (Rechts)
...
[12:35:15.456] Phase 1 | ✓ Debugounce für Settings nach 1100ms
```

---

### ✅ Phase 2: Turnier — 3 Spiele, 6 Punkte/Kehre, 6 Kehren/Spiel

**Test:** `Phase2_Turnier_3Spiele()`

**Konfiguration:**
- Modus: 2 (Turnier)
- MaxPunkteProKehre: 6
- MaxKehrenProSpiel: 6
- Richtung: 1 (Rechts = grün)

**Vorbereitung:**
- NetMQ `SetTeamNames`: `"1:TeamA1:TeamB1;2:TeamA2:TeamB2;3:TeamA3:TeamB3"`

**Ablauf pro Spiel (3x):**
1. Team-Namen validieren (z.B. "TeamA1" vs "TeamB1")
2. **6 Kehren** eingeben: Zufällige Werte 0–6
   - Taste `+` drücken nach jeder Kehre (außer vorletzter/letzter)
3. Kehren-Zähler müssen passen
4. Spiel-Navigation funktioniert

**Validierung:**
- Team-Namen beim Spiel-Start sichtbar
- Punkte-Anzeige aktualisiert sich nach jedem Input
- Spiel 1 → Spiel 2 → Spiel 3 Progression

**Beispiel-Log:**
```
[12:35:20.000] Phase 2 | ✓ Team-Namen korrekt: TeamA1 vs TeamB1
[12:35:21.050] Phase 2 | ✓ Kehre 1/6: 4 Punkte (Rechts)
[12:35:22.100] Phase 2 | ✓ Kehre 2/6: 2 Punkte (Links)
```

---

### ✅ Phase 3: BestOf — 3 Spiele, 8 Punkte/Kehre, 6 Kehren/Spiel

**Test:** `Phase3_BestOf_3Spiele()`

**Konfiguration:**
- Modus: 1 (BestOf)
- MaxPunkteProKehre: 8
- MaxKehrenProSpiel: 6
- Richtung: 1 (Rechts = grün)

**Vorbereitung:**
- NetMQ `SetTeamNames`: `"1:TeamA1:TeamB1;2:TeamA2:TeamB2;3:TeamA3:TeamB3"`

**Ablauf pro Spiel (3x):**
1. Team-Namen validieren
2. **6 Kehren** eingeben: Zufällige Werte 0–8
   - Taste `+` IMMER vor jedem Turn drücken (außer 1./letzte) — sollte keine Auswirkung haben
3. Spiel-Punkte nach jeder Kehre validieren
4. Match-Points tracking (wer gewinnt Spiel 1/2/3)

**Validierung:**
- Taste `+` vor Turn verursacht keinen Reset
- Spiel-Punkte kumulieren korrekt
- Match-Punkte nach Spiel-Ende aktualisiert (z.B. "1:0" → "1:1" → "2:1")

**Match-Punkte-Logik:**
- Wer mehr Punkte nach 6 Kehren → +1 Match-Punkt

---

### ✅ Phase 4: Ziel — 6 Kehren pro Disziplin

**Test:** `Phase4_Ziel_6Kehren()`

**Konfiguration:**
- Modus: 100 (Ziel)
- MaxKehrenProSpiel: 6

**Ablauf:**
- 4 Disziplinen der Reihe nach:
  1. **MassenVorne** — gültig: 0, 2, 4, 6, 8, 10
  2. **Schiessen** — gültig: 0, 2, 5, 10
  3. **MassenSeite** — gültig: 0, 2, 4, 6, 8, 10
  4. **Kombinieren** — gültig: 0, 2, 4, 6, 8, 10

- Pro Disziplin **6 Versuche** eingeben:
  - Versuche 1-4: zufällig gültige Werte
  - Versuch 5: ungültiger Wert (zeigt 1,5s "ungültig" overlay)
  - Versuch 6: zufällig gültiger Wert
  
- Optionally: Taste `-` zum Löschen, dann Ersatzwert

**Validierung:**
- Invalid overlay wird angezeigt und verschwindet nach 1,5s
- Disziplin-Übergänge funktionieren automatisch
- Finale Seite lädt erfolgreich

**Dauer:** ~30–40s

---

### ✅ Phase 5: Ziel — 12 Kehren pro Disziplin

**Test:** `Phase5_Ziel_12Kehren()`

**Konfiguration:**
- Modus: 100 (Ziel)
- MaxKehrenProSpiel: 12

**Ablauf:**
- Wie Phase 4, aber pro Disziplin **12 Versuche** statt 6
- Versuche 1-10: zufällig gültige Werte
- Versuch 11: ungültiger Wert → "ungültig" overlay
- Versuche 12: zufällig gültiger Wert
- Längere Interaktion mit besseren Timeouts konfiguriert

**Validierung:**
- Ungültiger Versuch löst korrekt ungültig-Overlay aus
- UI bleibt responsive über alle 48 Versuche hinweg

**Dauer:** ~60–80s

---

### ✅ Phase 6: Ziel2 — 2 Runden à 4 Disziplinen × 6 Kehren

**Test:** `Phase6_Ziel2_2Runden()`

**Konfiguration:**
- Modus: 101 (Ziel2 — zwei Runden)
- MaxKehrenProSpiel: 6

**Ablauf:**
1. **Runde 1:** 4 Disziplinen × 6 Kehren = 24 Versuche
   - Nach 24 Versuchen: App speichert Runde-1-Summe, setzt Versuchslisten zurück
2. **Runde 2:** Weitere 4 Disziplinen × 6 Kehren = 24 Versuche
   - Display zeigt verdoppelte Versuchszahl (24–48 statt 0–24)
3. **GesamtSumme:** Automatisch Runde 1 + Runde 2

**Validierung:**
- Automatischer Übergang zwischen Runden
- Ungültige Versuche in beiden Runden funktionieren
- finale Seite mit Gesamtsumme korrekt

**Dauer:** ~60–80s

---

### ✅ Phase 7: Settings — Navigation über Enter-Taste

**Test:** `Phase7_Settings_Navigation()`

**Konfiguration:**
- Modus: 0 (Training)
- Startseite: `/training`

**Ablauf:**
1. Navigiert zu `/training`
2. **5x Enter drücken** (mit je 1100ms Debounce) → Settings-Seite sollte öffnen
3. Taste `2` drücken (Konfiguration ändern)
4. Taste `8` drücken
5. Taste `+` drücken → zurück zu `/training`

**Validierung:**
- Settings-Seite lädt nach Enter-Sequenz
- Inhalte sind vorhanden (nicht leer)
- Navigation zurück funktioniert
- Final URL ist nicht leer

---

### ✅ Phase 10: Settings Persistence — Debounce & Speicherung

**Tests:** 2 Szenarien unter Phase 10

#### Test 1: `Phase10_RapidSettingsChanges_OnlyLastPersisted()`

**Szenario:** Benutzer ändert Settings 3x schnell hintereinander → nur LETZTE wird persistiert

**Ablauf:**
1. Lade aktuelle Settings
2. **Sende 3 Settings-Änderungen mit je 50ms Verzögerung:**
   - 1. Modus=0 (Training), MaxPunkte=15, MaxKehren=30
   - 2. Modus=1 (BestOf), MaxPunkte=10, MaxKehren=6
   - 3. Modus=2 (Turnier), MaxPunkte=10, MaxKehren=6
3. Validiere dass nur **Änderung #3 (Turnier)** persistiert wurde

**Validierung:**
- `ValidateSettingsPersistenceAsync()` wartet bis alle 4 Settings-Werte stimmen
- Erwartet: Modus=2, MaxPunkteProKehre=10, MaxKehrenProSpiel=6
- Debounce-Timeout: 1000ms (siehe `DEBOUNCE_DELAY_MS = 1100` in PhaseTestBase)

**Logging:**
```
[12:36:00.000] Phase 10 | ✓ Ändere Settings zu Turnier (Modus=2)
[12:36:00.050] Phase 10 | ✓ Debounce funktioniert - nur letzte Änderung persistiert
```

#### Test 2: `Phase10_DebounceTimeout_SettingsPersisted()`

**Szenario:** Settings-Änderung wird nach Debounce-Timeout persistiert

**Ablauf:**
1. Lade aktuelle Settings
2. Ändere zu BestOf: Modus=1, MaxPunkte=10, MaxKehren=6
3. Warte auf Debounce-Timeout
4. Validiere dass die Änderung in der Datei `_config/stocktv.config.json` persistiert wurde

**Validierung:**
- Settings-Datei wird aktualisiert
- `ValidateSettingsPersistenceAsync()` wartet auf korrekte Werte (Modus, MaxPunkte, MaxKehren)
- Nach ~1100ms sollte die Persistierung abgeschlossen sein

---

## 🏗️ Zentrale Logging & Infrastruktur

### TestLogWriter — Duale Protokollierung

`Helpers/TestLogWriter.cs` schreibt Logs zu:
1. **xUnit ITestOutputHelper** — Terminal/Test-Output
2. **Datei** — `TestResults/e2e-test-{yyyyMMdd-HHmmss}.log`

**Log-Format:**
```
Log started at 2026-09-13T19:00:00.000...
Log file: C:\...\TestResults\e2e-test-20260913-190000.log
Random Seed für diesen Testlauf: 1234567890

[HH:mm:ss.fff] PHASE    | ✓ Nachricht
═══════════════════════════════════════════════════════
  Phase 1: Training 15 Kehren
═══════════════════════════════════════════════════════
✓ Phase erfolgreich beendet

═══════════════════════════════════════════════════════
  TEST SUMMARY
═══════════════════════════════════════════════════════
Phases:         7
Success Checks: 48
Warnings:       2
Duration:       127.34s
Log File:       C:/.../TestResults/e2e-test-20260912-123500.log
═══════════════════════════════════════════════════════
```

### PhaseTestBase — Gemeinsame Test-Basis

Alle Phase-Tests erben von `PhaseTestBase` mit:
- `Log(phase, message)` — Strukturiertes Logging
- `LogPhaseStart(phase, description)` — Phase-Header
- `LogPhaseEnd(phase)` — Phase-Footer
- `SendSettings(byte[] settingsBytes)` — NetMQ Settings senden
- `GetCurrentSettings()` — Aktuelle Settings abrufen
- `SendResetResult()` — Spiel zurücksetzen
- `DEBOUNCE_DELAY_MS = 1100` — Standard Debounce für Settings
- `Random Rng` — Zufällige Wert-Generierung mit globalem Seed (pro Testlauf identisch)

### AppFixture — App, Browser, NetMQ Management

`Fixtures/AppFixture.cs` verwaltet für alle Tests:
- **HTTP Server** (Port 5001) — startet `dotnet run` im Subprocess
- **NetMQ REP Socket** (Port 4747) — für Befehle
- **NetMQ PUB Socket** (Port 4748) — für Event-Broadcasts
- **Playwright Browser** (Chromium) — headless oder GUI
- **Logger Initialisierung** — je Test ein neuer TestLogWriter

**Wichtig:** AppFixture ist **shared across all tests** via `[Collection("E2E Sequential")]`

### Collection — Sequenzielle Ausführung

```csharp
[CollectionDefinition("E2E Sequential", DisableParallelization = true)]
public class E2ESequentialCollection : ICollectionFixture<AppFixture> { }
```

Sorgt dafür dass:
- ✅ Alle Phases sequenziell laufen (Phase 1 → Phase 2 → ...)
- ✅ Eine App-Instanz für alle Tests (kein Neustart pro Test)
- ✅ State bleibt erhalten zwischen Tests (falls nötig)

---

## 📊 Erwartete Zeiten

| Phase | Test-Name | Status | Dauer |
|-------|-----------|--------|-------|
| 1 | Training 15 Kehren | ✅ | ~30s |
| 2 | Turnier 3 Spiele | ✅ | ~40s |
| 3 | BestOf 3 Spiele | ✅ | ~40s |
| 4 | Ziel 6 Kehren | ✅ | ~30–40s |
| 5 | Ziel 12 Kehren | ✅ | ~60–80s |
| 6 | Ziel2 2 Runden | ✅ | ~60–80s |
| 7 | Settings Navigation | ✅ | ~15s |
| 10a | Rapid Settings Changes | ✅ | ~10s |
| 10b | Debounce Timeout | ✅ | ~10s |
| **TOTAL** | **Alle grünen** | ✅ | **~6–8 Min** |

---

## 🔍 Troubleshooting

### 🖥️ Browser anzeigen (Headless deaktivieren)

Tests laufen standardmäßig im **Headless-Modus** (kein sichtbares Browser-Fenster). Um die Tests visuell zu beobachten:

```powershell
# PowerShell
$env:PLAYWRIGHT_HEADLESS = "false"
dotnet test StockTvBlazor.E2ETests/

# oder mit --filter für einzelne Phase
$env:PLAYWRIGHT_HEADLESS = "false"
dotnet test StockTvBlazor.E2ETests/ --filter "Phase1"
```

**Hinweis:** Im GUI-Modus sind Tests etwas langsamer. Für CI/CD sollte der Headless-Modus aktiv bleiben.

### ❌ Phase startet nicht (Port-Konflikt)

```powershell
# Prozesse auf Ports prüfen
netstat -ano | findstr "5001\|4747\|4748"

# StockTV Prozesse killen
Get-Process StockTvBlazor -ErrorAction SilentlyContinue | Stop-Process -Force
```

### ✅ Phase 4/5/6 sind vollständig implementiert

Alle drei Phasen sind grün und in Produktion. Siehe [Phase 4](#-phase-4-ziel--6-kehren-pro-disziplin), [Phase 5](#-phase-5-ziel--12-kehren-pro-disziplin), und [Phase 6](#-phase-6-ziel2--2-runden-à-4-disziplinen--6-kehren) für Details.

### ❌ Settings-Persistierung funktioniert nicht

- Prüfe `_config/stocktv.config.json` — wird sie aktualisiert?
- Debounce-Timeout: 1100ms — bei schnellen Tests `await Task.Delay(1200)` nutzen
- Logs in `TestResults/e2e-test-*.log` prüfen

### ❌ "Playwright executable doesn't exist"

```powershell
cd StockTvBlazor.E2ETests\bin\Debug\net10.0
.\playwright.ps1 install chromium
```

---

## ✅ Checkliste vor Tests

- [ ] Keine StockTV Prozesse laufen
- [ ] Ports 5001, 4747, 4748 frei
- [ ] .NET 10.0 SDK installiert
- [ ] `dotnet restore` ausgeführt
- [ ] Playwright Chromium installiert
- [ ] Log-Verzeichnis `TestResults/` darf lesen/schreiben

```powershell
# Schnelle Verifikation
dotnet --version              # 10.x?
netstat -ano | findstr "5001" # Port frei?
ls TestResults/               # Verzeichnis vorhanden?
```

---

## 📂 Dateistruktur

```
StockTvBlazor.E2ETests/
├── README.md                    ← Diese Dokumentation
├── Fixtures/
│   ├── AppFixture.cs            (App/Browser/NetMQ Management)
│   ├── PublisherSubscriberMessage.cs
│   └── PublisherSubscriberMessageQueue.cs
├── Helpers/
│   ├── GameplayScriptHelpers.cs (Input-Helfer: EnterAndConfirm, etc.)
│   └── TestLogWriter.cs         (Logging zu Console + Datei)
├── Tests/
│   └── Phases/
│       ├── PhaseTestBase.cs     (Basis-Klasse mit Log/Settings/Reset)
│       ├── Phase1TrainingE2ETests.cs
│       ├── Phase2TournamentE2ETests.cs
│       ├── Phase3BestOfE2ETests.cs
│       ├── Phase4Ziel6E2ETests.cs
│       ├── Phase5Ziel12E2ETests.cs
│       ├── Phase6Ziel2E2ETests.cs
│       ├── Phase7SettingsE2ETests.cs
│       └── Phase10SettingsPersistenceE2ETests.cs (2 Tests)
├── TestResults/
│   └── e2e-test-20260912-*.log (Auto-generiert nach Test-Run)
└── StockTvBlazor.E2ETests.csproj
```

---

## 🎯 Status & Nächste Schritte

**Aktuell Grün (11 Tests):**
- ✅ Phase 1–7: Training, Turnier, BestOf, Ziel (6 & 12 Kehren), Ziel2 (2 Runden), Settings Navigation
- ✅ Phase 10: Rapid Changes + Debounce Timeout (2 Tests)

**Implementiert (2026-09-13):**
- 🚀 Centralized Random Seed — Ein eindeutiger Seed pro Testlauf, einmalig geloggt
- ✅ Phase 4: Ziel 6 Kehren — Invalid input handling, discipline transitions, delete functionality
- ✅ Phase 5: Ziel 12 Kehren — Longer form, performance-validated, spaced logging
- ✅ Phase 6: Ziel2 2 Runden — Round transitions, automatic reset, gesamtsumme tracking

**Ausstehend:**
- 🔜 Phase 8: Deployment Checklisten
- 🔜 Phase 9: Theme & erweiterte Settings

---

**Version:** 2.2  
**Autor:** Comprehensive Phase-based E2E Suite  
**Status:** 11/11 Tests grün, Zentralisierte Seed-Verwaltung, Sequenzielle Execution
