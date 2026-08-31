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

**Wichtig:** [INSTALL.md](INSTALL.md) enthält die vollständige Endanwender-Installationsanleitung (Raspberry Pi + Windows), Inhalte sind dort bewusst ausformuliert statt nur verlinkt. Bei Änderungen an `build/rpi/install.sh`, `build/rpi/install-dual.sh` oder `build/windows/install-service.ps1` (Parameter, Ablauf) muss `INSTALL.md` entsprechend aktualisiert werden.

**Zwei Bildschirme:** Pro Plattform gibt es eine Ein- und eine Zwei-Bildschirm-Variante des Kiosks — Pi: `install.sh` / `install-dual.sh`, Windows: `install-service.ps1` mit bzw. ohne `-DualDisplay` (kopiert `start-kiosk-dual.ps1` über `start-kiosk.ps1`). Die Zwei-Bildschirm-Einrichtung wird per Sentinel `.kiosk-dual` gemerkt, damit ein Update sie nicht stillschweigend entfernt.

---

## Tech-Stack

- **Framework**: ASP.NET Core 10, Blazor Server (Interactive Server Components)
- **Netzwerk**: NetMQ (ZeroMQ), Makaretu.Dns (mDNS), REST auf eigenem Kestrel-Host (Swashbuckle)
- **UI**: Bootstrap, responsive Text **rein per CSS** (Container Queries) über die Komponente `Controls/AutoFitText` + `wwwroot/css/StockTV_AutoFit.css` — **kein eigenes JavaScript**
- **Deployment**: Docker (Linux/amd64), Raspberry Pi (linux-arm64, Kiosk), Windows Service (`UseWindowsService()` in `Program.cs`), Linux x64 (systemd)
- **Volumes / Datenpfade**: `./_config:/app/_config` (drei Dateien, siehe unten), `./_logs:/app/_logs` (relativ zum App-Verzeichnis, auf allen Plattformen gleich)

---

## Projektstruktur

```
StockTV/
├── StockTvBlazor/
│   ├── Api/                    # REST-Schnittstelle: StockTvApiHost, ApiKeyMiddleware, ConfigController
│   ├── Components/
│   │   ├── Pages/
│   │   │   ├── SettingPages/   # Settings, CustomThemePage, ThemePreview, ColorField
│   │   │   ├── HomeCards/      # CardInfo/CardSpielmodi/CardTastenhilfe (rotieren auf Home)
│   │   │   └── ...             # Training, Turnier, BestOf, Ziel, Input, Home
│   │   ├── Controls/           # PunkteEingabe, PunkteAnzeige, PunkteeingabePassiv, AutoFitText
│   │   ├── ViewModels/         # ViewModels pro Modus (erben von BaseViewModel)
│   │   └── Layout/             # MainLayout, ThemeHandler
│   ├── Models/                 # Game, Match, Turn, Begegnung, ZielBewerb, Debounce
│   ├── Networking/             # NetMqPublisherService, NetMqResponseService, MdnsDiscoveryService
│   ├── Services/               # MatchService, ZielService, SettingsService, GameStateStore,
│   │                           #   MarketingImageService, FileLogger
│   ├── Settings/               # Settings, DeviceSettings, GameSettings, UiSettings, ColorSettings, Themes
│   └── wwwroot/css/StockTV_AutoFit.css   # CSS-basierte Textskalierung (kein JS)
├── BlazorAppTests/             # Temporäres Blazor-Testprojekt (kein xUnit, nicht für automatisierte Tests)
├── build/
│   ├── rpi/                    # Raspberry Pi: publish-rpi.ps1, build-image.sh, install.sh, install-dual.sh
│   ├── windows/                # Windows x64: publish-windows.ps1, install-service.ps1, start-kiosk[-dual].ps1
│   ├── linux/                  # Linux x64: publish-linux.ps1, install.sh
│   ├── dev/                    # Entwickler-Helfer: test-dualdisplay.ps1/.bat
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
| Ziel2    | 101 | konfig.   | konfig.   | Wie Ziel, aber zwei Runden (Gesamtsumme = Runde 1 + Runde 2) |

### Ziel-Modus (4 fixe Disziplinen, Reihenfolge fix)
1. **MassenVorne** — gültige Werte: 0, 2, 4, 6, 8, 10
2. **Schiessen** — gültige Werte: 0, 2, 5, 10
3. **MassenSeite** — gültige Werte: 0, 2, 4, 6, 8, 10
4. **Kombinieren** — gültige Werte: 0, 2, 4, 6, 8, 10

Pro Disziplin werden `MaxKehrenProSpiel` Versuche eingegeben. Ungültige Werte werden 1,5 Sekunden angezeigt.

### Ziel2-Modus (Zwei-Runden-Variante von Ziel)
Gleiche 4 Disziplinen wie im Ziel-Modus. Sobald alle `MaxVersucheGesamt` (= `MaxKehrenProSpiel * 4`)
Versuche der ersten Runde abgeschlossen sind, setzt `ZielBewerb.AddVersuch()` die vier
Versuchslisten automatisch zurück, merkt sich die Rundensumme (`_runde1Summe`) und startet
transparent eine zweite Runde mit denselben Disziplinen in gleicher Reihenfolge. `GesamtSumme`
ergibt sich aus Runde 1 + Runde 2; `MaxVersucheDisplay`/`AnzahlVersucheDisplay` verdoppeln sich
entsprechend für die Anzeige. Routing (`/ziel`), die NetMQ-Sonderbehandlung von `GetResult`/
`ResetResult` (→ `ZielService` statt `MatchService`) sowie das `/input`-iframe-Handling sind
identisch zu Ziel — überall dort, wo `Modus.Ziel` geprüft wird, wird `Modus.Ziel2` mitgeprüft.

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
| `/display2` | Zweite Anzeige (gegenüberliegende Bahnseite), zeigt aktiven Modus gespiegelt als iframe |
| `/settings` | Einstellungsseite (nur über Geheimtaste erreichbar) |
| `/themes`   | Theme-Verwaltung (Custom Themes erstellen/bearbeiten) |
| `/marketing` | Werbebild der zentralen Verwaltung (via NetMQ `GoToImage`) |

**Home im Debug-Modus**: Öffnet automatisch mehrere Tabs (LayoutTest, training, turnier, bestof, input, settings, themes).

### Zweite Anzeige (Spiegelung)

An der Bahnmitte hängen zwei Bildschirme, je einer pro Bahnseite. Fenster 1 zeigt die normale
Anzeige, Fenster 2 (`/display2`) dieselben Daten mit vertauschten Spalten — dadurch sieht jede
Seite ihre Mannschaft dort, wo sie steht.

- `/display2` ist ein reiner Wrapper (gleiches Muster wie `/input`): er bettet die aktive Seite
  als iframe mit `?mirror=true` ein und navigiert selbst nie — die Kiosk-URL bleibt dauerhaft `/display2`.
- **`?mirror=true` heißt: reine Anzeige.** Jede eingebettete Seite (`Training`, `Turnier`, `BestOf`,
  `Ziel`, `Settings`) wertet den Parameter aus und deaktiviert damit Tastatureingabe, Autofokus und
  **Eigennavigation**. Letzteres ist zwingend: sonst navigiert die Seite im iframe beim Moduswechsel
  selbst, während `Display2` gleichzeitig die iframe-`src` austauscht — zwei konkurrierende
  Navigationen im selben Dokument, die als JS-Exception in `navigateTo` enden.
- Bedient wird ausschließlich über Fenster 1 bzw. `/input`. Der iframe hat zusätzlich
  `pointer-events: none`, `tabindex="-1"` und ein Blocker-Overlay.
- `Training`, `Turnier` und `BestOf` setzen bei `mirror` zusätzlich die CSS-Klasse `mirrored` auf
  die Shell. Die Spiegelung ist **reines CSS** (`direction: rtl` auf den Grids, `ltr` auf den Zellen,
  in `StockTV_Team_StyleSheet.css`). Kein `transform: scaleX(-1)` — Ziffern und Text bleiben lesbar.
  Farbe und Teamname wandern mit ihrer Mannschaft mit.
- **Ziel/Ziel2 und die Einstellungsseite werden nicht gespiegelt** (keine `mirrored`-Klasse), sind
  über `mirror` aber ebenfalls reine Anzeige.

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
| 8098 | HTTP          | REST-Schnittstelle (eigener Web-Host, siehe `Api/StockTvApiHost.cs`) |
| 4747 | NetMQ REP/REQ | Kommandos vom zentralen System |
| 4748 | NetMQ PUB/SUB | Ergebnis-Broadcasts (bei jeder Eingabe + alle 5 Sek. Alive) |

### REST-Schnittstelle (Port 8098)

Läuft in einem **eigenen** `WebApplication`-Host neben der Blazor-Anzeige, nicht als zweiter
Listener im selben Host. Grund: in einer `WebApplication` antworten alle Endpunkte auf allen
Listenern — die API wäre sonst auch über 8080 erreichbar und teilte sich die Middleware-Kette
der Anzeige (`UseHttpsRedirection`, `UseAntiforgery`, `UseStatusCodePagesWithReExecute`).
Zusätzlich reißt ein belegter Port 8098 so nicht die Anzeige mit herunter — `StartIfEnabledAsync`
fängt das ab und gibt `null` zurück, die Anzeige läuft ohne Fernsteuerung weiter.

Muster und Code sind aus **StockTvKiosk** übernommen (`KioskApiHost`, `ApiKeyMiddleware`,
`ConfigController`, `AppConfig.Secrets`).

**Absicherung:** Jede Anfrage braucht den Header aus `RestApi.ApiKeyHeader` (Standard
`X-Api-Key`); `/swagger` ist ausgenommen, damit sich die UI überhaupt öffnen lässt. Der Vergleich
läuft zeitkonstant (`CryptographicOperations.FixedTimeEquals`). Der Schlüssel wird bei **jeder**
Anfrage neu aus dem Settings-Objekt gelesen — nur dadurch gilt ein gewechselter Schlüssel sofort.

Ist `BindAddress` nicht Loopback und `ApiKey` leer, **startet die Schnittstelle nicht** und
schreibt den Grund ins Log.

| Endpunkt | Ersetzt NetMQ-Topic |
|---|---|
| `GET /api/v1/hello` · `GET /api/v1/info` | `Hello` · `Alive` |
| `GET /api/v1/result` · `POST /api/v1/result/reset` | `GetResult` · `ResetResult` |
| `GET` / `PUT /api/v1/settings` | `GetSettings` / `SetSettings` |
| `PUT /api/v1/match/teamnames` | `SetTeamNames` |
| `PUT /api/v1/ziel/teilnehmer` | `SetTeilnehmer` |
| `GET` / `PUT` / `DELETE /api/v1/image` · `POST /api/v1/image/show` | `SetImage` · `ClearImage` · `GoToImage` |
| `GET`/`PUT /api/v1/system/hostname` · `/network` · `POST /system/reboot` | die Pi-Kommandos |
| `GET /api/v1/config` · `POST /api/v1/config/api-key` | *(neu, kein NetMQ-Gegenstück)* |

**SignalR-Hub `/hubs/stocktv`** ersetzt den PUB-Socket 4748: `ResultChanged` (bei jeder Eingabe),
`Alive` (alle 5 Sek.) und neu `SettingsChanged`. Der .NET-Client übergibt den Schlüssel als
Kopfzeile (`HttpConnectionOptions.Headers`), die auch beim Aushandeln mitgeht — ein Browser-Client
bräuchte stattdessen `access_token` als Query-Parameter.

**DTOs** (`Api/GameDtos.cs`): `ResultDto` löst die alte Antwort aus 10 Byte Kopf + angehängtem
UTF-8-JSON ab. `SettingsUpdateDto.ToLegacyBytes()` übersetzt zurück ins Byte-Paket, damit REST und
NetMQ **denselben** Pfad in `SettingsService.SetSettings` nehmen — inklusive Theme-Logik und
Navigation bei Moduswechsel. Die `NACK:`-Zeichenketten werden zu Statuscodes: `not-a-pi` → **412**,
`values-present` → **409**, Validierungsfehler → **400**.

### Parallelbetrieb NetMQ ↔ REST/SignalR

Beide Protokolle laufen gleichzeitig, bis StockAppV2 umgestellt ist. Drei Bausteine tragen das:

- **`Services/GameCommandQueue.cs`** — alle zustandsändernden Kommandos beider Protokolle laufen
  serialisiert durch **eine** Warteschlange. `MatchService`/`ZielService` sind nicht thread-sicher,
  und REST-Aufrufe kommen auf Thread-Pool-Threads. Der frühere private `_actionChannel` des
  NetMQ-Dienstes ist darin aufgegangen. **Jede neue Schreiboperation gehört hier hinein.**
- **`Services/GameEventBroadcaster.cs`** — fächert Ergebnisse auf beide Wege auf. Der Hub lebt im
  DI-Container des API-Hosts und ist im Haupt-Container nicht injizierbar; `StockTvApiHost` hängt
  sich beim Start an `OnBroadcast`. Ist die Schnittstelle aus, verpufft der Aufruf folgenlos.
- **`Services/SubscriberRegistry.cs`** — zählt NetMQ- und SignalR-Zuhörer und setzt
  `BlockLocalChanges`, wenn mindestens einer da ist. Ohne das würden sich beide Quellen
  gegenseitig überschreiben. Nebenbei behoben: die NetMQ-Seite hat den Schalter bisher pro Frame
  *umgeschaltet* statt gezählt und kippte bei zwei Abonnenten oder einem Reconnect falsch.

`POST /api/v1/config/api-key` schreibt über `SettingsService.SaveSettingsNowAsync()` statt über
`RequestSaveSettings()` — nur so lässt sich ein gescheitertes Schreiben bemerken und der
Schlüssel im Arbeitsspeicher zurücknehmen. Sonst gälte im Betrieb ein Schlüssel, der nirgends
steht, und nach dem nächsten Start käme niemand mehr auf das Gerät.

**Geheimnisse in der Konfigurationsdatei:** `RestApi.ApiKey` trägt einen
`SecretStringConverter` (`Services/SecretProtector.cs`) und wird mit `enc:`-Präfix AES-256-CBC
abgelegt. Ein von Hand im Klartext eingetragener Wert wird beim nächsten Speichern verschlüsselt.
Der Schlüssel ist deterministisch aus einer Konstante abgeleitet — das ist **Verschleierung gegen
versehentliches Mitlesen, kein Schutz** vor jemandem, der Datei und Programm hat.

**NetMQ-Topics (4747):** `Hello`, `GetResult`, `ResetResult`, `GetSettings`, `SetSettings`, `SetTeamNames` (`"Spielnr:TeamA:TeamB;..."`), `SetTeilnehmer`

**Sockettyp:** StockTV bindet einen `ResponseSocket`, StockAppV2 verbindet sich mit einem
`DealerSocket` und stellt ein Leerframe voran — auf der Leitung also `[empty][topic][value][extra?]`.
Den dritten Frame nutzt als einziges Kommando `SetImage` (Dateiname).

**Werbebild (`Services/MarketingImageService.cs`):** `SetImage` (Frame 2 = Bilddaten, Frame 3 =
Dateiname), `GoToImage` und `ClearImage` — in StockAppV2 heißen die Aufrufe
`SetMarketingImage`/`ShowMarketing`/`ClearMarketingImage`. Das Bild wird an der **Signatur** geprüft
(PNG/JPEG/GIF/BMP/WebP, max. 8 MB), nicht an der Dateiendung — sonst lieferte die Anzeige beliebige
Bytes als Bild aus. Ablage in `_config/marketing/` (übersteht einen Neustart), Anzeige über die
Seite `/marketing`, ausgeliefert vom **Anzeige-Host** unter `/marketing/image` (nicht von der
REST-Schnittstelle: der Kiosk-Browser kennt den API-Schlüssel nicht). Antworten: `ACK`,
`NACK:no-image`, `NACK:empty-image`, `NACK:image-too-large`, `NACK:unsupported-format`,
`NACK:invalid-payload`.

**NetMQ-Topics für Raspberry-Pi-Verwaltung (4747, siehe `Services/NetworkConfigService.cs`/`Services/GameStateGuard.cs`):**
Nur auf echten Raspberry Pis nutzbar (`PlatformInfoService.IsRaspberryPi`) und nur solange keine
Match-/Zieldaten hinterlegt sind (`GameStateGuard.HasRecordedValues` — Kehren, Zielversuche,
Teamnamen oder Ziel-Spielername), sonst `NACK:not-a-pi` bzw. `NACK:values-present`. Ungültige
Payloads liefern `NACK:invalid-payload`/`invalid-mode`/`invalid-ip`/`invalid-gateway`/`invalid-dns`/
`invalid-hostname`, gültige Schreib-Kommandos `ACK` (Anwendung läuft asynchron im Hintergrund).
- `SetNetworkConfig` (`"<device>:<mode>:<cidr>:<gateway>:<dnsServers>"`, `mode` = `dhcp`/`static`,
  z.B. `"eth0:static:192.168.1.50/24:192.168.1.1:192.168.1.1,8.8.8.8"` oder `"eth0:dhcp:::"`)
- `GetNetworkConfig` — liest alle verbundenen Interfaces, Antwort im selben Format, `;`-getrennt
- `SetHostname` (Hostname als reiner Text-Payload)
- `GetHostname` — liest den aktuellen Hostnamen
- `RebootPi` — kein Payload

**mDNS:** Service-Typ `_stockTV._tcp.`, TXT-Records `pubSvc=4748`, `ctrSvc=4747`, `pkgVer=<Version>`,
`osVer=<SystemKind>: <OSDescription>` (`SystemKind` = `Windows`/`RaspberryPi`/`Docker`/`Linux`,
siehe `Services/PlatformInfoService.cs`). `PUBLIC_HOST` Env-Variable überschreibt die IP im Alive-Paket.

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

## Responsives Text-Sizing (CSS, kein JavaScript)

Die Textskalierung läuft **rein deklarativ per CSS Container Queries** — es gibt **kein eigenes JS** mehr (das frühere `autofitText.js` wurde entfernt). Kernidee: Da Blazor **serverseitig** rendert, ist die Textlänge bereits bekannt und wird als CSS-Variable `--len` übergeben, sodass langer Text schrumpft, kurzer Text die Box füllt.

**Bausteine:**
- **Komponente `Controls/AutoFitText`** — rendert `<span class="autofit" style="--len:…; --min:…px">`. Parameter: `Text`, `Min` (Mindest-px, früher `data-autofit-min`), `Vertical` (writing-mode vertical), `Class`.
- **`wwwroot/css/StockTV_AutoFit.css`** — enthält die `.autofit`-Regel:
  `font-size: max(var(--min), min(84cqh, calc(150cqw / var(--len))))`
  (vertikal: `cqh`/`cqw` vertauscht). `84` = Kanten-Deckel inkl. line-height-Metrik, `150` ≈ 100 / mittlere Zeichenbreite.
- **Container:** Die umgebende Zelle muss `container-type: size` haben. Das ist an den gemeinsamen Klassen gesetzt: `.score-cell`, `.score-top-row`, `.teamname` (in `StockTV_Team_StyleSheet.css`) sowie `.ziel-cell`, `.ziel-spielername` (in `Ziel.razor.css`).

**Verwendung im Markup:** `<div class="score-cell left-points"><AutoFitText Text="@ViewModel.LeftPoints" Min="10" /></div>`. Für `int`-Werte am Aufruf `.ToString()` anhängen (der `Text`-Parameter ist `string?`).

**Ausnahmen (bewusst viewport-basiert, kein AutoFitText):** BestOf-`match-points` (`font-size: min(20vh,18vw)`) und das Ziel-`ungültig`-Overlay (`font-size: 4vw`).

---

## Konfiguration & Umgebung

### Aufteilung auf drei Dateien in `_config/`

Sortiert **nicht** nach Inhalt, sondern danach, wer schreibt und wie oft — die Datei, deren
Verlust am teuersten ist, wird am seltensten geschrieben:

| Datei | Wer schreibt | Wie oft | Verlust bedeutet |
|---|---|---|---|
| `stocktv.device.json` | Installateur, REST-API | fast nie | **Gerät aus der Ferne unerreichbar** |
| `stocktv.config.json` | Bedienung, zentrale Verwaltung | pro Einstellungsänderung | Bahn neu einstellen |
| `stocktv.state.json` | die App selbst | pro Kehre | laufendes Spiel weg |

**Zuordnungsregel:** Alles, was `SetSettings` aus dem Netz oder die Einstellungsseite ändern
kann, gehört in `stocktv.config.json` — deshalb steht `BahnNummer` dort, obwohl es nach
Geräteidentität klingt. Läge sie in der Gerätedatei, würde ein Netzwerkbefehl diese schreiben und
die Zusage „im Spielbetrieb nie angefasst" wäre hinfällig. Genau diese Zusage macht
`PUT /api/v1/config` erst möglich.

Speichern läuft über `RequestSaveSettings(SettingsScope)` bzw. `SaveSettingsNowAsync(SettingsScope)`.
**Ohne den Scope würde jede Kehre auch die Datei mit dem API-Schlüssel neu schreiben.** Die
Geräte-Umschalter (`ToggleFileLogging`, `ChangeNetworking`) rufen `SettingsScope.Device`
selbst auf — `ExitSettingsPage` schreibt nur noch die Betriebs-Konfiguration.

**Migration:** Fehlt `stocktv.device.json`, wird die Altdatei einmalig aufgeteilt und als
`stocktv.config.json.migrated` gesichert (`SettingsService.MigrateLegacyFileIfNeededAsync`).

**Schreiben** ist atomar (Temp-Datei mit Write-Through + `File.Move`). Eine unlesbare
Konfigurationsdatei wird als `.corrupt` beiseitegelegt; ein unlesbarer **Spielstand** dagegen
kommentarlos verworfen — eine Anzeige, die wegen einer defekten Punktedatei nicht hochkommt,
wäre der schlechtere Tausch.

**Spielstand (`Services/GameStateStore.cs` + `GameStateFile.cs`)** enthält Kehren, Begegnungen
und den kompletten Zielbewerb (inkl. `Runde1Summe` und `Durchgang` — ohne die fängt ein
wiederhergestellter Ziel2-Bewerb die zweite Runde von vorn an). Geladen wird nur, wenn
`BahnNummer` und `Modus` zur aktuellen Konfiguration passen und `SavedAtUtc` jünger als 12 h ist
(deckt einen Turniertag ab). Die Bahn-Prüfung fängt nebenbei ab, dass jemand `_config/` von einer
anderen Bahn kopiert hat.

Gespeichert wird über die Ereignisse `OnMatchChanged`/`OnZielBewerbChanged` (verdrahtet in
`Program.cs`), nicht an den einzelnen Eingabestellen — so ist auch erfasst, was von außen kommt
(`SetTeamNames`, `SetTeilnehmer`, `ResetResult`). Das Schreiben läuft über eine `Channel`-Queue,
damit es nicht an jeder Punkteingabe hängt. Ist nichts eingegeben, wird die Datei gelöscht statt
eine leere zu hinterlassen. Beim Wiederherstellen kapselt `RestoreWithoutSaving()` die Ereignisse
weg, sonst schriebe das Laden sofort wieder.

### Betriebs-Konfiguration (`_config/stocktv.config.json`)

| Sektion | Einstellung | Bedeutung |
|---------|-------------|-----------|
| `General.BahnNummer` | `1–4` | Bahnnummer für mDNS und externe Verwaltung |
| `General.Spielgruppe` | Zahl | Spielgruppen-ID (extern gesetzt) |
| `Game.CurrentModus` | `0/1/2/100/101` | Training / BestOf / Turnier / Ziel / Ziel2 |
| `Game.MaxPunkteProKehre` | Zahl | Max. Punkte je Kehre (Standard: 15) |
| `Game.MaxKehrenProSpiel` | Zahl | Max. Kehren je Spiel (Standard: 30) |
| `UI.CurrentRichtung` | `0/1` | Spielrichtung: 0=Links, 1=Rechts |
| `UI.MidColumnWidth` | Zahl | Breite der Mittelspalte (% oder px) |
| `UI.ActiveThemeId` | GUID | Aktives Theme (per UUID verlinkt) |
| `UI.CustomThemes` | Array | Benutzerdefinierte Themes mit Farben |

`MessageVersion` und die abgeleiteten Felder (`UI.AllThemes`, `UI.ActiveTheme`, `UI.Colors`) sind
`[JsonIgnore]` — reine Getter, die beim Laden ohnehin verworfen wurden und die Datei nur aufgebläht
haben.

### Geräte-Konfiguration (`_config/stocktv.device.json`)

| Einstellung | Werte | Bedeutung |
|-------------|-------|-----------|
| `FileLoggingEnabled` | `true/false` | Protokollierung in `_logs/` aktivieren |
| `Network.Enabled` | `true/false` | NetMQ-Netzwerk aktivieren |
| `RestApi.Enabled` | `true/false` | REST-Schnittstelle starten |
| `RestApi.BindAddress` | IP | `0.0.0.0` = alle Schnittstellen, `127.0.0.1` = nur lokal |
| `RestApi.Port` | Zahl | TCP-Port (Standard: 8098) |
| `RestApi.ApiKey` | Text | Pflicht, sobald nicht nur Loopback; verschlüsselt (`enc:`) abgelegt |
| `RestApi.ApiKeyHeader` | Text | Header mit dem Schlüssel (Standard: `X-Api-Key`) |
| `RestApi.SwaggerEnabled` | `true/false` | Swagger-UI ausliefern |
| `RestApi.SwaggerRoute` | Text | Pfad der Swagger-UI (Standard: `swagger`) |

### Umgebungsvariablen

| Variable | Zweck | Beispiel |
|----------|-------|---------|
| `PUBLIC_HOST` | IP-Adresse im mDNS Alive-Paket (überschreibt die automatisch erkannte IP) | `192.168.1.10` |
| `ASPNETCORE_URLS` | HTTP-Bindungsadresse | `http://0.0.0.0:8080` |
| `ASPNETCORE_ENVIRONMENT` | Umgebung für `appsettings.{Environment}.json` | `Development` oder `Production` |

---

## Datenspeicherung & Logging

- **Config**: `_config/stocktv.device.json`, `stocktv.config.json`, `stocktv.state.json` (relativ zum App-Verzeichnis, werden beim Start geladen)
- **Logs**: `_logs/` (JSON-Dateien mit Timestamps, nur wenn `FileLoggingEnabled=true`)
- **Speicherung**: Via `Channel`-Queue im `SettingsService`, immer mit passendem `SettingsScope` — **nie direkt** auf `CurrentSettings` schreiben
- Im Training-Modus werden Kehren **nicht** persistiert
- `RequestSaveSettings(scope)` nach State-Änderungen aufrufen
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

**Ausnahmen:**
- `Home.razor.cs` verwendet `HomeBase : ComponentBase` (Vererbung statt partial), weil die Home-Seite keinen `@rendermode` hat und eine eigene Basisklasse nutzt.
- `BestOf`, `Training`, `Turnier` und `Ziel` erben zusätzlich als `partial class` von der gemeinsamen `MirrorableGamePageBase<TViewModel>` (`Components/Pages/MirrorableGamePageBase.cs`). Diese kapselt das für alle vier Seiten identische Demo-/Mirror-Query-Parameter-, Fokus-, Navigations- und Tasten-Handling (Spiegel-Fenster `/display2`, siehe dort); die `.razor`-Datei braucht dafür `@inherits MirrorableGamePageBase<KonkretesViewModel>`. Voraussetzung: das jeweilige ViewModel implementiert `IPageViewModel`, der jeweilige Service (`MatchService`/`ZielService`) implementiert `IGameInputService`.

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

**Wichtig:** NetMQ bindet die Ports 4747/4748 im Konstruktor — es kann immer nur **eine** Instanz
laufen. Eine parallel laufende `StockTvBlazor.exe` (z.B. eine installierte) muss vorher beendet werden,
sonst bricht der Start mit `AddressAlreadyInUseException` ab.

### Zwei Anzeigen lokal testen

```powershell
build\dev\test-dualdisplay.ps1            # Haupt- und Spiegelfenster nebeneinander
build\dev\test-dualdisplay.ps1 -Demo      # mit Demo-Daten (Teamnamen, Punkte)
build\dev\test-dualdisplay.ps1 -SecondScreen
```

Das Skript beendet auf Rückfrage eine laufende Instanz, baut, startet die App und öffnet zwei
Browserfenster (Haupt- und Spiegelfenster mit getrennten Profilen). Das Hauptfenster wird zuletzt
geöffnet und behält damit den Tastatur-Fokus. ENTER im Konsolenfenster beendet alles wieder.

Manuell/in Visual Studio genügen zwei Browserfenster auf `…/turnier` und `…/display2`.

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
| Seite nach Docker-Deploy komplett unstyled ("An unhandled error has occurred") | Scoped-CSS-Hash-Mismatch: lokale `bin`/`obj` waren im Docker-Build-Context und `dotnet publish` im Container hat das CSS-Bundle (`StockTvBlazor.styles.css`) fälschlich als aktuell angesehen | `.dockerignore` im Repo-Root muss `**/bin/` und `**/obj/` ausschließen (siehe `.dockerignore`); vor `remotebuild_std.ps1` sicherstellen, dass sie greift |

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
