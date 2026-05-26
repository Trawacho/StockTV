# ============================================
#  StockTV - Windows Service installieren
#
#  Muss als Administrator auf dem Zielrechner
#  ausgefuehrt werden.
#
#  Erstinstallation (lokales ZIP oder Skript-Verzeichnis):
#    .\install-service.ps1
#
#  Automatisch neueste Version von GitHub laden:
#    .\install-service.ps1 -Download
#
#  Mit Kiosk-Modus (Browser startet bei Anmeldung automatisch):
#    .\install-service.ps1 -Download -Kiosk
#
#  Deinstallieren:
#    .\install-service.ps1 -Uninstall
# ============================================

param(
    [string]$SourceDir   = "",          # Quellverzeichnis (Standard: Skript-Verzeichnis)
    [string]$InstallDir  = "C:\StockTV",
    [string]$ServiceName = "StockTV",
    [int]$Port           = 8080,
    [string]$KioskUser     = "stocktv-kiosk",
    [string]$KioskPassword = "stocktv",
    [switch]$Download,                  # Neueste Version von GitHub Releases laden
    [switch]$Kiosk,                     # Kiosk-Modus: Browser bei Anmeldung automatisch starten
    [switch]$Uninstall
)

$REPO            = "Trawacho/StockTV2"
$ASSET_NAME      = "stocktv-windows-x64.zip"
$KIOSK_TASK      = "StockTV Kiosk"
$KIOSK_SENTINEL  = Join-Path $InstallDir ".kiosk"
$KIOSK_SCRIPT    = Join-Path $InstallDir "start-kiosk.ps1"

$ErrorActionPreference = "Stop"

# ============================================
#  GitHub-Download (optional)
# ============================================

if ($Download) {
    Write-Host "Suche neueste Version auf GitHub..."

    $headers  = @{ "User-Agent" = "install-service.ps1" }
    $release  = Invoke-RestMethod -Uri "https://api.github.com/repos/$REPO/releases/latest" -Headers $headers
    $asset    = $release.assets | Where-Object { $_.name -eq $ASSET_NAME }

    if (-not $asset) {
        Write-Error "Kein Asset '$ASSET_NAME' im Release $($release.tag_name) gefunden."
        exit 1
    }

    Write-Host "Gefunden: $($release.tag_name)"

    $TempRoot = Join-Path $env:TEMP ("stocktv_install_" + [System.Guid]::NewGuid().ToString("N"))
    New-Item $TempRoot -ItemType Directory -Force | Out-Null

    $ZipPath  = Join-Path $TempRoot $ASSET_NAME
    $AppPath  = Join-Path $TempRoot "app"

    Write-Host "Lade $ASSET_NAME herunter..."
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $ZipPath -UseBasicParsing

    Write-Host "Entpacke..."
    Expand-Archive -Path $ZipPath -DestinationPath $AppPath -Force

    $SourceDir = $AppPath
    Write-Host ""
}

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
        Write-Host "Dienst '$ServiceName' nicht gefunden - nichts zu tun."
    }

    $task = Get-ScheduledTask -TaskName $KIOSK_TASK -ErrorAction SilentlyContinue
    if ($task) {
        Unregister-ScheduledTask -TaskName $KIOSK_TASK -Confirm:$false
        Write-Host "Kiosk-Task geloescht."
    }

    if (Test-Path $KIOSK_SENTINEL) { Remove-Item $KIOSK_SENTINEL -Force }

    $winlogon = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
    Set-ItemProperty -Path $winlogon -Name "AutoAdminLogon" -Value "0" -Type String -ErrorAction SilentlyContinue
    Remove-ItemProperty -Path $winlogon -Name "DefaultPassword" -ErrorAction SilentlyContinue
    Write-Host "Autologin deaktiviert."

    Write-Host "Hinweis: Kiosk-Benutzer '$KioskUser' wurde nicht geloescht. Manuell entfernen: Remove-LocalUser -Name '$KioskUser'"

    exit 0
}

# ============================================
#  Installieren / Aktualisieren
# ============================================

if (-not $SourceDir) { $SourceDir = $PSScriptRoot }

$exe = Join-Path $InstallDir "StockTvBlazor.exe"

# Sentinel pruefen: Kiosk war beim Erstinstall aktiviert -> immer neu einrichten
if (Test-Path $KIOSK_SENTINEL) { $Kiosk = $true }

Write-Host "=========================================="
Write-Host "   StockTV Windows Service Setup"
Write-Host "   Quelle:  $SourceDir"
Write-Host "   Ziel:    $InstallDir"
Write-Host "   Port:    $Port"
if ($Kiosk) {
    Write-Host "   Kiosk:   ja"
}
Write-Host "==========================================`n"

# Dienst stoppen und auf vollstaendiges Beenden warten
$existingSvc = Get-Service $ServiceName -ErrorAction SilentlyContinue
if ($existingSvc -and $existingSvc.Status -eq "Running") {
    Write-Host "Stoppe laufenden Dienst..."
    Stop-Service $ServiceName -Force
    $timeout = 30
    $elapsed = 0
    while ((Get-Service $ServiceName).Status -ne "Stopped" -and $elapsed -lt $timeout) {
        Start-Sleep -Seconds 1
        $elapsed++
    }
    if ((Get-Service $ServiceName).Status -ne "Stopped") {
        Write-Warning "Dienst nach $timeout Sekunden noch nicht gestoppt - Dateien koennen gesperrt sein."
    } else {
        Write-Host "Dienst gestoppt (nach $elapsed Sek.)."
    }
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
        -Description "StockTV Blazor Server - Punkteanzeigesystem fuer den Stocksport" `
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
    Write-Host "Dienst '$ServiceName' bereits vorhanden - wird nur aktualisiert."
}

# Dienst starten
Write-Host "Starte Dienst..."
Start-Service $ServiceName
Start-Sleep -Seconds 2
$status = (Get-Service $ServiceName).Status

# ============================================
#  Kiosk-Modus einrichten / pruefen
# ============================================

if ($Kiosk) {
    Write-Host "`nKiosk-Modus wird eingerichtet..."

    # Port-Platzhalter in start-kiosk.ps1 ersetzen
    if (-not (Test-Path $KIOSK_SCRIPT)) {
        Write-Error "start-kiosk.ps1 nicht gefunden in $InstallDir"
        exit 1
    }
    $content = Get-Content -Path $KIOSK_SCRIPT -Raw -Encoding UTF8
    $content = $content -replace '__PORT__', $Port
    Set-Content -Path $KIOSK_SCRIPT -Value $content -Encoding UTF8
    Write-Host "  start-kiosk.ps1 konfiguriert (Port: $Port)."

    # Lokalen Kiosk-Benutzer anlegen
    $secPass = ConvertTo-SecureString $KioskPassword -AsPlainText -Force
    $existingUser = Get-LocalUser -Name $KioskUser -ErrorAction SilentlyContinue
    if (-not $existingUser) {
        New-LocalUser -Name $KioskUser `
            -Password $secPass `
            -FullName "StockTV Kiosk" `
            -Description "StockTV Kiosk Benutzer" `
            -PasswordNeverExpires `
            -UserMayNotChangePassword | Out-Null
        Write-Host "  Benutzer '$KioskUser' angelegt."
    } else {
        Set-LocalUser -Name $KioskUser -Password $secPass
        Write-Host "  Benutzer '$KioskUser' vorhanden - Passwort aktualisiert."
    }

    # Autologin in Registry einrichten
    $plainPass = $KioskPassword

    $winlogon = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
    Set-ItemProperty -Path $winlogon -Name "AutoAdminLogon"    -Value "1"               -Type String
    Set-ItemProperty -Path $winlogon -Name "DefaultUserName"   -Value $KioskUser        -Type String
    Set-ItemProperty -Path $winlogon -Name "DefaultPassword"   -Value $plainPass        -Type String
    Set-ItemProperty -Path $winlogon -Name "DefaultDomainName" -Value $env:COMPUTERNAME -Type String
    Write-Host "  Autologin fuer '$KioskUser' konfiguriert."

    # Energieoptionen: Display und Standby dauerhaft deaktivieren
    powercfg /change monitor-timeout-ac 0
    powercfg /change monitor-timeout-dc 0
    powercfg /change standby-timeout-ac 0
    powercfg /change standby-timeout-dc 0
    powercfg /change hibernate-timeout-ac 0
    powercfg /change hibernate-timeout-dc 0
    Write-Host "  Energieoptionen konfiguriert (kein Standby, kein Display-Timeout)."

    # Windows-Meldungen systemweit unterdrucken
    # Sperrbildschirm deaktivieren
    $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "NoLockScreen" -Value 1 -Type DWord

    # Erste-Anmeldung-Animation deaktivieren
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" `
        -Name "EnableFirstLogonAnimation" -Value 0 -Type DWord

    # OOBE-Datenschutz-Assistent deaktivieren (erscheint sonst beim ersten Login eines neuen Benutzers)
    $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\OOBE"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "DisablePrivacyExperience" -Value 1 -Type DWord

    # OneDrive-Einrichtungsassistent deaktivieren
    $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\OneDrive"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "DisableFileSyncNGSC" -Value 1 -Type DWord

    # Windows Update: kein automatischer Neustart bei angemeldeten Benutzern
    $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "NoAutoRebootWithLoggedOnUsers" -Value 1 -Type DWord

    # Windows Error Reporting deaktivieren
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting" `
        -Name "Disabled" -Value 1 -Type DWord

    # Defender Security Center Benachrichtigungen deaktivieren
    $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "DisableNotifications" -Value 1 -Type DWord

    Write-Host "  Windows-Systemmeldungen deaktiviert."

    # Scheduled Task anlegen / aktualisieren (laeuft fuer alle Benutzer der Gruppe Users)
    $action = New-ScheduledTaskAction `
        -Execute "powershell.exe" `
        -Argument "-NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$KIOSK_SCRIPT`""

    $trigger = New-ScheduledTaskTrigger -AtLogOn

    $settings = New-ScheduledTaskSettingsSet `
        -ExecutionTimeLimit ([TimeSpan]::Zero) `
        -MultipleInstances IgnoreNew

    $principal = New-ScheduledTaskPrincipal `
        -GroupId "BUILTIN\Users" `
        -RunLevel Limited

    Register-ScheduledTask `
        -TaskName $KIOSK_TASK `
        -Action $action `
        -Trigger $trigger `
        -Principal $principal `
        -Settings $settings `
        -Force | Out-Null

    Write-Host "  Scheduled Task '$KIOSK_TASK' registriert."

    # Sentinel anlegen
    New-Item -Path $KIOSK_SENTINEL -ItemType File -Force | Out-Null

    Write-Host "  Kiosk-Modus eingerichtet."
    Write-Host "  Neustart erforderlich, damit Autologin aktiv wird."
}

# ============================================
#  Zusammenfassung
# ============================================

Write-Host "`n=========================================="
Write-Host "  Dienst '$ServiceName': $status"
Write-Host ""
Write-Host "  Web-UI:  http://localhost:$Port"
if ($Kiosk) {
    Write-Host "  Kiosk:   Browser startet automatisch bei Anmeldung"
    Write-Host "           (Task: '$KIOSK_TASK')"
}
Write-Host ""
Write-Host "  Verwaltung:"
Write-Host "    Start:     Start-Service $ServiceName"
Write-Host "    Stop:      Stop-Service $ServiceName"
Write-Host "    Logs:      Get-EventLog -LogName Application -Source $ServiceName -Newest 20"
Write-Host "    Entfernen: .\install-service.ps1 -Uninstall"
Write-Host "=========================================="
