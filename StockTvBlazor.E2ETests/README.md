# StockTV E2E Tests — Dokumentation

**Status:** ✅ Phase 1–7, Phase 9–13 grün | 🚀 Phase 4–6, Phase 9, Phase 11–13 vollständig implementiert  
**Datum:** 2026-09-15  
**Framework:** xUnit + Playwright + NetMQ  
**Execution:** Sequenziell, ein AppFixture für alle Tests ([Collection("E2E Sequential")])

---

## 🚀 Schnelleinstieg

```powershell
cd C:\Users\daniel\source\repos\StockTV

# Alle Tests (27 Tests: Phase 1–7, Phase 9 (5 Tests), Phase 10, Phase 11 (6 Tests), Phase 12 (4 Tests), Phase 13 grün)
dotnet test StockTvBlazor.E2ETests/

# Einzelne Phase
dotnet test StockTvBlazor.E2ETests/ --filter "Phase1"
dotnet test StockTvBlazor.E2ETests/ --filter "Phase7"
dotnet test StockTvBlazor.E2ETests/ --filter "Phase9"
dotnet test StockTvBlazor.E2ETests/ --filter "Phase10"
dotnet test StockTvBlazor.E2ETests/ --filter "Phase11"
dotnet test StockTvBlazor.E2ETests/ --filter "Phase12"
dotnet test StockTvBlazor.E2ETests/ --filter "Phase13"

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
✓ Phase 9: Theme Layout Editor (5 Tests)
✓ Phase 10: Settings Persistence (2 Tests)
✓ Phase 11: Schriftstärke der Zellen (6 Tests)
✓ Phase 12: Themes-Seite Live-Edit (4 Tests)
✓ Phase 13: Modus-Schnellwechsel auf /input (Press-and-Hold)

Bestanden: 27, Übersprungen: 0, Dauer: ~9–11 Min
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
- `ziel-state.json` Persistierung: Alle Versuche pro Disziplin korrekt gespeichert
- `ResetResult` NetMQ-Kommando: `ziel-state.json` wird korrekt gelöscht

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
- `ziel-state.json` Persistierung: Alle Versuche pro Disziplin korrekt gespeichert

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
- Finale Seite mit Gesamtsumme korrekt
- `ziel-state.json` Persistierung: Runde1 Versuche, Rundenwechsel, Runde 2 Versuche + Runde1Summe korrekt persistiert

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

### ✅ Phase 9: Theme & Tabellenstruktur — Layout-Editor

**Tests:** 5 Tests für TableLayoutEditor auf `/themes` → Tab "Tabellenstruktur"

#### Test 1: `Phase9_KehreZeile_DefaultValuesDisplayCorrectly()`

**Szenario:** Regressionstest für `@bind-value` zwei-Wege-Binding. Default-Werte müssen in der UI angezeigt werden.

**Ablauf:**
1. Navigiere zu `/themes`
2. Klicke auf Tab "Tabellenstruktur"
3. Öffne Accordion "Kehre-Zeile"
4. Lese die drei Eingabefelder:
   - Links: **42.5%**
   - Mitte: **15%**
   - Rechts: **42.5%**

**Validierung:**
- Alle drei Felder zeigen die korrekten Default-Werte an
- Keine fehlenden/leeren Felder (Bug-Regression)

**Dauer:** ~3–5s

#### Test 2: `Phase9_SumWarning_AppearsWhenSumNot100_DisappearsWhenFixed()`

**Szenario:** Live-Warnung bei ungültiger Summe (≠ 100%) ein-/ausblenden.

**Ablauf:**
1. Öffne "Kehre-Zeile"
2. Ändere "Links" auf **50** → Summe wird 107,5 %
3. Prüfe `.sum-warning` ist sichtbar und enthält Text mit "107.5"
4. Ändere "Links" zurück auf **42.5** → Summe = 100 %
5. Prüfe `.sum-warning` verschwindet

**Validierung:**
- Warnung erscheint **sofort** nach Wert-Änderung (via `@bind-value:after` Callback)
- Warntext enthält die Summe: "⚠ Summe: 107.5 % — sollte 100 % sein"
- Warnung verschwindet, sobald Summe wieder 100 % ist

**Dauer:** ~5–7s

#### Test 3: `Phase9_ValueChange_PersistsToConfigFile()`

**Szenario:** Wert-Änderung wird nach Debounce in `stocktv.config.json` persistiert.

**Ablauf:**
1. Öffne "Punkte-Grid (Training / Turnier)"
2. Ändere "Mitte" (MidGrid3MidWidth) auf **8**
3. Warte `DEBOUNCE_DELAY_MS` (1100ms)
4. Lese `_config/stocktv.config.json`
5. Prüfe `UI.TableLayout.MidGrid3MidWidth == 8`
6. Cleanup: setze zurück auf Default (**6**)

**Validierung:**
- Wert wird nach Debounce-Timeout in JSON geschrieben
- `JsonDocument.Parse()` bestätigt den neuen Wert
- Cleanup funktioniert (anderer Test wird nicht beeinflusst)

**Dauer:** ~10–12s (wegen Debounce-Wartezeit)

#### Test 4: `Phase9_ResetAllValues_RestoresDefaultsInUiAndFile()`

**Szenario:** Button "Alle Werte zurücksetzen" stellt alle Defaults wieder her.

**Ablauf:**
1. Öffne zwei verschiedene Accordion-Gruppen
2. Ändere 2–3 Werte in verschiedenen Gruppen
3. Klicke Button "Alle Werte zurücksetzen"
4. Warte Debounce-Timeout
5. Prüfe:
   - UI zeigt alle Default-Werte
   - Keine `.sum-warning` mehr sichtbar irgendwo
   - Config-Datei enthält alle Default-Werte + `MidColumnWidth == 90`

**Validierung:**
- Reset-Button funktioniert und setzt **alle** Felder zurück
- Config-Datei wird aktualisiert
- Keine Seiten-Effekte auf andere Tests (vollständiger Cleanup)

**Dauer:** ~10–12s

#### Test 5: `Phase9_GroupKindHint_DistinguishesRowsFromColumns()`

**Szenario:** UX-Hint unterscheidet Zeilen-Gruppen von Spalten-Gruppen.

**Ablauf:**
1. Öffne "Kopf-/Mitte-/Fuß-Zeilen" (Zeilen-Gruppe)
2. Prüfe `.group-kind` Text enthält **"Zeilenhöhen"**
3. Öffne "Kehre-Zeile" (Spalten-Gruppe)
4. Prüfe `.group-kind` Text enthält **"Spaltenbreiten"**

**Validierung:**
- Zeilen-Gruppen zeigen "↕ Zeilenhöhen — werden übereinander angeordnet..."
- Spalten-Gruppen zeigen "↔ Spaltenbreiten — werden nebeneinander angeordnet..."

**Dauer:** ~3–5s

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

### ✅ Phase 11: Schriftstärke der Zellen — Font-Editor

**Tests:** 6 Tests für `FontEditor` auf `/themes` → Tab "Schrift"

Analog zu Phase 9 (Tabellenstruktur), aber für `font-weight` statt Zeilen-/Spaltenmaße: 14 zuvor
hartkodierte CSS-Werte (`.teamname`, `.score-top-row`, `.left-point-sum`/`.right-point-sum`,
`.left-points`/`.right-points`, `.input-value`, `.seperator`, BestOf-Matchpunkte,
`.ziel-spielername` sowie die 6 Ziel-Werte-Zellen) sind jetzt per Dropdown (100–900, CSS-
Standardstufen) editierbar. Feld-Lookup nutzt von Anfang an Exact-Match auf den Label-Textknoten
(`label/text()[normalize-space()='...']`), um die in Phase 9 gefundene Substring-Kollisionsgefahr
(z.B. "Gesamt" vs. "Gesamtpunkte") von vornherein auszuschließen.

#### Test 1: `Phase11_DefaultValuesDisplayCorrectly()`

**Szenario:** Alle 14 Dropdowns zeigen nach Reset ihre Default-Werte.

**Ablauf:**
1. Navigiere zu `/themes`, Tab "Schrift"
2. Reset, dann alle 14 Felder auslesen und gegen ihre bekannten Defaults prüfen (400/700/700/700/
   700/600/200/600/500/500/500/500/500/500)

**Dauer:** ~3–5s

#### Test 2: `Phase11_ValueChange_PersistsToConfigFile()`

**Szenario:** Wert-Änderung wird nach Debounce in `stocktv.config.json` persistiert.

**Ablauf:**
1. "Summe" (ZielSummeWeight) auf 900 setzen
2. Debounce abwarten, `UI.CellFontWeight.ZielSummeWeight == 900` in der Config-Datei prüfen
3. Cleanup: zurück auf 500

**Dauer:** ~5–7s

#### Test 3: `Phase11_ResetAllValues_RestoresDefaultsInUiAndFile()`

**Szenario:** Button "Alle Werte zurücksetzen" stellt alle Defaults in UI und Datei wieder her.

**Ablauf:**
1. Zwei Felder aus unterschiedlichen Bereichen ändern (Team-Namen, Ziel-Summe)
2. Reset klicken, UI + `stocktv.config.json` auf Default-Werte prüfen

**Dauer:** ~5–7s

#### Test 4: `Phase11_ZielFields_IndependentlyEditable()`

**Szenario:** Regressionstest — die 6 Ziel-Werte-Zellen teilten sich vorher eine gemeinsame
`.ziel-cell`-Regel; jetzt muss eine Änderung an einem Feld die anderen 5 unberührt lassen.

**Ablauf:**
1. Nur "Summe" auf 900 ändern
2. Prüfen, dass Versuche/Gesamt/Letzter Wert/Ziel-Eingabe/Gesamtpunkte/Spielername unverändert
   bei ihren Defaults bleiben

**Dauer:** ~5–7s

#### Test 5: `Phase11_ScoreCellFields_IndependentlyEditable()`

**Szenario:** Analoger Regressionstest für Training/Turnier/BestOf — Punkte-Summe, aktuelle
Kehre-Punkte und Eingabe teilten sich vorher `.score-cell`.

**Ablauf:**
1. Nur "Eingabe (Tippbuffer)" auf 100 ändern
2. Prüfen, dass Punkte-Summe und Aktuelle Kehre-Punkte unverändert bei 700 bleiben

**Dauer:** ~5–7s

#### Test 6: `Phase11_FontWeightAppliesToRenderedCell()`

**Szenario:** Absicherung über die reine Settings-Persistierung hinaus — die CSS-Variable muss
tatsächlich visuell auf der echten Seite ankommen, nicht nur im Setting gespeichert werden.

**Ablauf:**
1. "Summe" auf 900 setzen
2. Zu `/ziel` navigieren, computed `font-weight` von `.ziel-summe` per `getComputedStyle()`
   auslesen und gegen "900" prüfen
3. Cleanup: zurück zu `/themes`, Reset

**Dauer:** ~5–7s

---

### ✅ Phase 12: Themes-Seite Live-Edit — Live-Apply, Footer, Schrift-Standort, Scroll-Fix

**Tests:** 4 Tests für `CustomThemePage` auf `/themes` → Tab "Themes" (+ Tab "Schrift")

Deckt die Überarbeitung der Themes-Seite ab: Farb-/Feld-Änderungen an einem bestehenden Theme
werden jetzt sofort (ohne Klick auf "Speichern") persistiert — "Speichern" dient nur noch dem
Anlegen neuer Themes; der Footer (Abbrechen/Speichern) ist dementsprechend nur noch beim Anlegen
sichtbar; die Schriftart-Auswahl ist aus dem "Themes"-Tab in den "Schrift"-Tab gewandert und
global (nicht mehr pro Theme); ein Scroll-Fix stellt sicher, dass der Speichern-Button auch bei
kleinem Fenster erreichbar bleibt.

#### Test 1: `Phase12_LiveApply_ColorChangeOnExistingThemePersistsWithoutSave()`

**Szenario:** Eine Farbänderung an einem bereits gespeicherten Theme wird ohne Klick auf
"Speichern" nach Debounce persistiert.

**Ablauf:**
1. Neues Theme anlegen + speichern (macht es zu einem "bestehenden" Theme)
2. "Farben"-Accordion öffnen, "Hintergrund" auf einen neuen Hex-Wert ändern — **kein** Speichern-Klick
3. Debounce abwarten, `stocktv.config.json` prüfen: der neue Farbwert ist persistiert
4. Cleanup: Test-Theme löschen

**Validierung:** Name ist nicht eindeutig — es genügt, dass irgendein Eintrag mit dem Testnamen die
neue Farbe zeigt (robust gegenüber eventuellen Karteileichen aus früheren Läufen).

**Dauer:** ~4–6s

#### Test 2: `Phase12_Footer_HiddenForExistingTheme_VisibleForNewTheme()`

**Szenario:** Der Footer (Abbrechen/Speichern) ist nur beim Anlegen eines neuen Themes sichtbar.

**Ablauf:**
1. "+ Neu" klicken → `.editor-footer` ist sichtbar (1 Treffer)
2. "Speichern" klicken → `.editor-footer` ist verschwunden (0 Treffer)
3. Cleanup: Test-Theme löschen

**Dauer:** ~2–3s

#### Test 3: `Phase12_FontFamilyDropdown_OnlyInSchriftTab()`

**Szenario:** Die Schriftart-Auswahl existiert nur noch im "Schrift"-Tab, nicht mehr im
"Themes"-Tab.

**Ablauf:**
1. Auf dem "Themes"-Tab: Label "Schriftart (optional)" → 0 Treffer
2. Auf dem "Schrift"-Tab: Label "Schriftart (optional)" → 1 Treffer

**Dauer:** ~2–3s

#### Test 4: `Phase12_SmallViewport_SaveButtonReachableViaScroll()`

**Szenario:** Regressionstest für den Scroll-Bug — bei kleinem Viewport (800×450) muss der
Speichern-Button beim Anlegen eines neuen Themes per Scroll erreichbar/klickbar bleiben.

**Ablauf:**
1. Viewport auf 800×450 verkleinern
2. "+ Neu" klicken, "Speichern" klicken — Playwright scrollt beim Klick automatisch in den
   sichtbaren Bereich; schlägt der Scroll fehl, läuft der Klick in ein Timeout
3. Cleanup: Test-Theme löschen, Viewport zurücksetzen

**Validierung:** Klick war erfolgreich (kein Timeout) → Footer verschwunden (Theme gespeichert).

**Dauer:** ~2–3s

---

### ✅ Phase 13: Modus-Schnellwechsel auf /input — Press-and-Hold Modus-Bearbeitung

**Test:** 1 Test (`Phase13_ModusQuickSwitch_DisabledWhileSubscriberConnected_ThenFullFlow`) für die neue
Bearbeitung des `.display`-Felds auf `/input`: 5 Sekunden gedrückt halten schaltet das Numpad auf
die von der Settings-Seite bekannte up/prev/next/down-Beschriftung um, wobei nur "4"/"6" den
Modus durchblättern; "+" bestätigt (speichert + navigiert). Die Funktion ist bewusst nur nutzbar,
solange kein Subscriber (StockApp) am NetMQ-PUB-Socket verbunden ist
(`General.BlockLocalChanges`).

**Ablauf:**
1. Modus per NetMQ auf Training setzen (definierter Ausgangszustand)
2. Zu `/input` navigieren
3. **Deaktiviert bei verbundenem Subscriber:** Der Test-Fixture-Subscriber ist seit
   `AppFixture.InitializeNetMQ()` dauerhaft verbunden (Ausgangszustand aller E2E-Tests) →
   `.display` hat keine `clickable`-Klasse; 5s Halten aktiviert die Bearbeitung nicht
4. `Fixture.SetPublisherSubscriptionActive(false)` — Test-Subscriber trennt sich testweise
   (simuliert StockApp-Verbindungsabbau) → `.display` wird `clickable` (gepollt, bis zu 5s)
5. Kurzer Tap (1s, < 5s) → aktiviert nichts
6. 5s+ Halten auf `.display` → `.keypad-panel` bekommt `modus-edit-active`-Klasse, Numpad zeigt
   up/prev/next/down statt Ziffern
7. Ziffern-Taste (leere Position) antippen → wirkungslos (Modus + Bearbeitungsmodus unverändert)
8. "next" (6) antippen → `.display`-Text wechselt sofort zu "BestOf", Iframe-`src` bleibt
   unverändert (noch nicht bestätigt)
9. "Bestätigen" (+) antippen → Bearbeitungsmodus endet, Iframe navigiert zu `/bestof`, Numpad
   zeigt wieder Ziffern
10. `stocktv.config.json` prüfen: `CurrentModus`/`MaxPunkteProKehre`/`MaxKehrenProSpiel`
    korrekt persistiert (Modus=1/BestOf)
11. Cleanup (`finally`): Test-Subscriber wird wieder verbunden
    (`SetPublisherSubscriptionActive(true)`), damit `BlockLocalChanges=true` — der von allen
    anderen Phasen erwartete Ausgangszustand — wiederhergestellt ist

**Validierung:** CSS-Klassen (`clickable`, `modus-edit-active`), Numpad-Beschriftung, Anzeige-Text,
Iframe-`src` vor/nach Bestätigung, Settings-Persistierung in `stocktv.config.json`.

**Infrastruktur:** `AppFixture.SetPublisherSubscriptionActive(bool)` (neu) verbindet/trennt die
testeigene Subscription am PUB-Socket (Port 4748), um serverseitig `General.BlockLocalChanges` zu
simulieren — während der Trennung werden keine Publisher-Nachrichten (z.B. `GetResult`, `Alive`)
aufgezeichnet, was für diesen Test unerheblich ist.

**Dauer:** ~15–20s

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
| 9a | Default Values Display | ✅ | ~3–5s |
| 9b | Sum Warning Display | ✅ | ~5–7s |
| 9c | Value Persistence | ✅ | ~10–12s |
| 9d | Reset All Button | ✅ | ~10–12s |
| 9e | Group Kind Hints | ✅ | ~3–5s |
| 10a | Rapid Settings Changes | ✅ | ~10s |
| 10b | Debounce Timeout | ✅ | ~10s |
| **TOTAL** | **Alle grünen** | ✅ | **~8–10 Min** |

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
│       ├── Phase9ThemeLayoutE2ETests.cs (5 Tests)
│       ├── Phase10SettingsPersistenceE2ETests.cs (2 Tests)
│       ├── Phase11FontWeightE2ETests.cs (6 Tests)
│       ├── Phase12ThemesLiveEditE2ETests.cs (4 Tests)
│       └── Phase13ModusQuickSwitchE2ETests.cs (1 Test)
├── TestResults/
│   └── e2e-test-20260912-*.log (Auto-generiert nach Test-Run)
└── StockTvBlazor.E2ETests.csproj
```

---

## 🎯 Status & Nächste Schritte

**Aktuell Grün (27 Tests):**
- ✅ Phase 1–7: Training, Turnier, BestOf, Ziel (6 & 12 Kehren), Ziel2 (2 Runden), Settings Navigation
- ✅ Phase 9: Theme Layout Editor (5 Tests) — Default values, sum warnings, persistence, reset, group hints
- ✅ Phase 10: Rapid Changes + Debounce Timeout (2 Tests)
- ✅ Phase 11: Schriftstärke der Zellen (6 Tests) — Default values, persistence, reset, Ziel-/Score-Cell-Unabhängigkeit, gerenderte Zelle
- ✅ Phase 12: Themes-Seite Live-Edit (4 Tests) — Live-Apply, Footer-Sichtbarkeit, Schriftart-Standort, Scroll-Fix
- ✅ Phase 13: Modus-Schnellwechsel auf /input (1 Test) — Press-and-Hold-Aktivierung, Deaktivierung bei verbundenem Subscriber, Live-Vorschau, Bestätigung + Persistierung

**Implementiert (2026-09-27):**
- 🚀 Phase 11: Schriftstärke der Zellen — FontEditor E2E Tests (6 Szenarien)
  - Default-Werte-Display
  - Persistierung nach Debounce-Timeout
  - Reset-Button-Funktionalität
  - Unabhängigkeit der 6 Ziel-Werte-Zellen (vormals gemeinsame `.ziel-cell`-Regel)
  - Unabhängigkeit der 3 Training/Turnier/BestOf-Punkte-Zellen (vormals gemeinsame `.score-cell`-Regel)
  - End-to-End-Rendering-Check (computed `font-weight` auf der echten Seite)
- 🚀 Phase 12: Themes-Seite Live-Edit E2E Tests (4 Szenarien)
  - Live-Apply: Farbänderung an bestehendem Theme ohne Speichern-Klick persistiert
  - Footer nur beim Anlegen eines neuen Themes sichtbar
  - Schriftart-Auswahl nur noch im Schrift-Tab (global statt pro Theme)
  - Scroll-Fix: Speichern-Button bei kleinem Viewport erreichbar
- 🚀 Phase 13: Modus-Schnellwechsel auf /input E2E Test (1 Szenario)
  - Deaktiviert, solange ein Subscriber (StockApp) am PUB-Socket verbunden ist
  - Press-and-Hold-Aktivierung (kurzer Tap wirkungslos, 5s+ aktiviert)
  - Numpad-Umschaltung auf up/prev/next/down, nur "4"/"6" wirksam
  - Live-Vorschau ohne Iframe-Navigation, Bestätigung via "+" navigiert + persistiert

**Ausstehend:**
- 🔜 Phase 8: Deployment Checklisten

---

**Version:** 2.5  
**Autor:** Comprehensive Phase-based E2E Suite  
**Status:** 27/27 Tests grün, Zentralisierte Seed-Verwaltung, Sequenzielle Execution
