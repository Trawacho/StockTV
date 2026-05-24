# ============================================
#  StockTV – Publish Windows (win-x64)
# ============================================
#
#  Nur bauen + Zip fuer GitHub Release:
#    .\publish-windows.ps1
#
#  Bauen + direkt auf Zielrechner deployen (WinRM):
#    .\publish-windows.ps1 -TargetHost 192.168.1.xx
#    .\publish-windows.ps1 -TargetHost 192.168.1.xx -Install

param(
    [string]$TargetHost  = "",
    [string]$TargetUser  = "",
    [string]$InstallDir  = "C:\StockTV",
    [string]$ServiceName = "StockTV",
    [switch]$Install
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$OutputDir   = Join-Path $PSScriptRoot "publish"
$ZipFile     = Join-Path $PSScriptRoot "stocktv-windows-x64.zip"
$Project     = Join-Path $ProjectRoot "StockTvBlazor\StockTvBlazor.csproj"

# ============================================
#  1. Bauen
# ============================================

Write-Host "=========================================="
Write-Host "   PUBLISH: StockTV fuer Windows"
Write-Host "   Ziel: win-x64 (self-contained)"
Write-Host "==========================================`n"

if (Test-Path $OutputDir) {
    Write-Host "Loesche altes Publish-Verzeichnis..."
    Remove-Item $OutputDir -Recurse -Force
}

Write-Host "Baue Projekt..."
dotnet publish $Project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained `
    --output $OutputDir `
    -p:UseAppHost=true

# ============================================
#  2. install-service.ps1 ins Paket aufnehmen
# ============================================

Copy-Item "$PSScriptRoot\install-service.ps1" "$OutputDir\install-service.ps1" -Force

# ============================================
#  3. Zip fuer GitHub Release erstellen
# ============================================

Write-Host "Erstelle Release-Zip..."
if (Test-Path $ZipFile) { Remove-Item $ZipFile -Force }
Compress-Archive -Path "$OutputDir\*" -DestinationPath $ZipFile

Write-Host "`nPublish: $OutputDir"
Write-Host "Release:  $ZipFile"

# ============================================
#  3. Direkt deployen (optional, via WinRM)
# ============================================

if (-not $TargetHost) {
    Write-Host "`n=========================================="
    Write-Host "Fertig! Naechster Schritt:"
    Write-Host "  ZIP auf Zielrechner kopieren und"
    Write-Host "  install-service.ps1 als Administrator ausfuehren"
    Write-Host "=========================================="
    exit 0
}

Write-Host "`n=========================================="
Write-Host "   DEPLOY: StockTV -> $TargetHost"
Write-Host "=========================================="

$credParams = @{}
if ($TargetUser) { $credParams['Credential'] = Get-Credential -UserName $TargetUser -Message "Passwort fuer $TargetHost" }

$session = New-PSSession -ComputerName $TargetHost @credParams

# Dienst stoppen falls laufend
Invoke-Command -Session $session -ScriptBlock {
    param($svc)
    if (Get-Service $svc -ErrorAction SilentlyContinue) {
        Write-Host "Stoppe Dienst '$svc'..."
        Stop-Service $svc -Force
    }
} -ArgumentList $ServiceName

# Dateien kopieren
Write-Host "Kopiere Dateien nach $TargetHost`:$InstallDir ..."
if (-not (Test-Path $OutputDir)) {
    Write-Error "Publish-Verzeichnis nicht gefunden: $OutputDir"
}
$null = Invoke-Command -Session $session -ScriptBlock {
    param($dir)
    if (-not (Test-Path $dir)) { New-Item $dir -ItemType Directory -Force }
} -ArgumentList $InstallDir

Copy-Item -Path "$OutputDir\*" -Destination $InstallDir -ToSession $session -Recurse -Force

# Dienst installieren (Erstinstallation)
if ($Install) {
    Invoke-Command -Session $session -ScriptBlock {
        param($dir, $svc)
        $exe = Join-Path $dir "StockTvBlazor.exe"
        if (-not (Get-Service $svc -ErrorAction SilentlyContinue)) {
            Write-Host "Installiere Dienst '$svc'..."
            New-Service -Name $svc `
                -BinaryPathName $exe `
                -DisplayName "StockTV Punkteanzeige" `
                -StartupType Automatic
            # Umgebungsvariablen fuer den Dienst setzen
            $regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$svc"
            New-ItemProperty -Path $regPath -Name "Environment" `
                -PropertyType MultiString `
                -Value @("ASPNETCORE_URLS=http://+:8080", "ASPNETCORE_ENVIRONMENT=Production") `
                -Force
            Write-Host "Dienst '$svc' installiert."
        }
    } -ArgumentList $InstallDir, $ServiceName
}

# Dienst starten
Invoke-Command -Session $session -ScriptBlock {
    param($svc)
    Start-Service $svc
    $status = (Get-Service $svc).Status
    Write-Host "Dienst '$svc': $status"
} -ArgumentList $ServiceName

Remove-PSSession $session

Write-Host "`n=========================================="
Write-Host "Deployment abgeschlossen!"
Write-Host "Web-UI: http://${TargetHost}:8080"
Write-Host "=========================================="
