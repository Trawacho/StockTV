# ============================================
#  StockTV – Windows Service installieren
#
#  Muss als Administrator auf dem Zielrechner
#  ausgefuehrt werden.
#
#  Erstinstallation:
#    .\install-service.ps1
#
#  Deinstallieren:
#    .\install-service.ps1 -Uninstall
# ============================================

param(
    [string]$SourceDir   = "",          # Quellverzeichnis (Standard: Skript-Verzeichnis)
    [string]$InstallDir  = "C:\StockTV",
    [string]$ServiceName = "StockTV",
    [int]$Port           = 8080,
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

# Adminrechte pruefen
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Dieses Skript muss als Administrator ausgefuehrt werden."
    exit 1
}

# ============================================
#  Deinstallieren
# ============================================

if ($Uninstall) {
    Write-Host "Deinstalliere Dienst '$ServiceName'..."
    $svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($svc) {
        if ($svc.Status -eq "Running") { Stop-Service $ServiceName -Force }
        sc.exe delete $ServiceName
        Write-Host "Dienst geloescht."
    } else {
        Write-Host "Dienst '$ServiceName' nicht gefunden – nichts zu tun."
    }
    exit 0
}

# ============================================
#  Installieren / Aktualisieren
# ============================================

if (-not $SourceDir) { $SourceDir = $PSScriptRoot }

$exe = Join-Path $InstallDir "StockTvBlazor.exe"

Write-Host "=========================================="
Write-Host "   StockTV Windows Service Setup"
Write-Host "   Quelle:  $SourceDir"
Write-Host "   Ziel:    $InstallDir"
Write-Host "   Port:    $Port"
Write-Host "==========================================`n"

# Dienst stoppen (falls laufend)
$existingSvc = Get-Service $ServiceName -ErrorAction SilentlyContinue
if ($existingSvc -and $existingSvc.Status -eq "Running") {
    Write-Host "Stoppe laufenden Dienst..."
    Stop-Service $ServiceName -Force
    Start-Sleep -Seconds 2
}

# Zielverzeichnis anlegen und Dateien kopieren
Write-Host "Kopiere Dateien nach $InstallDir ..."
if (-not (Test-Path $InstallDir)) {
    New-Item $InstallDir -ItemType Directory -Force | Out-Null
}
Copy-Item -Path "$SourceDir\*" -Destination $InstallDir -Recurse -Force

# Unterverzeichnisse anlegen
foreach ($sub in @("_config", "_logs")) {
    $path = Join-Path $InstallDir $sub
    if (-not (Test-Path $path)) { New-Item $path -ItemType Directory -Force | Out-Null }
}

# Dienst anlegen (nur beim ersten Mal)
if (-not (Get-Service $ServiceName -ErrorAction SilentlyContinue)) {
    Write-Host "Installiere Windows-Dienst '$ServiceName'..."
    New-Service `
        -Name $ServiceName `
        -BinaryPathName $exe `
        -DisplayName "StockTV Punkteanzeige" `
        -Description "StockTV Blazor Server – Punkteanzeigesystem fuer den Stocksport" `
        -StartupType Automatic

    # Umgebungsvariablen setzen
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
    New-ItemProperty -Path $regPath -Name "Environment" `
        -PropertyType MultiString `
        -Value @(
            "ASPNETCORE_URLS=http://+:$Port",
            "ASPNETCORE_ENVIRONMENT=Production"
        ) -Force | Out-Null

    Write-Host "Dienst '$ServiceName' installiert."
} else {
    Write-Host "Dienst '$ServiceName' bereits vorhanden – wird nur aktualisiert."
}

# Dienst starten
Write-Host "Starte Dienst..."
Start-Service $ServiceName
Start-Sleep -Seconds 2
$status = (Get-Service $ServiceName).Status

Write-Host "`n=========================================="
Write-Host "  Dienst '$ServiceName': $status"
Write-Host ""
Write-Host "  Web-UI:  http://localhost:$Port"
Write-Host ""
Write-Host "  Verwaltung:"
Write-Host "    Start:   Start-Service $ServiceName"
Write-Host "    Stop:    Stop-Service $ServiceName"
Write-Host "    Logs:    Get-EventLog -LogName Application -Source $ServiceName -Newest 20"
Write-Host "    Entfernen: .\install-service.ps1 -Uninstall"
Write-Host "=========================================="
