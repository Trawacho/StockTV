# Plan: Neuer Spielmodus "Special1"

> Status: **Nur Planung** – Umsetzung erfolgt später. Dieses Dokument dient als Grundlage für die
> Implementierung und listet offene Fragen, die vorher geklärt werden sollten.

## 1. Spielregeln (wie vom User beschrieben)

- Es gibt pro Mannschaft eine "Quote", die nur die Werte `0`, `6`, `9`, `12` annehmen kann.
- Jeder gültige Zieher (Treffer) einer Mannschaft schaltet deren Quote einen Schritt weiter:
  - 1. Zieher → Quote `6` (entspricht "1")
  - 2. Zieher → Quote `9` (entspricht "2")
  - 3. Zieher → Quote `12` = **AUS** (entspricht "3")
- Die Mannschaft, die zuerst `12` (also 3 Treffer) erreicht, gewinnt das Spiel und bekommt
  **1 Spielpunkt**.
- Nach Spielende wird die Quote beider Mannschaften wieder auf `0:0` zurückgesetzt, die
  Spielpunkte (z.B. `1:0`) bleiben über das Match hinweg erhalten (wie bei BestOf/Turnier).
- **Keine Netzwerkübertragung** für diesen Modus (kein `Publish("GetResult", …)`, siehe Abschnitt 5).

### Offene Fragen (vor Implementierung klären)

1. **Eingabe-Logik:** Bei BestOf wird über Numpad ein numerischer Punktewert (0–15) eingegeben und
   per `*`/`/` einer Seite zugewiesen. Bei Special1 gibt es aber keine "Punktzahl", sondern nur
   "Treffer ja/nein" pro Zieher. Soll die Eingabe weiterhin über `*` (Grün) / `/` (Rot) erfolgen,
   einfach ohne vorherige Zahleneingabe (Taste drücken = ein Treffer für die jeweilige Seite)?
   Oder soll wie bisher eine Zahl eingegeben werden und nur `6`, `9`, `12` als gültige Werte
   akzeptiert werden (analog zur Validierung im Ziel-Modus)?
2. **Was passiert bei einem ungültigen Zustand**, z.B. wenn eine Mannschaft bereits `12` (AUS) hat
   und trotzdem nochmal `*`/`/` gedrückt wird, bevor `+` (Reset/Spielende) ausgelöst wurde? Ignorieren,
   oder wie im Ziel-Modus 1,5 Sek. "ungültig" anzeigen?
3. **Kehren-Zählung / Kehren-Limit:** Bestehende Modi nutzen `MaxKehrenProSpiel`. Bei Special1 endet
   ein Spiel nicht nach einer festen Kehrenzahl, sondern sobald eine Seite `12` erreicht. Braucht es
   trotzdem ein Kehren-Limit als Fallback (z.B. falls beide Seiten nie `12` erreichen)?
4. **Unentschieden möglich?** Aktuell gibt es bei BestOf ein Unentschieden (`GamePoints = 1:1`), wenn
   beide Seiten am Kehrenlimit gleich viele Punkte haben. Bei Special1 scheint das Spiel erst mit
   einem eindeutigen Sieger (12 erreicht) zu enden – ein Unentschieden ist also vermutlich nicht
   vorgesehen. Bitte bestätigen.
5. **Anzeige "-" (letzte Kehre löschen):** Soll das einen Schritt zurücksetzen (12→9→6→0)?
6. **Persistenz:** Sollen die "Kehren" (Zieher) für Special1 wie bei BestOf/Turnier gespeichert
   werden (`SaveTurnsAsync`), oder wie bei Training gar nicht persistiert werden?
7. **Enum-Wert für `GameSettings.Modus.Special1`:** Vorschlag `3` (nächster freier Wert nach
   `Turnier = 2`, siehe Abschnitt 4). Da die Settings-Seite per generischer `Next()/Previous()`-
   Erweiterung (`Extensions/StructExtension.cs`) durch alle Enum-Werte in Deklarationsreihenfolge
   blättert, bestimmt die Position im Enum auch die Reihenfolge im Moduswechsel auf der
   Settings-Seite.

## 2. Betroffene Komponenten (Kopie von BestOf)

Analog zur bestehenden Drei-Datei-Struktur (`.razor` / `.razor.cs` / `.razor.css`):

- `Components/Pages/Special1.razor` (`@page "/special1"`) – Kopie von `BestOf.razor`, Anzeige der
  Quote (`6/9/12`) statt der Punktesumme.
- `Components/Pages/Special1.razor.cs` – Kopie von `BestOf.razor.cs` (Keydown-Handling über
  `MatchService.ProcessKeyAsync`, Navigation-Events).
- `Components/Pages/Special1.razor.css` – ggf. wiederverwendbar aus `BestOf.razor.css`, da gleiches
  Grid-Layout (Teamname links/rechts, Mittelspalte mit Quote + Spielpunkten).
- `Components/ViewModels/Special1ViewModel.cs` – Kopie/Ableitung von `BestOfViewModel`, aber mit
  eigener Logik für die Quote-Anzeige (`6/9/12` statt Punktesumme aus `Turns`).

## 3. Spiellogik – Modell-Ebene

Die aktuelle `Game`/`Match`-Logik basiert auf `Turn.PointsLeft`/`PointsRight` als beliebige
Zahlenwerte, die aufsummiert werden (`LeftPointsSum`, `GamePointsLeft` bei Kehrenlimit-Erreichen).
Für Special1 passt dieses Modell nicht 1:1, da:

- Der Spielgewinn nicht von der Summe, sondern vom Erreichen eines Zustands (`12`) abhängt.
- Ein Spiel eine variable Anzahl an Kehren dauert (endet, sobald eine Seite `12` erreicht).

**Optionen:**

- **(a)** Eigene Auswertungslogik in `Game` ergänzen (z.B. `internal bool IsSpecial1;` Flag +
  angepasste `GamePointsLeft`/`GamePointsRight`-Berechnung: Sieg sobald `Turns.Count(t => t.PointsLeft > 0)`
  bzw. die Anzahl gewerteter Treffer 3 erreicht, unabhängig vom Kehrenlimit), plus Anpassung in
  `Match.Reset()`, wann ein neues `Game` begonnen wird (aktuell an `MaxKehrenProSpiel` gekoppelt).
- **(b)** Eigenes, separates Modell (kein `Game`/`Turn`-Reuse), dafür komplett getrennte Logik –
  mehr Aufwand, aber kein Risiko, bestehende Modi (BestOf/Turnier) zu beeinflussen.

→ Empfehlung: **(a)**, sofern die Turn-basierte Struktur (Kehren-Liste, `AddTurn`/`DeleteLastTurn`)
für die spätere Auswertung/Historie sinnvoll ist. Details hängen von den offenen Fragen in
Abschnitt 1 ab.

## 4. Anpassungen an bestehenden Dateien

| Datei | Änderung |
|---|---|
| `Settings/GameSettings.cs` | Neuer Enum-Wert `Special1` in `Modus` |
| `Services/SettingsService.cs` | `GetModusUrl()` → Mapping auf `/special1`; `ChangeModus()` → ggf. eigene `MaxKehrenProSpiel`/`MaxPunkteProKehre`-Defaults; Prüfung an allen Stellen, die `Modus.Training`/`Modus.Ziel` unterscheiden (Zeile ~114, ~196, ~335) |
| `Models/Match.cs` | `Reset()` – Bedingung für "neues Spiel bei Kehrenlimit" (Zeile ~115) um `Special1` erweitern bzw. eigene Bedingung (Sieg bei Quote 12) |
| `Networking/NetMqResponseService.cs` | Prüfen, ob/wie `GetSettings`/`SetSettings` mit dem neuen Modus umgehen müssen (aktuell Sonderbehandlung nur für `Ziel`/`Ziel2`) |
| `Components/Pages/Input.razor.cs` | iframe-Sonderbehandlung (aktuell nur `Ziel`/`Ziel2`, Zeile 93) prüfen, ob Special1 eine eigene Eingabe-Seite braucht oder das normale Numpad-Verhalten reicht |
| `Components/Pages/SettingPages/ThemePreview.razor.cs` | Neuer Modus muss in der Theme-Vorschau auswählbar sein |
| `Components/Pages/Home*` / `HomeCards` | Falls Home-Seite je nach Modus rotiert/anzeigt, ggf. `CardDisplay` für Special1 ergänzen |
| `Program.cs` | Ggf. DI-Registrierung für `Special1ViewModel` (transient, analog zu `BestOfViewModel`) |
| `todo.md` | Eintrag für diesen Modus (siehe unten) |

## 5. Keine Netzwerkübertragung für Special1

`MatchService.ProcessKeyAsync()` ruft aktuell **immer** am Ende
`_publisherService.Publish("GetResult", CurrentMatch.SerializeJson())` auf (`Services/MatchService.cs:105`),
unabhängig vom Modus. Für Special1 (und laut separatem Todo-Punkt auch für Training) muss das
unterbunden werden – vermutlich durch eine Prüfung auf `CurrentSettings.Game.CurrentModus`
unmittelbar vor dem `Publish`-Aufruf. Siehe separaten Todo-Punkt "Training-Modus: keine
Netzwerkübertragung".

## 6. Vorgeschlagene Umsetzungsreihenfolge (für später)

1. Offene Fragen (Abschnitt 1) mit dem User klären.
2. `GameSettings.Modus.Special1` + Routing (`GetModusUrl`) ergänzen.
3. Modell-Logik für Quote/Sieg-Erkennung (Abschnitt 3) umsetzen + minimal testen (`BlazorAppTests`).
4. `Special1.razor` + `.razor.cs` + `.razor.css` + `Special1ViewModel` als Kopie von BestOf anlegen,
   Anzeige auf Quote (`6/9/12`) umstellen.
5. Publisher-Unterdrückung für Special1 (und Training) einbauen.
6. Settings-Seite / Theme-Vorschau / Input-Seite testen (Moduswechsel, Keypad, TV-Anzeige).
