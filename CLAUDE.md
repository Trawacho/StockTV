# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Wichtige Verhaltensregeln

- **Branching**: Niemals direkt in `main` oder `release/*` committen. Jede Änderung erfolgt in einem eigenen `feature/*`- oder `hotfix/*`-Branch. Die vollständige Branching- und Release-Strategie ist in [CONTRIBUTING.md](CONTRIBUTING.md) beschrieben — diese Regeln sind verbindlich.
- **Commits**: Nur auf explizite Aufforderung des Users committen oder pushen.

## Projektübersicht

**StockTV** ist eine Blazor Server-Applikation als **Punkteanzeige und Eingabeterminal** für den Stocksport. Pro Spielbahn läuft eine Instanz der App. Die Anzeige wird auf einem TV dargestellt, die Eingabe primär über Numpad oder alternativ über `/input` auf einem Tablet.

Ein zentrales Verwaltungsprogramm verbindet sich über NetMQ, setzt Teamdaten und empfängt Ergebnisse.

---

## Build & Entwicklung

```powershell
# Lokaler .NET-Build (Fehlerprüfung)
dotnet build StockTvBlazor/StockTvBlazor.csproj

# App lokal starten
dotnet run --project StockTvBlazor/StockTvBlazor.csproj
```

```bat
# Docker-Image bauen (linux/amd64)
build\buildproject.bat
```

```powershell
# Remote-Deployment auf Server 'csl' (4 Container: stocktvBahn1–4)
build\remotebuild_std.ps1

# Raspberry Pi: bauen + Release-Zip / direkt deployen
build\rpi\publish-rpi.ps1
build\rpi\publish-rpi.ps1 -PiHost 192.168.1.xx -Install   # Erstinstallation

# Windows x64: bauen + Release-Zip / direkt deployen (WinRM)
build\windows\publish-windows.ps1
build\windows\publish-windows.ps1 -TargetHost 192.168.1.xx -Install

# Linux x64: bauen + Release-Zip / direkt deployen (SSH)
build\linux\publish-linux.ps1
build\linux\publish-linux.ps1 -TargetHost 192.168.1.xx -Install
```

Details zu den Plattform-Skripten und dem GitHub Release-Prozess: siehe [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Tech-Stack

- **Framework**: ASP.NET Core 10, Blazor Server (Interactive Server Components)
- **Netzwerk**: NetMQ (ZeroMQ), Makaretu.Dns (mDNS)
- **UI**: Bootstrap, responsive Text via `wwwroot/js/autofitText.js` (`stockTvAutoFit.observe`)
- **Deployment**: Docker (Linux/amd64), Raspberry Pi (linux-arm64, Kiosk), Windows Service (`UseWindowsService()` in `Program.cs`), Linux x64 (systemd)
- **Volumes / Datenpfade**: `./_config:/app/_config`, `./_logs:/app/_logs` (relativ zum App-Verzeichnis, auf allen Plattformen gleich)

---

## Projektstruktur

```
StockTV/
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
├── BlazorAppTests/             # Temporäres Blazor-Testprojekt (kein xUnit, nicht für automatisierte Tests)
├── build/
│   ├── rpi/                    # Raspberry Pi: publish-rpi.ps1, build-image.sh, install.sh
│   ├── windows/                # Windows x64: publish-windows.ps1, install-service.ps1
│   ├── linux/                  # Linux x64: publish-linux.ps1, install.sh
│   ├── Dockerfile              # Multi-stage Docker Build (linux/amd64)
│   └── docker-compose.yml
└── .github/workflows/
    └── release.yml             # Unified Release-Workflow (alle 3 Plattformen parallel)
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
- `SettingsService` — Einstellungen laden/speichern (via asynchronen `Channel`-Queue, nie direkt schreiben), Navigation. Läuft als `HostedService`.
- `MatchService` — aktuelles Spiel, Eingabe-Verarbeitung
- `ZielService` — Zielbewerb-Logik
- `FontService` — Verwaltung von System-Schriftarten (geladen aus lokalen Fonts)
- `NetMqPublisherService` — PUB-Socket Port 4748, läuft als `HostedService`
- `NetMqResponseService` — REP/REQ-Socket Port 4747, läuft als `HostedService`

### Transient ViewModels
`TurnierViewModel`, `TrainingViewModel`, `BestOfViewModel`, `ZielViewModel`, `SettingsViewModel` erben von `BaseViewModel`, abonnieren Events und müssen in `Dispose()` abgemeldet werden.

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

## Konfiguration & Umgebung

### Konfigurationsdatei (`_config/stocktv.config.json`)

| Sektion | Einstellung | Bedeutung |
|---------|-------------|-----------|
| `General.FileLoggingEnabled` | `true/false` | Protokollierung in `_logs/` aktivieren |
| `General.BahnNummer` | `1–4` | Bahnnummer für mDNS und externe Verwaltung |
| `General.Spielgruppe` | Zahl | Spielgruppen-ID (extern gesetzt) |
| `General.MessageVersion` | `1` | Protokoll-Version für NetMQ |
| `Game.CurrentModus` | `0/1/2/100` | Training / BestOf / Turnier / Ziel |
| `Game.MaxPunkteProKehre` | Zahl | Max. Punkte je Kehre (Standard: 15) |
| `Game.MaxKehrenProSpiel` | Zahl | Max. Kehren je Spiel (Standard: 30) |
| `UI.CurrentRichtung` | `0/1` | Spielrichtung: 0=Links, 1=Rechts |
| `UI.MidColumnWidth` | Zahl | Breite der Mittelspalte (% oder px) |
| `UI.ActiveThemeId` | GUID | Aktives Theme (per UUID verlinkt) |
| `UI.CustomThemes` | Array | Benutzerdefinierte Themes mit Farben |
| `Network.Enabled` | `true/false` | NetMQ-Netzwerk aktivieren |

### Umgebungsvariablen

| Variable | Zweck | Beispiel |
|----------|-------|---------|
| `PUBLIC_HOST` | IP-Adresse im mDNS Alive-Paket (überschreibt die automatisch erkannte IP) | `192.168.1.10` |
| `ASPNETCORE_URLS` | HTTP-Bindungsadresse | `http://0.0.0.0:8080` |
| `ASPNETCORE_ENVIRONMENT` | Umgebung für `appsettings.{Environment}.json` | `Development` oder `Production` |

---

## Datenspeicherung & Logging

- **Config**: `_config/stocktv.config.json` (relativ zum App-Verzeichnis, wird beim Start geladen)
- **Logs**: `_logs/` (JSON-Dateien mit Timestamps, nur wenn `FileLoggingEnabled=true`)
- **Speicherung**: Via `Channel`-Queue im `SettingsService` — **nie direkt** auf `CurrentSettings` schreiben
- Im Training-Modus werden Kehren **nicht** persistiert
- `SaveTurnsAsync()` / `RequestSaveSettings()` nach State-Änderungen aufrufen
- Logging-Level in `appsettings.json`: Microsoft.AspNetCore auf `Warning`, default auf `Information`

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

---

## Service-Initialisierung & Lifecycle

Die App folgt einem **zweistufigen** Initialisierungs-Prozess in `Program.cs`:

1. **Service-Registrierung** — alle Singletons und ViewModels in DI-Container
2. **Nach `app.Build()`** — `SettingsService.InitializeAsync()`, `MatchService.InitializeMatch()`, `ZielService.InitializeZiel()` aufrufen (synchron auf dem main-Thread vor `app.Run()`)

Wichtig: `SettingsService` ist gleichzeitig `IHostedService` — das bedeutet, dass Einstellungen asynchron geladen werden, aber die synchrone Init sorgt dafür, dass die UI beim Rendern schon Daten hat.

**SettingsService-Pattern**: Änderungen immer über `RequestSaveSettings()` oder `SaveTurnsAsync()` einleiten, nicht direkt `CurrentSettings` mutieren. Der Service nutzt eine `Channel`-Queue um Schreibzugriffe zu serialisieren.

---

## Entwicklung & Debugging

### Lokale Entwicklung

```powershell
# Abhängigkeiten prüfen
dotnet restore StockTvBlazor/StockTvBlazor.csproj

# Debug-Build und Start (mit Hot Reload)
dotnet watch run --project StockTvBlazor/StockTvBlazor.csproj
```

App läuft dann auf `https://localhost:5001` oder konfiguriert via `appsettings.Development.json`.

### Debugging im Browser

- **Chrome/Edge DevTools** — öffne F12, Tab "Network" um WebSocket (`/blazor?id=…`) zu prüfen
- **Circuit Disconnect** — wenn die WebSocket trennt, sieht man Fehler auf der Seite; Logs im `_logs/` Ordner prüfen
- **Hot Reload** — wenn Code geändert wird (außer `Program.cs`), lädt Blazor automatisch neu

### NetMQ-Debugging

NetMQ läuft auf eigenem `Poller`-Thread. Bei State-Änderungen von außen (z.B. `SetSettings` vom zentralen System):

1. **Callback wird auf Poller-Thread aufgerufen**
2. **State-Änderung muss über `_actionChannel` delegiert werden** (nicht direkt `MatchService.UpdateMatch()` aufrufen)
3. **Main-Thread verarbeitet die Änderung** und triggert Events
4. **UI wird via Blazor WebSocket benachrichtigt**

→ Wenn die UI nicht aktualisiert wird, nachdem externe Befehle kommen, ist oft der Fehler, dass die State-Änderung nicht über den Channel lief.

### Troubleshooting

| Problem | Ursache | Lösung |
|---------|--------|--------|
| Einstellungen werden nicht gespeichert | Direkter Zugriff auf `CurrentSettings` statt `RequestSaveSettings()` | `SettingsService.RequestSaveSettings()` aufrufen |
| WebSocket trennt häufig | Circuit timeout oder Netzwerk-Problem | Logs in `_logs/` prüfen, ggf. `Logging.LogLevel` auf `Debug` setzen |
| UI aktualisiert sich nicht nach NetMQ-Befehl | State-Änderung nicht über `_actionChannel` | Callback-Code prüfen, muss `_actionChannel.Writer.WriteAsync()` nutzen |
| Theme-Farben ändern sich nicht | `OnSettingsChanged` Event nicht abonniert | ViewModel muss in `Dispose()` unsubscribe aufrufen |

---

## Git Workflow

Siehe [CONTRIBUTING.md](CONTRIBUTING.md) für vollständige Anleitung. Kurz zusammengefasst:

**Branch-Struktur:**
```
main (nur Releases)
  ← release/v1.0 (Tag v1.0.0)
    ← develop (Integration)
      ← feature/* (neue Features)
```

**Workflows:**
- **Feature**: `git checkout -b feature/kurzbeschreibung` von `develop`, Merge zu `develop` mit `--no-ff`
- **Release**: `develop` → `release/vX.Y`, dann `release/vX.Y` → `main`, Tag setzen (z.B. `git tag v1.0.0`), `release/vX.Y` → `develop` zurück (wichtig!)
- **Hotfix**: `git checkout -b hotfix/kurzbeschreibung` von `release/vX.Y`, Merge zu `release/vX.Y`, Tag (`v1.0.1`), dann zu `main` **und `develop`** (verhindert Divergence)

⚠️ **Wichtig:** Nach jedem Merge von `release/vX.Y` immer auch zu `develop` zurück mergen!

---

## Testprojekt & Manuelle Tests

`BlazorAppTests/` ist ein **interaktives Testprojekt**, keine automatisierte Test-Suite:
- Dient zum **manuellen Testen** von Komponenten in Isolation (`LayoutTest`, `HomeCards`, usw.)
- Im Debug-Modus öffnet die Home-Seite automatisch alle Test-Tabs
- **Nicht** für xUnit / Automatisierung gedacht (würde zu viele Blazor-Komplexitäten mitschleppen)
