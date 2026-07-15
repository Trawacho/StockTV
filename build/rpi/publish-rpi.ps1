# ============================================
#  StockTV – Publish + optionales Deploy
#  Raspberry Pi (linux-arm64, self-contained)
# ============================================
#
#  Nur bauen + Zip fuer GitHub Release:
#    .\publish-rpi.ps1
#
#  Bauen + direkt auf Pi deployen:
#    .\publish-rpi.ps1 -PiHost 192.168.1.xx
#    .\publish-rpi.ps1 -PiHost 192.168.1.xx -Install            (Erstinstallation)
#    .\publish-rpi.ps1 -PiHost 192.168.1.xx -SudoPass geheim    (anderes sudo-Passwort)

param(
    [string]$PiHost      = "",
    [string]$PiUser      = "pi",
    [string]$SudoPass    = "stocktv",
    [string]$RemoteDir   = "/opt/stocktv",
    [string]$ServiceName = "stocktv",
    [switch]$Install
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$OutputDir   = Join-Path $PSScriptRoot "publish"
$ZipFile     = Join-Path $PSScriptRoot "stocktv-rpi.zip"
$Project     = Join-Path $ProjectRoot "StockTvBlazor\StockTvBlazor.csproj"

# ============================================
#  1. Bauen
# ============================================

Write-Host "=========================================="
Write-Host "   PUBLISH: StockTV fuer Raspberry Pi"
Write-Host "   Ziel: linux-arm64 (self-contained)"
Write-Host "==========================================`n"

if (Test-Path $OutputDir) {
    Write-Host "Loesche altes Publish-Verzeichnis..."
    Remove-Item $OutputDir -Recurse -Force
}

Write-Host "Baue Projekt..."

dotnet publish $Project `
    --configuration Release `
    --runtime linux-arm64 `
    --self-contained `
    --output $OutputDir `
    -p:UseAppHost=true

# ============================================
#  2. Zip fuer GitHub Release erstellen
# ============================================

Write-Host "Erstelle Release-Zip..."

if (Test-Path $ZipFile) {
    Remove-Item $ZipFile -Force
}

Compress-Archive -Path "$OutputDir\*" -DestinationPath $ZipFile

Write-Host "`nPublish: $OutputDir"
Write-Host "Release:  $ZipFile"

# ============================================
#  3. Direkt auf Pi deployen (optional)
# ============================================

if (-not $PiHost) {
    Write-Host "`n=========================================="
    Write-Host "Fertig! Naechster Schritt:"
    Write-Host "  GitHub Release erstellen und stocktv-rpi.zip hochladen"
    Write-Host "  https://github.com/Trawacho/StockTV/releases/new"
    Write-Host "=========================================="
    exit 0
}

$RemoteUser = "$PiUser@$PiHost"

Write-Host "`n=========================================="
Write-Host "   DEPLOY: StockTV -> $PiHost"
Write-Host "=========================================="

# Hilfsfunktion: sudo-Befehl mit Passwort via stdin
function Invoke-Sudo($cmd) {
    ssh $RemoteUser "echo '$SudoPass' | sudo -S $cmd"
}

# --- Service stoppen (falls laufend) ---
Write-Host "`nStoppe Dienst auf Pi..."
ssh $RemoteUser "echo '$SudoPass' | sudo -S systemctl stop $ServiceName 2>/dev/null; true"

# --- Zielverzeichnis anlegen ---
Write-Host "Erstelle Verzeichnis $RemoteDir..."
Invoke-Sudo "mkdir -p $RemoteDir"
Invoke-Sudo "chown ${PiUser}:${PiUser} $RemoteDir"

# --- Dateien kopieren ---
Write-Host "Kopiere Dateien..."
scp -r "$OutputDir/*" "${RemoteUser}:${RemoteDir}/"

# --- Berechtigungen setzen ---
Write-Host "Setze Berechtigungen..."
ssh $RemoteUser "chmod +x $RemoteDir/StockTvBlazor && mkdir -p $RemoteDir/_config $RemoteDir/_logs"

# --- Service installieren (nur beim ersten Mal) ---
if ($Install) {
    Write-Host "`nInstalliere systemd-Dienst..."

    $ServiceContent = @"
[Unit]
Description=StockTV Punkteanzeige
After=network.target

[Service]
WorkingDirectory=$RemoteDir
ExecStart=$RemoteDir/StockTvBlazor
Restart=always
RestartSec=5
TimeoutStopSec=15
User=$PiUser
Environment=ASPNETCORE_URLS=http://+:8080
Environment=ASPNETCORE_ENVIRONMENT=Production
# Environment=PUBLIC_HOST=192.168.1.xx

[Install]
WantedBy=multi-user.target
"@

    $TempServiceFile = New-TemporaryFile
    [System.IO.File]::WriteAllText($TempServiceFile, $ServiceContent, [System.Text.Encoding]::UTF8)
    scp -T $TempServiceFile "${RemoteUser}:/tmp/$ServiceName.service"
    Invoke-Sudo "mv /tmp/$ServiceName.service /etc/systemd/system/$ServiceName.service"
    Remove-Item $TempServiceFile
    Invoke-Sudo "systemctl daemon-reload"
    Invoke-Sudo "systemctl enable $ServiceName"
    Write-Host "Dienst '$ServiceName' installiert und aktiviert."
}

# --- Service starten ---
Write-Host "`nStarte Dienst..."
Invoke-Sudo "systemctl start $ServiceName"

# --- Status anzeigen ---
Write-Host "`n--- Dienst-Status ---"
Invoke-Sudo "systemctl status $ServiceName --no-pager"

Write-Host "`n=========================================="
Write-Host "Deployment abgeschlossen!"
Write-Host "Web-UI: http://${PiHost}:8080"
Write-Host "=========================================="
