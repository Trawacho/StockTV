# ============================================
#  StockTV – Publish Linux x64 (linux-x64)
# ============================================
#
#  Nur bauen + Zip fuer GitHub Release:
#    .\publish-linux.ps1
#
#  Bauen + direkt auf Linux-Server deployen:
#    .\publish-linux.ps1 -TargetHost 192.168.1.xx
#    .\publish-linux.ps1 -TargetHost 192.168.1.xx -Install   (Erstinstallation)

param(
    [string]$TargetHost  = "",
    [string]$TargetUser  = "stocktv",
    [string]$SudoPass    = "",
    [string]$RemoteDir   = "/opt/stocktv",
    [string]$ServiceName = "stocktv",
    [switch]$Install
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$OutputDir   = Join-Path $PSScriptRoot "publish"
$ZipFile     = Join-Path $PSScriptRoot "stocktv-linux-x64.zip"
$Project     = Join-Path $ProjectRoot "StockTvBlazor\StockTvBlazor.csproj"

# ============================================
#  1. Bauen
# ============================================

Write-Host "=========================================="
Write-Host "   PUBLISH: StockTV fuer Linux"
Write-Host "   Ziel: linux-x64 (self-contained)"
Write-Host "==========================================`n"

if (Test-Path $OutputDir) {
    Write-Host "Loesche altes Publish-Verzeichnis..."
    Remove-Item $OutputDir -Recurse -Force
}

Write-Host "Baue Projekt..."
dotnet publish $Project `
    --configuration Release `
    --runtime linux-x64 `
    --self-contained `
    --output $OutputDir `
    -p:UseAppHost=true

# ============================================
#  2. Zip fuer GitHub Release erstellen
# ============================================

Write-Host "Erstelle Release-Zip..."
if (Test-Path $ZipFile) { Remove-Item $ZipFile -Force }
Compress-Archive -Path "$OutputDir\*" -DestinationPath $ZipFile

Write-Host "`nPublish: $OutputDir"
Write-Host "Release:  $ZipFile"

# ============================================
#  3. Direkt auf Server deployen (optional)
# ============================================

if (-not $TargetHost) {
    Write-Host "`n=========================================="
    Write-Host "Fertig! Naechster Schritt:"
    Write-Host "  install.sh auf dem Ziel-Server ausfuehren"
    Write-Host "  oder: GitHub Release erstellen und hochladen"
    Write-Host "=========================================="
    exit 0
}

$RemoteUser = "$TargetUser@$TargetHost"

Write-Host "`n=========================================="
Write-Host "   DEPLOY: StockTV -> $TargetHost"
Write-Host "=========================================="

function Invoke-Sudo($cmd) {
    if ($SudoPass) {
        ssh $RemoteUser "echo '$SudoPass' | sudo -S $cmd"
    } else {
        ssh $RemoteUser "sudo $cmd"
    }
}

# Dienst stoppen
Write-Host "`nStoppe Dienst auf Server..."
ssh $RemoteUser "sudo systemctl stop $ServiceName 2>/dev/null; true"

# Zielverzeichnis anlegen
Write-Host "Erstelle Verzeichnis $RemoteDir..."
Invoke-Sudo "mkdir -p $RemoteDir"
Invoke-Sudo "chown ${TargetUser}:${TargetUser} $RemoteDir"

# Dateien kopieren
Write-Host "Kopiere Dateien..."
scp -r "$OutputDir/*" "${RemoteUser}:${RemoteDir}/"

# Berechtigungen setzen
Write-Host "Setze Berechtigungen..."
ssh $RemoteUser "chmod +x $RemoteDir/StockTvBlazor && mkdir -p $RemoteDir/_config $RemoteDir/_logs"

# Service installieren (nur beim ersten Mal)
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
User=$TargetUser
Environment=ASPNETCORE_URLS=http://+:8080
Environment=ASPNETCORE_ENVIRONMENT=Production
# Environment=PUBLIC_HOST=192.168.1.xx

[Install]
WantedBy=multi-user.target
"@

    $ServiceContent | ssh $RemoteUser "sudo tee /etc/systemd/system/$ServiceName.service > /dev/null"
    Invoke-Sudo "systemctl daemon-reload"
    Invoke-Sudo "systemctl enable $ServiceName"
    Write-Host "Dienst '$ServiceName' installiert und aktiviert."
}

# Dienst starten
Write-Host "`nStarte Dienst..."
Invoke-Sudo "systemctl start $ServiceName"

Write-Host "`n--- Dienst-Status ---"
Invoke-Sudo "systemctl status $ServiceName --no-pager"

Write-Host "`n=========================================="
Write-Host "Deployment abgeschlossen!"
Write-Host "Web-UI: http://${TargetHost}:8080"
Write-Host "=========================================="
