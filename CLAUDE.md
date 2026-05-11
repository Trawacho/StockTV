# StockTV2 — CLAUDE.md

## Projektübersicht

**StockTV2** ist eine Blazor Server-Applikation, die als **Punkteanzeige und Eingabeterminal** für den Stocksport dient. Pro Spielbahn läuft eine Instanz der App. Die Anzeige wird auf einem TV dargestellt, die Eingabe erfolgt primär über ein angeschlossenes Numpad oder alternativ über die `/input`-Seite auf einem Tablet.

Ein zentrales Verwaltungsprogramm (externes Tool) verbindet sich über NetMQ, setzt Teamdaten, und empfängt Ergebnisse.

---

## Tech-Stack

- **Framework**: ASP.NET Core 10, Blazor Server (Interactive Server Components)
- **Laufzeit**: .NET 10
- **Netzwerk**: NetMQ (ZeroMQ), Makaretu.Dns (mDNS)
- **UI**: Bootstrap, Auto-Fit-Text via JS (`stockTvAutoFit.observe`)
- **Deployment**: Docker (multi-stage), docker-compose
- **Ziel-Plattform**: Linux/amd64

---

## Projektstruktur

```
StockTV2/
├── StockTvBlazor/          # Haupt-App (Blazor Server)
│   ├── Components/
│   │   ├── Pages/          # Razor-Pages (Training, Turnier, BestOf, Ziel, Input, Settings)
│   │   ├── Controls/       # Wiederverwendbare Komponenten (PunkteEingabe, PunkteAnzeige)
│   │   ├── ViewModels/     # ViewModels pro Modus (erben von BaseViewModel)
│   │   └── Layout/
│   ├── Models/             # Game, Match, Turn, Begegnung, ZielBewerb, Debounce
│   ├── Networking/         # NetMqPublisherService, NetMqResponseService, MdnsDiscoveryService
│   ├── Services/           # MatchService, ZielService, SettingsService, FileLogger
│   └── Settings/           # Settings, GameSettings, UiSettings, GeneralSettings, NetworkSettings, Themes
├── BlazorAppTests/         # Test-Projekt (wird für automatisierte Tests ausgebaut)
└── build/                  # Dockerfile, docker-compose.yml, Build-Skripte
```

**Ignorieren:** `ScoreBoard.razor` ist ein früher Prototyp und wird entfernt.

---

## Spielmodi (`GameSettings.Modus`)

| Modus        | Wert | MaxKehren | MaxPunkte | Beschreibung |
|-------------|------|-----------|-----------|--------------|
| Training    | 0    | 30        | 15        | Freies Spiel, keine Spielzählung |
| BestOf      | 1    | 6         | 10        | Mehrere Spiele pro Match |
| Turnier     | 2    | 6         | 10        | Wie BestOf, mit Teamnamen (extern gesetzt) |
| Ziel        | 100  | konfigurierbar | konfigurierbar | Zielbewerb mit 4 fixen Disziplinen |

### Ziel-Modus (4 fixe Disziplinen, Reihenfolge fix)
1. **MassenVorne** — gültige Werte: 0, 2, 4, 6, 8, 10
2. **Schiessen** — gültige Werte: 0, 2, 5, 10
3. **MassenSeite** — gültige Werte: 0, 2, 4, 6, 8, 10
4. **Kombinieren** — gültige Werte: 0, 2, 4, 6, 8, 10

Pro Disziplin werden `MaxKehrenProSpiel` Versuche eingegeben. Ungültige Werte werden abgelehnt (1,5 Sekunden Fehlermeldung).

---

## Datenspeicherung

- **Config-Datei**: `_config/stocktv.config.json` (relativ zum App-Verzeichnis)
- **Logs**: `_logs/` (relativ zum App-Verzeichnis)
- Speicherung läuft über einen asynchronen `Channel`-Queue im `SettingsService` (kein direktes Schreiben)
- Im **Training-Modus** werden Kehren **nicht** persistiert

---

## Netzwerk & Ports

| Port | Protokoll       | Zweck |
|------|----------------|-------|
| 8080 | HTTP           | Blazor Web UI |
| 4747 | NetMQ REP/REQ  | Kommandos vom zentralen System |
| 4748 | NetMQ PUB/SUB  | Ergebnis-Broadcasts (bei jeder Eingabe + alle 5 Sek. Alive) |

### NetMQ-Topics (Port 4747, Request/Response)
- `Hello` → `Welcome`
- `GetResult` → Settings-Bytes + JSON-Spielstand
- `ResetResult` → Spiel zurücksetzen
- `GetSettings` → Settings-Byte-Array
- `SetSettings` → Settings übernehmen (triggert Navigation)
- `SetTeamNames` → Teamdaten setzen (`"Spielnr:TeamA:TeamB;..."`)
- `SetTeilnehmer` → Spielername für Ziel-Modus

### mDNS
- Service-Typ: `_stockTV._tcp.`
- TXT-Records: `pubSvc=4748`, `ctrSvc=4747`, `pkgVer=<Version>`
- Env-Variable `PUBLIC_HOST` überschreibt die automatisch ermittelte IP/Hostname im Alive-Paket

---

## Eingabe-Logik

### Tasten-Mapping (Numpad & Keyboard)
| Taste(n)                              | Aktion |
|---------------------------------------|--------|
| `0`–`9`, numpad                       | Zahl eingeben |
| `*`                                   | Punkte Grün zuweisen (Enter) |
| `/`, `Backspace`                      | Punkte Rot zuweisen (Enter) |
| `+`                                   | Kehre bestätigen / Reset |
| `-`                                   | Letzte Kehre löschen |
| `Enter` (5×, wenn Eingabe = 0)        | Einstellungsseite öffnen |

### Richtung (`UiSettings.Richtung`)
- `Links`: Grün-Taste → rechte Seite (PointsRight), Rot-Taste → linke Seite (PointsLeft)
- `Rechts`: Grün-Taste → linke Seite (PointsLeft), Rot-Taste → rechte Seite (PointsRight)

### Debounce
Schnelles Mehrfach-Drücken wird via `Debounce`-Klasse gefiltert (Ausnahme: `-` bei Eingabe = 0 für Mehrfachlöschung).

### BlockLocalChanges
Wenn `GeneralSettings.BlockLocalChanges = true`, werden lokale Tastatureingaben ignoriert. Steuerung nur noch über Netzwerk möglich.

---

## UI-Seiten & Navigation

| URL         | Beschreibung |
|-------------|-------------|
| `/`         | Home (Begrüßungsseite, leitet nach ~10 Sek. zum aktiven Modus) |
| `/training` | Anzeige Training-Modus |
| `/turnier`  | Anzeige Turnier-Modus |
| `/bestof`   | Anzeige BestOf-Modus |
| `/ziel`     | Anzeige Ziel-Modus |
| `/input`    | Keypad-Seite (Tablet-Eingabe, zeigt Anzeige-Seite als iframe) |
| `/settings` | Einstellungsseite (nur über Geheimtaste erreichbar) |

**Home-Seite im Debug-Modus**: Öffnet mehrere Tabs (Training, BestOf, LayoutTest) gleichzeitig für schnellen Überblick verschiedener Ansichten.

---

## Services & Architektur

### Singletons (app-weite Zustände)
- `SettingsService` — Einstellungen laden/speichern, Settings-Seite, Navigation
- `MatchService` — aktuelles Spiel (Match, Game, Turn), Eingabe-Verarbeitung
- `ZielService` — Zielbewerb-Logik
- `NetMqPublisherService` — PUB-Socket (Port 4748)
- `NetMqResponseService` — REP-Socket (Port 4747)

### Transient ViewModels
- `TurnierViewModel`, `TrainingViewModel`, `BestOfViewModel`, `ZielViewModel`
- Erben von `BaseViewModel`, abonnieren `OnMatchChanged` und `OnSettingsChanged`
- Werden pro Blazor-Komponenteninstanz erzeugt und müssen in `Dispose()` abgemeldet werden

### Events-Muster
Services kommunizieren über `Action`-Events:
- `OnSettingsChanged` — Settings geändert
- `OnMatchChanged` / `OnZielBewerbChanged` — Spielstand geändert
- `OnGlobalRefresh` — UI soll neu rendern
- `OnNavigationRequested` — Navigation zu einer URL

---

## Themes & Farben

- Built-in Themes: `Hell` (ID `00000000-...-0001`), `Dunkel` (ID `00000000-...-0002`)
- Custom Themes möglich (per `JsonThemeRepository` persistiert)
- Aktives Theme wird per GUID in `UiSettings.ActiveThemeId` gespeichert
- `ColorSettings` werden aus dem aktiven Theme + aktueller Richtung berechnet

---

## Build & Deployment

### Lokaler Dev-Build
```bat
build\buildproject.bat
```
Baut ein `linux/amd64`-Image lokal mit docker buildx.

### Remote-Deployment (PowerShell)
```powershell
build\remotebuild_std.ps1
```
Baut Image → exportiert als `.tar` → SCP zu `daniel@csl:~/composer/` → lädt und startet per SSH → räumt auf.

**Remoter Server:** `csl` (Linux), deployt 4 Container: `stocktvBahn1`–`stocktvBahn4`.

### Docker-Konfiguration
- Base Image: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`
- Ports freigegeben: 8080, 4747, 4748
- Volumes: `./_config:/app/_config`, `./_logs:/app/_logs`
- `PUBLIC_HOST` Env-Variable für korrekte IP in Alive-Nachrichten setzen

---

## Wichtige Konventionen

- **Keine Kommentare** für offensichtlichen Code — nur bei nicht-offensichtlichem Verhalten
- Settings-Änderungen immer über `SettingsService` Methoden, nie direkt auf `CurrentSettings` (außer lesend)
- NetMQ-Callbacks laufen auf dem Poller-Thread → State-Änderungen immer über `_actionChannel` delegieren, nicht direkt aufrufen
- Blazor-Komponenten: Event-Handler in `Dispose()` immer abmelden
- `SaveTurnsAsync` / `RequestSaveSettings` immer nach State-Änderungen aufrufen, die persistiert werden sollen

### Blazor Komponenten-Dateistruktur (Standard)

Jede Blazor-Komponente wird auf **drei Dateien** aufgeteilt:

| Datei | Inhalt |
|---|---|
| `Komponente.razor` | Nur das HTML-Template (`@page`, `@using`, Markup) |
| `Komponente.razor.cs` | C# Code-behind als `partial class` (`[Parameter]`, Event-Handler, Logik) |
| `Komponente.razor.css` | Scoped CSS (Blazor-isoliert, flache Selektoren) |

```csharp
// Komponente.razor.cs
namespace StockTvBlazor.Components.Pages; // oder .Controls / .Layout

public partial class MeineKomponente
{
    [Parameter] public string Wert { get; set; } = "";
}
```

- Kein `@code`-Block in `.razor`-Dateien
- Kein `<style>`-Block in `.razor`-Dateien
- CSS-Selektoren in `.razor.css` immer flach schreiben (keine Einrückung VS-Stil)
