<#
=============================================================
 StockTV - Dual-Display-Test (nur Entwicklung)

 Startet die App lokal und oeffnet zwei Browserfenster:
   Fenster 1 : normale Anzeige  -> hier wird getippt (Numpad)
   Fenster 2 : /display2        -> gespiegelte Anzeige

 Bildschirmauswahl:
   Standardmaessig wird Fenster 1 auf dem Windows-"Hauptbildschirm"
   (Primary) platziert und Fenster 2 entweder daneben (gleicher
   Bildschirm) oder mit -SecondScreen auf dem ersten NICHT-primaeren
   Bildschirm. Das haengt davon ab, wie Windows aktuell "Primary"
   zugeordnet hat - bei vertauschten/extern angeschlossenen Monitoren
   kann das die falsche Seite treffen.

   Mit -MainDisplayIndex / -MirrorDisplayIndex kannst du die
   Bildschirme stattdessen fest ueber ihren Index ansprechen
   (0-basiert), unabhaengig von der Windows-Primary-Einstellung.
   Das Skript listet beim Start alle erkannten Bildschirme mit
   Index, Aufloesung und Position auf.

 Beispiele:
   .\test-dualdisplay.ps1                     # nebeneinander auf einem Bildschirm
   .\test-dualdisplay.ps1 -Demo               # mit Demo-Daten (Teamnamen, Punkte)
   .\test-dualdisplay.ps1 -SecondScreen       # Fenster 2 auf den 2. Bildschirm (nach Windows-Primary)
   .\test-dualdisplay.ps1 -MainDisplayIndex 0 -MirrorDisplayIndex 1   # fest per Index, unabhaengig von Primary
   .\test-dualdisplay.ps1 -StopRunning        # laufende StockTvBlazor.exe vorher beenden
=============================================================
#>
[CmdletBinding()]
param(
    [int]$Port             = 5129,
    [string]$StartPage     = "turnier",   # Startseite fuer Fenster 1: turnier, bestof, training, input, ...
    [switch]$Demo,                        # haengt ?demo=true an Fenster 1 und 2
    [switch]$SecondScreen,                # Fenster 2 auf den zweiten Bildschirm (statt nebeneinander); ignoriert falls Indizes angegeben
    [int]$MainDisplayIndex   = -1,        # 0-basierter Bildschirm-Index fuers Hauptfenster; -1 = automatisch (Windows-Primary)
    [int]$MirrorDisplayIndex = -1,        # 0-basierter Bildschirm-Index fuers Spiegel-Fenster; -1 = automatisch
    [switch]$StopRunning,                 # bereits laufende StockTvBlazor.exe ohne Rueckfrage beenden
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$project  = Join-Path $repoRoot "StockTvBlazor\StockTvBlazor.csproj"

if (-not (Test-Path $project)) {
    Write-Error "Projekt nicht gefunden: $project"
    exit 1
}

# --- 1. Konflikt pruefen: NetMQ 4747/4748 duerfen nur einmal gebunden sein ----
$running = Get-Process -Name "StockTvBlazor" -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Es laeuft bereits eine StockTV-Instanz (PID: $($running.Id -join ', '))." -ForegroundColor Yellow
    Write-Host "Sie belegt die NetMQ-Ports 4747/4748 - die Testinstanz kann so nicht starten."

    $stop = $StopRunning
    if (-not $stop) {
        $answer = Read-Host "Jetzt beenden? [J/n]"
        if ($answer -eq "" -or $answer -match "^[JjYy]") { $stop = $true }
    }

    if (-not $stop) {
        Write-Host "Abgebrochen." -ForegroundColor Red
        exit 1
    }

    $running | Stop-Process -Force
    Start-Sleep -Seconds 2
    Write-Host "Beendet. (Nach dem Test ggf. wieder manuell starten.)" -ForegroundColor Yellow
}

# --- 2. Build ---------------------------------------------------------------
if (-not $NoBuild) {
    Write-Host "`nBuild..." -ForegroundColor Cyan
    dotnet build $project -v quiet --nologo
    if ($LASTEXITCODE -ne 0) { Write-Error "Build fehlgeschlagen."; exit 1 }
}

# --- 3. App starten ---------------------------------------------------------
# Ueber Umgebungsvariablen statt launchSettings, damit kein Browser doppelt aufgeht.
$env:ASPNETCORE_URLS        = "http://localhost:$Port"
$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host "Starte StockTV auf http://localhost:$Port ..." -ForegroundColor Cyan
$app = Start-Process -FilePath "dotnet" `
                     -ArgumentList @("run", "--project", $project, "--no-launch-profile", "--no-build") `
                     -WorkingDirectory $repoRoot -PassThru

$ready = $false
for ($i = 0; $i -lt 40; $i++) {
    try {
        $null = Invoke-WebRequest -Uri "http://localhost:$Port" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        $ready = $true
        break
    } catch {
        if ($app.HasExited) { break }
        Start-Sleep -Milliseconds 1500
    }
}

if (-not $ready) {
    Write-Host "App ist nicht hochgekommen - laeuft evtl. doch noch eine Instanz?" -ForegroundColor Red
    if (-not $app.HasExited) { Stop-Process -Id $app.Id -Force }
    exit 1
}
Write-Host "App laeuft." -ForegroundColor Green

# --- 4. Browser suchen ------------------------------------------------------
$browser = $null
foreach ($c in @(
    "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    "C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    "C:\Program Files\Google\Chrome\Application\chrome.exe",
    "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe")) {
    if (Test-Path $c) { $browser = $c; break }
}
if (-not $browser) { Write-Error "Kein Edge/Chrome gefunden."; exit 1 }

# --- 5. Fenstergeometrie -----------------------------------------------------
Add-Type -AssemblyName System.Windows.Forms
$allScreens = [System.Windows.Forms.Screen]::AllScreens
$primary    = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$secondary  = $allScreens | Where-Object { -not $_.Primary } | Select-Object -First 1

Write-Host "Erkannte Bildschirme:" -ForegroundColor DarkGray
for ($i = 0; $i -lt $allScreens.Count; $i++) {
    $s   = $allScreens[$i]
    $tag = if ($s.Primary) { " <- Windows-Primary" } else { "" }
    Write-Host ("  [{0}] {1}x{2} bei ({3},{4}){5}" -f $i, $s.Bounds.Width, $s.Bounds.Height, $s.Bounds.X, $s.Bounds.Y, $tag) -ForegroundColor DarkGray
}

if ($MainDisplayIndex -ge 0 -or $MirrorDisplayIndex -ge 0) {
    # --- Explizite Zuordnung ueber Index, unabhaengig von Windows-Primary ---
    if ($MainDisplayIndex -lt 0) { $MainDisplayIndex = 0 }
    if ($MainDisplayIndex -ge $allScreens.Count) {
        Write-Error "MainDisplayIndex $MainDisplayIndex ungueltig (verfuegbar: 0..$($allScreens.Count - 1))."
        exit 1
    }
    $mainBounds = $allScreens[$MainDisplayIndex].Bounds
    $mainPos    = "$($mainBounds.X),$($mainBounds.Y)"
    $mainSize   = "$($mainBounds.Width),$($mainBounds.Height)"

    if ($MirrorDisplayIndex -ge 0) {
        if ($MirrorDisplayIndex -ge $allScreens.Count) {
            Write-Error "MirrorDisplayIndex $MirrorDisplayIndex ungueltig (verfuegbar: 0..$($allScreens.Count - 1))."
            exit 1
        }
        $mirrBounds = $allScreens[$MirrorDisplayIndex].Bounds
        $mirrPos    = "$($mirrBounds.X),$($mirrBounds.Y)"
        $mirrSize   = "$($mirrBounds.Width),$($mirrBounds.Height)"
    } else {
        # Kein Mirror-Index angegeben -> rechte Haelfte des Hauptbildschirms
        $half     = [int]($mainBounds.Width / 2)
        $mainSize = "$half,$($mainBounds.Height)"
        $mirrPos  = "$($mainBounds.X + $half),$($mainBounds.Y)"
        $mirrSize = "$half,$($mainBounds.Height)"
    }
    Write-Host "Bildschirmauswahl: Haupt=[$MainDisplayIndex] Spiegel=$(if ($MirrorDisplayIndex -ge 0) { "[$MirrorDisplayIndex]" } else { "rechte Haelfte von [$MainDisplayIndex]" })" -ForegroundColor DarkGray
}
elseif ($SecondScreen -and $secondary) {
    $b = $secondary.Bounds
    $mainPos  = "$($primary.X),$($primary.Y)"
    $mainSize = "$($primary.Width),$($primary.Height)"
    $mirrPos  = "$($b.X),$($b.Y)"
    $mirrSize = "$($b.Width),$($b.Height)"
} else {
    if ($SecondScreen) { Write-Host "Kein zweiter Bildschirm erkannt - Fenster werden nebeneinander gelegt." -ForegroundColor Yellow }
    $half     = [int]($primary.Width / 2)
    $mainPos  = "$($primary.X),$($primary.Y)"
    $mainSize = "$half,$($primary.Height)"
    $mirrPos  = "$($primary.X + $half),$($primary.Y)"
    $mirrSize = "$half,$($primary.Height)"
}

$suffix = ""
if ($Demo) { $suffix = "?demo=true" }
$mainUrl = "http://localhost:$Port/$StartPage$suffix"

# Fenster 2 zeigt den Wrapper /display2 - der folgt automatisch dem aktiven Modus
# und reicht demo=true an die gespiegelte Seite durch.
$mirrorUrl = "http://localhost:$Port/display2$suffix"

$profileRoot   = Join-Path $env:TEMP "stocktv-dualtest"
$mainProfile   = Join-Path $profileRoot "main"
$mirrorProfile = Join-Path $profileRoot "mirror"

$commonArgs = @(
    "--no-first-run"
    "--no-default-browser-check"
    "--disable-translate"
    "--disable-infobars"
    "--disable-session-crashed-bubble"
    "--noerrdialogs"
)

# --- 6. Fenster oeffnen -----------------------------------------------------
# Spiegel zuerst, Hauptfenster danach -> das Hauptfenster hat am Ende den
# Tastatur-Fokus, dort funktioniert die Numpad-Eingabe.
Write-Host "Oeffne Spiegel-Fenster:  $mirrorUrl"
$mirrorProc = Start-Process -FilePath $browser -PassThru -ArgumentList (@(
    "--user-data-dir=$mirrorProfile"
    "--window-position=$mirrPos"
    "--window-size=$mirrSize"
    "--app=$mirrorUrl"
) + $commonArgs)

Start-Sleep -Seconds 2

Write-Host "Oeffne Haupt-Fenster:    $mainUrl"
$mainProc = Start-Process -FilePath $browser -PassThru -ArgumentList (@(
    "--user-data-dir=$mainProfile"
    "--window-position=$mainPos"
    "--window-size=$mainSize"
    "--new-window", $mainUrl
) + $commonArgs)

Write-Host ""
Write-Host "=============================================================" -ForegroundColor Green
Write-Host " Eingabe im HAUPTFENSTER (linkes bzw. primaeres Fenster):"
Write-Host "   Ziffern = Punkte,  * = Gruen,  / = Rot,  + = Kehre,  - = loeschen"
Write-Host "   5x Enter bei Eingabe 0 = Einstellungsseite"
Write-Host " Das Spiegel-Fenster nimmt bewusst keine Eingaben entgegen."
Write-Host "=============================================================" -ForegroundColor Green
Write-Host ""
Read-Host "ENTER druecken zum Beenden (schliesst Browser und App)"

# --- 7. Aufraeumen ----------------------------------------------------------
foreach ($p in @($mirrorProc, $mainProc)) {
    if ($p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
}
if ($app -and -not $app.HasExited) { Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue }
Get-Process -Name "StockTvBlazor" -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -like "$repoRoot*" } |
    Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "Beendet." -ForegroundColor Cyan