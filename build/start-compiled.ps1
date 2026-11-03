<#
=============================================================
 StockTV - Kompilierte Version starten (kein Build)

 Startet die bereits gebaute StockTvBlazor.exe direkt und
 oeffnet optional den Browser dazu. Anders als
 test-dualdisplay.ps1 wird hier NICHT gebaut und es wird nur
 EIN Fenster geoeffnet.

 Beispiele:
   .\start-compiled.ps1                        # Release-Build, oeffnet Browser
   .\start-compiled.ps1 -Configuration Debug    # Debug-Build starten
   .\start-compiled.ps1 -StartPage bestof       # andere Startseite
   .\start-compiled.ps1 -Demo                   # mit Demo-Daten
   .\start-compiled.ps1 -NoBrowser              # nur die App starten, kein Browserfenster
   .\start-compiled.ps1 -StopRunning            # laufende Instanz vorher ohne Rueckfrage beenden
=============================================================
#>
[CmdletBinding()]
param(
    [int]$Port                 = 5129,
    [string]$StartPage         = "turnier",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration     = "Release",
    [switch]$Demo,
    [switch]$NoBrowser,
    [switch]$StopRunning
)

$ErrorActionPreference = "Stop"

$repoRoot    = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$projectDir  = Join-Path $repoRoot "StockTvBlazor"
$binDir      = Join-Path $projectDir "bin\$Configuration"

if (-not (Test-Path $binDir)) {
    Write-Error "Kein '$Configuration'-Build gefunden unter: $binDir`nBitte zuerst bauen (z.B. dotnet build -c $Configuration)."
    exit 1
}

# --- 1. Passende .exe suchen (neueste, falls mehrere Ziel-Frameworks) -------
$exe = Get-ChildItem -Path $binDir -Filter "StockTvBlazor.exe" -Recurse |
       Sort-Object LastWriteTime -Descending |
       Select-Object -First 1

if (-not $exe) {
    Write-Error "StockTvBlazor.exe wurde unter $binDir nicht gefunden. Bitte zuerst bauen."
    exit 1
}
Write-Host "Verwende Build: $($exe.FullName)" -ForegroundColor DarkGray

# --- 2. Konflikt pruefen: NetMQ 4747/4748 duerfen nur einmal gebunden sein --
$running = Get-Process -Name "StockTvBlazor" -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Es laeuft bereits eine StockTV-Instanz (PID: $($running.Id -join ', '))." -ForegroundColor Yellow

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
}

# --- 3. App starten ----------------------------------------------------------
$env:ASPNETCORE_URLS        = "http://localhost:$Port"
$env:ASPNETCORE_ENVIRONMENT = $Configuration

Write-Host "Starte StockTV (kompiliert) auf http://localhost:$Port ..." -ForegroundColor Cyan
$app = Start-Process -FilePath $exe.FullName -WorkingDirectory $exe.DirectoryName -PassThru

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
    Write-Host "App ist nicht hochgekommen." -ForegroundColor Red
    if (-not $app.HasExited) { Stop-Process -Id $app.Id -Force }
    exit 1
}
Write-Host "App laeuft. (PID: $($app.Id))" -ForegroundColor Green

# --- 4. Browser oeffnen (optional) -------------------------------------------
$suffix = ""
if ($Demo) { $suffix = "?demo=true" }
$url = "http://localhost:$Port/$StartPage$suffix"

if (-not $NoBrowser) {
    Write-Host "Oeffne Browser: $url"
    Start-Process $url
} else {
    Write-Host "Aufrufbar unter: $url"
}

Write-Host ""
Write-Host "App laeuft im Hintergrund (PID $($app.Id))." -ForegroundColor Green
Write-Host "Zum Beenden: Stop-Process -Id $($app.Id)  (oder Task-Manager)" -ForegroundColor DarkGray
