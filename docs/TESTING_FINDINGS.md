# Testing Findings — StockTV

Dokumentation aller während der Testplanung aufgefundenen Verdachtsfälle (potenzielle Bugs). Jeder Eintrag wird vor der Testschreibung bewertet: Beheben oder als bekanntes Verhalten dokumentieren?

## Testplan-Notiz: Match & ZielBewerb Tests

**Match & ZielBewerb gehören NICHT zu Phase 2 (Unit-Tests)**, sondern zu Phase 3 (Integration/E2E):
- **Grund:** Match ist stark an SettingsService gekoppelt (kein Dependency Injection auf Service-Level)
- **Unit-Tests würden Mocking benötigen,** aber `SettingsService.CurrentSettings` ist nicht-virtual
- **Besserer Ansatz:** Phase 3 bUnit-Komponententests (Training/BestOf/Ziel Pages) + Playwright-E2E Tests decken Match/ZielBewerb-Verhalten ab

**Phase 2 bleibt:** nur einfache Models ohne komplexe Dependencies (Debounce, Turn, Begegnung)

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
