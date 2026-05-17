# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Projektübersicht

**StockTV2** ist eine Blazor Server-Applikation als **Punkteanzeige und Eingabeterminal** für den Stocksport. Pro Spielbahn läuft eine Instanz der App. Die Anzeige wird auf einem TV dargestellt, die Eingabe primär über Numpad oder alternativ über `/input` auf einem Tablet.

Ein zentrales Verwaltungsprogramm verbindet sich über NetMQ, setzt Teamdaten und empfängt Ergebnisse.

---

## Build & Entwicklung

```powershell
# Lokaler .NET-Build (Fehlerprüfung)
dotnet build StockTvBlazor/StockTvBlazor.csproj

# App lokal starten
dotnet run --project StockTvBlazor/StockTvBlazor.csproj

# Tests
dotnet test BlazorAppTests/BlazorAppTests.csproj
```

```bat
# Docker-Image bauen (linux/amd64)
build\buildproject.bat
```

```powershell
# Remote-Deployment auf Server 'csl' (4 Container: stocktvBahn1–4)
build\remotebuild_std.ps1
```

---

## Tech-Stack

- **Framework**: ASP.NET Core 10, Blazor Server (Interactive Server Components)
- **Netzwerk**: NetMQ (ZeroMQ), Makaretu.Dns (mDNS)
- **UI**: Bootstrap, responsive Text via `wwwroot/js/autofitText.js` (`stockTvAutoFit.observe`)
- **Deployment**: Docker (multi-stage), Base Image `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`, Ziel-Plattform Linux/amd64
- **Volumes**: `./_config:/app/_config`, `./_logs:/app/_logs`

---

## Projektstruktur

```
StockTV2/
├── StockTvBlazor/
│   ├── Components/
│   │   ├── Pages/
│   │   │   ├── SettingPages/   # Settings, CustomThemePage, ThemePreview, ColorField
│   │   │   ├── HomeCards/      # CardDisplay1–4 (rotieren auf der Home-Seite)
│   │   │   └── ...             # Training, Turnier, BestOf, Ziel, Input, Home
│   │   ├── Controls/           # PunkteEingabe, PunkteAnzeige, PunkteeingabePassiv
│   │   ├── ViewModels/         # ViewModels pro Modus (erben von BaseViewModel)
│   │   └── Layout/             # MainLayout, ThemeHandler
│   ├── Models/                 # Game, Match, Turn, Begegnung, ZielBewerb, Debounce
│   ├── Networking/             # NetMqPublisherService, NetMqResponseService, MdnsDiscoveryService
│   ├── Services/               # MatchService, ZielService, SettingsService, FileLogger
│   ├── Settings/               # Settings, GameSettings, UiSettings, ColorSettings, Themes
│   └── wwwroot/js/autofitText.js
├── BlazorAppTests/
└── build/                      # Dockerfile, docker-compose.yml, Build-Skripte
```

---

## Spielmodi (`GameSettings.Modus`)

| Modus    | Wert | MaxKehren | MaxPunkte | Beschreibung |
|---------|------|-----------|-----------|--------------|
| Training | 0   | 30        | 15        | Freies Spiel, keine Spielzählung |
| BestOf   | 1   | 6         | 10        | Mehrere Spiele pro Match |
| Turnier  | 2   | 6         | 10        | Wie BestOf, mit Teamnamen (extern gesetzt) |
| Ziel     | 100 | konfig.   | konfig.   | Zielbewerb mit 4 fixen Disziplinen |

### Ziel-Modus (4 fixe Disziplinen, Reihenfolge fix)
1. **MassenVorne** — gültige Werte: 0, 2, 4, 6, 8, 10
2. **Schiessen** — gültige Werte: 0, 2, 5, 10
3. **MassenSeite** — gültige Werte: 0, 2, 4, 6, 8, 10
4. **Kombinieren** — gültige Werte: 0, 2, 4, 6, 8, 10

Pro Disziplin werden `MaxKehrenProSpiel` Versuche eingegeben. Ungültige Werte werden 1,5 Sekunden angezeigt.

---

## UI-Seiten & Navigation

| URL       | Beschreibung |
|-----------|-------------|
| `/`       | Home (Countdown ~10 Sek., dann → aktiver Modus) |
| `/training` | Anzeige Training-Modus |
| `/turnier`  | Anzeige Turnier-Modus |
| `/bestof`   | Anzeige BestOf-Modus |
| `/ziel`     | Anzeige Ziel-Modus |
| `/input`    | Keypad-Seite (Tablet), zeigt aktiven Modus als iframe |
| `/settings` | Einstellungsseite (nur über Geheimtaste erreichbar) |
| `/themes`   | Theme-Verwaltung (Custom Themes erstellen/bearbeiten) |

**Home im Debug-Modus**: Öffnet automatisch mehrere Tabs (LayoutTest, training, turnier, bestof, input, settings, themes).

---

## Services & Architektur

### Singletons
- `SettingsService` — Einstellungen laden/speichern (via asynchronen `Channel`-Queue, nie direkt schreiben), Navigation
- `MatchService` — aktuelles Spiel, Eingabe-Verarbeitung
- `ZielService` — Zielbewerb-Logik
- `NetMqPublisherService` — PUB-Socket Port 4748
- `NetMqResponseService` — REP-Socket Port 4747

### Transient ViewModels
`TurnierViewModel`, `TrainingViewModel`, `BestOfViewModel`, `ZielViewModel` erben von `BaseViewModel`, abonnieren Events und müssen in `Dispose()` abgemeldet werden.

### Events-Muster
```
OnSettingsChanged       — Settings geändert
OnMatchChanged          — Spielstand geändert
OnZielBewerbChanged     — Zielbewerb geändert
OnGlobalRefresh         — UI neu rendern
OnNavigationRequested   — Navigation zu URL
```

### NetMQ-Callbacks
Laufen auf dem Poller-Thread → State-Änderungen **immer** über `_actionChannel` delegieren, nicht direkt aufrufen.

---

## Netzwerk & Ports

| Port | Protokoll      | Zweck |
|------|---------------|-------|
| 8080 | HTTP          | Blazor Web UI |
| 4747 | NetMQ REP/REQ | Kommandos vom zentralen System |
| 4748 | NetMQ PUB/SUB | Ergebnis-Broadcasts (bei jeder Eingabe + alle 5 Sek. Alive) |

**NetMQ-Topics (4747):** `Hello`, `GetResult`, `ResetResult`, `GetSettings`, `SetSettings`, `SetTeamNames` (`"Spielnr:TeamA:TeamB;..."`), `SetTeilnehmer`

**mDNS:** Service-Typ `_stockTV._tcp.`, TXT-Records `pubSvc=4748`, `ctrSvc=4747`, `pkgVer=<Version>`. `PUBLIC_HOST` Env-Variable überschreibt die IP im Alive-Paket.

---

## Eingabe-Logik

| Taste(n)                        | Aktion |
|---------------------------------|--------|
| `0`–`9`, numpad                 | Zahl eingeben |
| `*`                             | Punkte Grün zuweisen |
| `/`, `Backspace`                | Punkte Rot zuweisen |
| `+`                             | Kehre bestätigen / Reset |
| `-`                             | Letzte Kehre löschen |
| `Enter` (5× bei Eingabe = 0)    | Einstellungsseite öffnen |

**Richtung:** `Links` → Grün = rechte Seite; `Rechts` → Grün = linke Seite.
**Debounce:** Schnelles Mehrfach-Drücken wird gefiltert (Ausnahme: `-` bei Eingabe = 0).
**BlockLocalChanges:** Wenn `true`, werden lokale Tastatureingaben ignoriert.

---

## Themes & CSS-Variablen

`ThemeHandler` (in `Layout/`) injiziert ein `<style @key="_updateCounter">` mit CSS-Variablen in den DOM, die sich bei jedem `OnSettingsChanged` aktualisieren:

```css
--bg-color, --fg-color, --fg-left, --fg-right,
--fg-color-ziel-gesamt, --fg-color-ziel-einzel
```

- Built-in Themes: `Hell` (ID `…0001`), `Dunkel` (ID `…0002`)
- Custom Themes via `JsonThemeRepository` persistiert, aktives Theme per GUID in `UiSettings.ActiveThemeId`
- `ColorSettings` = aktives Theme + aktuelle Richtung

---

## Responsives Text-Sizing (autofitText.js)

`stockTvAutoFit.observe(containerSelector)` wird in `OnAfterRenderAsync` auf **jedes** Render aufgerufen (nicht nur `firstRender`). Das JS registriert `ResizeObserver` + `MutationObserver` per Element (WeakMap, kein Doppel-Register) und berechnet per Binary Search die maximale Schriftgröße via `scrollWidth`/`scrollHeight <= clientWidth`/`clientHeight`.

HTML-Attribute zur Steuerung:
- `data-autofit` — aktiviert AutoFit auf dem Element
- `data-autofit-min="10"` — minimale Schriftgröße in px
- `data-autofit-max="300"` — überschreibt die berechnete Obergrenze
- `data-autofit-vertical="true"` — für `writing-mode: vertical-*` Elemente

---

## Datenspeicherung

- **Config**: `_config/stocktv.config.json` (relativ zum App-Verzeichnis)
- **Logs**: `_logs/`
- Speicherung über `Channel`-Queue im `SettingsService` — nie direkt auf `CurrentSettings` schreiben
- Im Training-Modus werden Kehren **nicht** persistiert
- `SaveTurnsAsync` / `RequestSaveSettings` nach State-Änderungen aufrufen

---

## Blazor Komponenten-Dateistruktur

Jede Komponente wird auf **drei Dateien** aufgeteilt:

| Datei | Inhalt |
|---|---|
| `Komponente.razor` | HTML-Template (`@page`, `@using`, Markup) — kein `@code`, kein `<style>` |
| `Komponente.razor.cs` | C# Code-behind als `partial class` |
| `Komponente.razor.css` | Scoped CSS, flache Selektoren |

Namespace entspricht dem Ordner-Pfad:
```csharp
namespace StockTvBlazor.Components.Pages;            // Pages/
namespace StockTvBlazor.Components.Pages.SettingPages; // Pages/SettingPages/
namespace StockTvBlazor.Components.Controls;         // Controls/
namespace StockTvBlazor.Components.Layout;           // Layout/
```

**Ausnahme:** `Home.razor.cs` verwendet `HomeBase : ComponentBase` (Vererbung statt partial), weil die Home-Seite keinen `@rendermode` hat und eine eigene Basisklasse nutzt.

Event-Handler in `Dispose()` immer abmelden.
