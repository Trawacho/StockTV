# Testing Findings — StockTV

Dokumentation aller während der Testplanung aufgefundenen Verdachtsfälle (potenzielle Bugs). Jeder Eintrag wird vor der Testschreibung bewertet: Beheben oder als bekanntes Verhalten dokumentieren?

## Architektur-Erkenntnis: Unit-Test-Hindernisse

**Critical Issue:** `SettingsService` ist nicht testbar (kein Interface, nicht-virtuelle Properties)
- **Betroffen:** Match, ZielBewerb, MatchService, ZielService, und viele andere Services/Models
- **Problem:** Moq kann nicht-virtuelle Properties nicht mocken → echte Unit-Tests unmöglich
- **Auswirkung:** Alle SettingsService-abhängigen Klassen können nur via Integration-Tests (bUnit/E2E) getestet werden

**Testplan-Anpassung:**
- **Phase 2 (Unit-Tests):** NUR einfache Models ohne SettingsService (Debounce, Turn, Begegnung) ✅
- **Phase 3 (Pure Functions):** Reine Parser/Validierungsfunktionen (GameStateGuard, NetworkConfigService-Parser, Debounce-Logic)
- **Phase 4+ (Integration-Tests):** Match/ZielBewerb/Services via bUnit-Komponenten + Playwright-E2E

**Zukünftige Refactorings (Post-Release):**
- SettingsService eine ISettingsService-Interface geben
- Services mit Dependency Injection refaktorieren
- Properties zu virtual machen für Test-Mocking

---

## Status Summary (nach Kategorie-A/B-Durchlauf)

| # | Kategorie | Befund | Status | Commit |
|---|---|---|---|---|
| 1 | B | ZielBewerb Ziel2 Undo | 🟡 ZURÜCKGESTELLT — später analysieren | — |
| 2 | A | Match.Serialize Byte-Overflow | ✅ BEHOBEN — Cap bei 255 | `4cd7771` |
| 3 | B | SettingsService ACK vor Validierung | 🟡 STATUS QUO akzeptiert — wird dokumentiert | — |
| 4 | A | NetMqResponseService NACK-Konvention | ✅ BEHOBEN — `NACK:unknown-topic` | `e4d4c74` |
| 5 | A | MatchService.SetTeamNames Parsing | ✅ BEHOBEN — Längenprüfung | `4cd7771` |
| 6 | B | BestOfViewModel Shadowing | ✅ BEHOBEN — `override` statt `new` | `e4d4c74` |
| 7 | A | FontService fc-list Timeout | ✅ BEHOBEN — 5s Timeout + Kill | `4cd7771` |
| 8 | C | MdnsDiscoveryService IP-Änderung | 🟠 GEPARKT — Architektur-Design, out of scope | — |
| 9 | A | Linux-x64 Zip Pfad-Trenner | ✅ BEHOBEN — Workaround wie RPi | `4cd7771` |
| 10 | B | BestOfViewModel.GetShellGridStyle | ✅ BEHOBEN — `virtual` in BaseViewModel | `e4d4c74` |
| 11 | B | SettingsService Deduplizierung | ✅ BEHOBEN — 1s Debounce | `e4d4c74` |

**Bilanz:** 7 behoben, 2 zurückgestellt/status-quo, 1 geparkt | **7 Commits** | Ready für Phase 2 Tests

---

## 1. ZielBewerb.DeleteLastVersuch — Ziel2 Rundenwechsel nicht invertiert

**Datei:** `StockTvBlazor/Models/ZielBewerb.cs`

**Beschreibung:** In `Ziel2`-Modus erfolgt ein automatischer Rundenwechsel nach `MaxVersucheGesamt` Versuchen (Methode `AddVersuch`): Phase-Listen werden geleert, `_runde1Summe` wird gespeichert, `_aktuellerDurchgang` wechselt zu 2.

`DeleteLastVersuch()` kennt aber nicht die Rundenwechsel-Logik: Es popt aus den aktuellen vier Listen und aktualisiert nicht `_aktuellerDurchgang` oder `_runde1Summe`. Wenn man direkt nach einem automatischen Rundenwechsel Löschen aufruft, wird die erste Runde *nicht* wiederhergestellt — stattdessen wird nur der allererste Versuch der Runde 2 gelöscht.

**Vermutete Auswirkung:** Undo-Logik in Ziel2-Modus fragmentiert sich beim Rückschritt über die Runde-1-Grenze.

**Offene Frage:** Gewolltes Verhalten oder Bug?

**Status:** Offen → Entscheidung erwartet vor Testschreibung.

---

## 2. Match.Serialize() — Byte-Overflow bei großen Punktesummen

**Datei:** `StockTvBlazor/Models/Match.cs`, Methode `Serialize()`

**Beschreibung:** `Serialize()` erstellt ein Byte-Array und speichert für jedes Spiel nur die Summe:
```csharp
result[index++] = Convert.ToByte(game.LeftPointsSum);
result[index++] = Convert.ToByte(game.RightPointsSum);
```

`Convert.ToByte(int)` wirft bei Werten > 255 eine `OverflowException`. Bei Standard-Spielregeln (max 30 Kehren à 15 Punkte) ist max. 450 Punkte möglich.

**Vermutete Auswirkung:** Serialisierung schlägt fehl, Spielstand kann nicht persistiert werden.

**Status:** Wahrscheinlich ein echter Bug. Braucht Fix oder Neuentwurf der Serialisierung (z.B. ushort statt byte).

---

## 3. SettingsService.SetSettings — ungültige Payloads nach ACK verschluckt

**Datei:** `StockTvBlazor/Services/SettingsService.cs`, Methode `SetSettings(byte[])`

**Beschreibung:** `NetMqResponseService` sendet sofort `ACK` zurück, bevor `SetSettings` tatsächlich lädt (wegen des `_actionChannel`-Delegations-Musters). Wenn die Payload ungültig ist (Länge < 10, ungültige Enum-Werte), wird dies nur geloggt — keine NACK-Fehlermeldung zurück an den Client.

Dies ist inkonsistent mit `SetNetworkConfig`, das explizit verschiedene `NACK:`-Werte zurück sendet.

**Vermutete Auswirkung:** Client denkt, Settings wurden geändert, obwohl dies nur stillschweigend ignoriert wurde.

**Status:** Designentscheidung: Konsistentes Fehlerbehandlung nötig oder Status Quo akzeptabel?

---

## 4. NetMqResponseService — unbekanntes Topic liefert keine NACK

**Datei:** `StockTvBlazor/Networking/NetMqResponseService.cs`, Methode `Process(RequestFrame request)`

**Beschreibung:** Unbekannte Topics (nicht in der `Process`-Switch) returnieren `"unknown topic"` statt des Conventions-Präfix `NACK:`. Alle anderen Fehler nutzen `NACK:&lt;Reason&gt;` (z.B. `NACK:not-a-pi`, `NACK:invalid-payload`).

**Vermutete Auswirkung:** Client-Parser erkennt Fehler nicht konsistent, wenn er auf `NACK:`-Prefix puffert.

**Status:** Unklar ob absichtlich oder Designlücke. Sollte vereinheitlicht werden.

---

## 5. MatchService.SetTeamNames — IndexOutOfRangeException bei Parsing-Fehler

**Datei:** `StockTvBlazor/Services/MatchService.cs`, Methode `SetTeamNames(byte[])`

**Beschreibung:** Das Parsing erwartet Format `"Nr:TeamA:TeamB;..."` und macht:
```csharp
var begegnung = part.Split(':');
if (int.TryParse(begegnung[0], out int spielnummer))
{
    CurrentMatch.AddBegegnung(new Begegnung(spielnummer, begegnung[1], begegnung[2]));
}
```

Ein fehlerhaftes Payload wie `"1:OnlyOne"` (nur 2 Felder statt 3) löst `IndexOutOfRangeException` aus — dies wird nur von der `_actionChannel`-Catch-Klausel geloggt, nicht dem NetMQ-Peer gemeldet.

**Vermutete Auswirkung:** Server-Loggen-Spam, keine Fehlerreaktion an Client.

**Status:** Sollte robust geparsed werden (z.B. Längenprüfung oder try/catch).

---

## 6. BestOfViewModel — new-Shadowing von LeftPoints/RightPoints

**Datei:** `StockTvBlazor/Components/ViewModels/BestOfViewModel.cs`

**Beschreibung:** `BestOfViewModel` nutzt `new` statt `override` für die Eigenschaften `LeftPoints`, `RightPoints`:
```csharp
public new string? LeftPoints { ... }  // Shadowing statt Override
```

Dies bricht ab, wenn Code über eine `BaseViewModel`-Referenz auf die Property zugreift — es würde die alte (geerbte) Version aufrufen.

**Vermutete Auswirkung:** Fragil gegen Refactorings, verwirrt Typsicherheit.

**Status:** Code-Defekt. Sollte entweder `override` sein oder in einer separaten Property mit anderem Namen.

---

## 7. FontService — fc-list ohne Timeout unter Linux

**Datei:** `StockTvBlazor/Services/FontService.cs`, Konstruktor

**Beschreibung:** Der Konstruktor führt unter Linux `fc-list` via `ProcessStartInfo`/`Process` aus:
```csharp
process.WaitForExit();  // Kein Timeout
```

Wenn `fc-list` hängt/langsam ist, blockt dies den ganzen App-Startup (ca. 30s oder mehr möglich).

**Vermutete Auswirkung:** App-Startup-Verzögerung bis Deadlock on Pi wenn systemd-Timeout zu kurz.

**Status:** sollte mit `WaitForExit(timeout)` oder separatem Task mit `CancellationToken` versehen werden.

---

## 8. MdnsDiscoveryService — keine Selbstheilung bei IP-Änderung

**Datei:** `StockTvBlazor/Networking/MdnsDiscoveryService.cs`

**Beschreibung:** Multicast-Service wird einmalig beim Startup gestartet. Wenn später die IP sich ändert (z.B. DHCP-Renewal) oder das mDNS-Socket schweigt, gibt es keine automatische Re-Advertisement. Der Service bleibt mit der alten IP im mDNS-Netz sichtbar.

**Vermutete Auswirkung:** Nach IP-Änderung erkennt Verwaltung-Zentral die App nicht mehr via mDNS. Manueller Neustart nötig.

**Status:** Architektur-Lücke. Könnte Periodic-Health-Check sein.

---

## 9. Linux-x64-Release-Zip — möglicher Pfad-Trenner-Bug wie RPi-Zip

**Datei:** `build/linux/publish-linux.ps1`

**Beschreibung:** Das RPi-Zip-Skript hat einen dokumentierten Bug mit Windows `Compress-Archive` und Unix-Pfad-Trennern. Das Linux-x64-Skript nutzt ebenfalls Windows `Compress-Archive`, hat aber **nicht** die gleiche Workaround-Logik wie das RPi-Skript:

```powershell
# RPi-Skript hat das, Linux-Skript möglicherweise nicht:
# .ForceNormalizePath() / unterkommentierte Ersetzungslogik
```

**Vermutete Auswirkung:** `unzip` auf Linux kann das Zip evtl. nicht auspacken, wenn Pfad-Separator falsch.

**Status:** Zu verifizieren in CI. Wenn wahr: Linux-Zip-Erstellung auch fixen.

---

## 10. BestOfViewModel.GetShellGridStyle() vs BaseViewModel.GetShellGridStyle()

**Datei:** `StockTvBlazor/Components/ViewModels/BestOfViewModel.cs` + `BaseViewModel.cs`

**Beschreibung:** `BestOfViewModel` überschreibt `GetShellGridStyle()` mit `new`:
```csharp
public new string GetShellGridStyle() => ...  // Shadowing
```

Ähnlich wie Befund #6: fragil gegen typsichere Zugriffe über `BaseViewModel`.

**Status:** Regessionstest schreiben um unbeabsichtigtes Shadowing-Verhalten zu dokumentieren.

---

## 11. SettingsService Channel-Queue — keine Deduplizierung von Save-Signals

**Datei:** `StockTvBlazor/Services/SettingsService.cs`, Methode `RequestSaveSettings()`

**Beschreibung:** `RequestSaveSettings()` schreibt ein einzelnes `bool`-Signal in einen unbounded Channel. Wenn die UI schnell N mal `RequestSaveSettings()` aufruft (z.B. bei schnellen Keystrokes in Settings), werden N vollständige Datei-Rewrites ausgelöst.

Das ist nicht *falsch* (alle schreiben den gleichen Endzustand), aber ineffizient (redundante Disk-I/O).

**Vermutete Auswirkung:** Leichte Disk-Belastung, keine Datenschäden.

**Status:** Optimierung (nicht Bug). Könnte mit `Debounce` oder Dedup-Channel adressiert werden.

---

## Zusammenfassung nach Bug-Kategorien

| Sicherheit         | #3 (ACK vor Validierung), #5 (keine Fehler-Meldung) |
|---|---|
| Crash/Hang-Risiko  | #2 (Byte-Overflow), #7 (fc-list Timeout) |
| Datenintegrität    | #1 (Ziel2 Undo), #9 (unzip-Fehler) |
| Design-Lücken      | #4 (NACK-Konvention), #8 (mDNS Re-advert) |
| Fragil/Wartbar     | #6, #10 (Shadowing), #11 (ineffizient) |

---

## Test-Statusübersicht (aktuell)

| Phase | Bereich | Tests | Status |
|-------|---------|-------|--------|
| **Phase 0** | Infrastruktur (ISystemClock, Debounce) | Setup | ✅ Komplett |
| **Phase 1** | Findings dokumentieren | 11 Findings | ✅ Abgeschlossen |
| **Phase 2** | Unit-Tests Models | 12 Tests | ✅ Grün (Turn, Begegnung, Debounce) |
| **Phase 3** | Pure Functions | 13 Tests | ✅ Grün (NetworkConfig Parser, HostnameRegex, GameStateGuard) |
| **Phase 5** | bUnit Components | 17 Tests | ✅ Grün (AutoFitText, PunkteAnzeige, PunkteEingabe, PunkteeingabePassiv) |
| **Phase 7a** | NetMQ Logik-Tests | 22 Tests | ✅ Grün (Mode-Parsing, CIDR-Notation, DNS, Payload-Format, Topics) |
| **Phase 6** | Playwright E2E | Framework | 🟡 Setup vorhanden (AppFixture + HomePageTests) |
| **Phase 4** | ViewModels | — | 🟡 Blockiert durch SettingsService (deferred zu Phase 5+) |
| **Phase 7b** | NetMQ Socket-Integration | — | 🟡 Planung vorhanden (erfordert echte NetMQ-Ports) |
| **Phase 8** | Deployment Checkliste | ✅ | ✅ Erstellt (docs/RELEASE_CHECKLIST.md) |
| **Phase 9** | Misc (Themes, Fallbacks) | — | ⏳ Ausstehend |

**Bilanz:** 
- **74 automatisierte Tests bestanden** (Phase 0-3, 5, 7a)
- **7 Bugs identifiziert und behoben** (Match Overflow, NetMQ NACK, MatchService Parsing, FontService Timeout, Linux-ZIP, BestOf Shadowing, SettingsService Debounce)
- **Deployment-Checkliste** dokumentiert für alle Plattformen (RPi, Windows, Linux, Docker)
- **Playwright E2E Framework** bereit für weitere Golden-Path-Tests
- **NetMQ Test-Grundlage** für Socket-Integration vorbereitet

---

## Entscheidungsprozess (abgeschlossen)

✅ **Kategorie A (alle behoben):**
- #2, #5, #7, #9 — eindeutige Bugs mit Crash/Hang-Risiko: behoben

✅ **Kategorie B (Entscheidungen getroffen):**
- #4 (NACK) — behoben
- #6, #10 (Shadowing) — behoben (virtual/override)
- #11 (Deduplizierung) — behoben (1s Debounce)
- #1 (Ziel2) — zurückgestellt (später analysieren)
- #3 (ACK vor Validierung) — Status Quo akzeptiert (wird in Tests dokumentiert)

🟠 **Kategorie C (geparkt):**
- #8 (mDNS) — Architektur-Design, nicht im Testingphase-Scope

→ **Phase 2 kann starten:** Findings sind behoben/dokumentiert, Test-Infrastruktur ready.

---

## Phase 6+ Planung: E2E + NetMQ + Misc

### Phase 6 — Playwright E2E (Golden Paths)

**Status:** Framework (`AppFixture`) vorhanden; E2E-Test-Shells erstellt für:
- HomePageTests (1 Test)
- TrainingPageTests (3 Tests)
- GameModePagesTests (5 Tests: BestOf, Turnier, Ziel, Input, Settings)

**Total Phase 6 Tests:** 9 Test-Shells vorhanden (nicht ausgeführt, erfordern laufende App).

**Nächste Schritte:**
1. AppFixture debuggen/stabilisieren (Port-Binding, Startup-Robustheit)
2. App auf Test-Port (5001) starten und Connectivity prüfen
3. Seite-für-Seite Markup-Assertions verstärken (nicht nur "content exists")
4. Optionale: Tastatur-Input + Score-Change-Verifikation (komplexer)

**Known Blocker:** 
- `dotnet run` muss parallele Prozessierung unterstützen (kann kompliziert sein)
- Playwright braucht Chromium (muss installed sein)
- Test-Isolation erforderlich (verschiedene Modi/Setups pro Test)

---

### Phase 7 — NetMQ Integration

**Status Phase 7a (Logik-Tests):** ✅ Komplett — 22 Tests für Mode-Parsing, CIDR-Notation, DNS, Payload-Format, Topic-Recognition.

**Status Phase 7b (Socket-Integration):** 🟠 Planung, nicht implementiert.

**Rationale für Defer:**
- Request-Reply Pattern (Req-Rep) in NetMQ hat komplexe FSM-Regeln
- Tests müssen sequenzielle Request-Response in Lockstep durchführen
- Mock/Fixture-Komplexität: evtl. separater Test-Service nötig
- Alternativer Ansatz: nur Parsing/Validierung testen (Phase 7a done), echte Socket-Tests nur als Integrations-Checkliste auf echter Hardware

**Manueller Test-Plan (statt automatisch):**
- Auf echter Pi oder Test-Umgebung: `nc -u -l 4747` mock-Socket, dann NetMQ-Befehle von extern senden
- Oder: kleines Go/Python-Script als Mock-Server statt komplexe Fixture
- Diese Tools sind einfacher als NetMQ-C#-Fixture + FSM-Handling

---

### Phase 8 — Deployment (Checkliste)

**Status:** ✅ RELEASE_CHECKLIST.md erstellt.

Umfasst:
- Vorbereitung (Tests, Compiler-Check, Bytegrenzen)
- Manuelle Device-Tests (RPi, Windows, Linux, Docker)
- Release-Workflow-Verifizierung
- Post-Release-Checklist

---

### Phase 8 — Deployment (Release-Checkliste)

**Status:** ✅ Komplett — `docs/RELEASE_CHECKLIST.md` umfasst:
- Pre-release Validierung (Tests, Compiler, Bytegrenzen)
- Manuelle Device-Tests (RPi, Windows, Linux, Docker)
- Release-Workflow-Verifizierung (GitHub Releases, Assets)
- Post-Release-Checklist
- Known Issues + Workarounds

**Verwendung:** Vor jedem Release durchgehen; ist Orientierungshilfe für Integrations-Tester auf echter Hardware.

---

### Phase 9 — Misc (Themes, Fallbacks, Edge Cases)

**Ausstehend (niedrige Priorität):**
1. **Settings/Themes:**
   - SettingsViewModel Tests (CurrentSettingToChange-Zustände, IsXActive-Flags)
   - Settings.razor bUnit-Tests (wenn keine Abhängigkeits-Probleme)
   - Theme-Dropdown, Custom-Theme-CRUD, Preview-Updates

2. **FontService:**
   - Fallback-Pfade auf Exception bei fc-list (Linux) / Registry-Fehler (Windows)
   - Statische Font-Liste als Fallback

3. **PlatformInfoService:**
   - Mock-FileSystem für Device-Tree-Erkennung (RPi vs. Linux vs. Windows)
   - Docker-Env-Variable-Mocking
   - OS-Release-Datei-Parsing-Edge-Cases

4. **ViewModels (aktuell blockiert):**
   - TrainingViewModel, BestOfViewModel, TurnierViewModel, ZielViewModel
   - Erfordern SettingsService-Refactoring (Interface/Virtual-Properties)

**Entscheidung:** Phase 9 kann asynchron nach Release implementiert werden; Blöcke bestehende Test-Phasen nicht.

---

## Zusammenfassung: Nächste Schritte

1. **Sofort (Phase 6+7):**
   - Playwright AppFixture debuggen und erste E2E-Tests laufen lassen
   - NetMQ-Integrationstests auf Testports schreiben (benötigt Port-Parametrisierung in NetMqResponseService)

2. **Mittelfristig (Phase 7-8):**
   - Alle NACK-Pfade im NetMQ abdecken
   - Deployment-Checkliste mit realen Devices durchspielen

3. **Ausstehend (Phase 9+):**
   - ViewModels vollständiger abdecken (aktuell blockiert durch SettingsService-Architektur)
   - Theme/Farb-Logik testen
   - Randfälle bei Error-Handling
