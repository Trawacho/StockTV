# ============================================
#  StockTV - Kiosk-Starter
#  Wird vom Scheduled Task 'StockTV Kiosk' bei
#  jeder Benutzeranmeldung ausgefuehrt.
# ============================================

$url = "http://localhost:__PORT__"

# Explorer sofort beenden - entfernt Taskbar, Startmenue und Desktop
Stop-Process -Name "explorer" -Force -ErrorAction SilentlyContinue

# Warten bis StockTV erreichbar ist (max. 60 Sekunden)
for ($i = 0; $i -lt 30; $i++) {
    try {
        $null = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        break
    } catch {
        Start-Sleep -Seconds 2
    }
}

# Edge oder Chrome suchen
$browser = $null
$candidates = @(
    "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    "C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    "C:\Program Files\Google\Chrome\Application\chrome.exe",
    "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
)
foreach ($c in $candidates) {
    if (Test-Path $c) { $browser = $c; break }
}

if (-not $browser) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        "Kein Browser gefunden (Edge oder Chrome).",
        "StockTV Kiosk"
    ) | Out-Null
    exit 1
}

# Kiosk-Profil anlegen: deaktiviert Uebersetzungs- und Benachrichtigungsdialoge
$kioskProfile = Join-Path $PSScriptRoot "kiosk-profile"
$defaultDir   = Join-Path $kioskProfile "Default"
New-Item -Path $defaultDir -ItemType Directory -Force | Out-Null

$prefs = @'
{
  "translate": { "enabled": false },
  "translate_blocked_languages": ["de", "en"],
  "browser": {
    "check_default_browser": false,
    "show_home_button": false
  },
  "profile": {
    "default_content_setting_values": {
      "notifications": 2,
      "geolocation": 2,
      "media_stream_mic": 2,
      "media_stream_camera": 2
    }
  }
}
'@
Set-Content -Path (Join-Path $defaultDir "Preferences") -Value $prefs -Encoding UTF8

# Bildschirmschoner deaktivieren
$desktop = "HKCU:\Control Panel\Desktop"
Set-ItemProperty -Path $desktop -Name "ScreenSaveActive"    -Value "0" -Type String
Set-ItemProperty -Path $desktop -Name "ScreenSaverIsSecure" -Value "0" -Type String
Set-ItemProperty -Path $desktop -Name "ScreenSaveTimeout"   -Value "0" -Type String

# Toast-Benachrichtigungen deaktivieren
$p = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications"
if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
Set-ItemProperty -Path $p -Name "ToastEnabled" -Value 0 -Type DWord

# Action Center / Benachrichtigungscenter deaktivieren
$p = "HKCU:\SOFTWARE\Policies\Microsoft\Windows\Explorer"
if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
Set-ItemProperty -Path $p -Name "DisableNotificationCenter" -Value 1 -Type DWord

# Windows-Tipps, Vorschlaege und Spotlight deaktivieren
$p = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"
if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
Set-ItemProperty -Path $p -Name "SoftLandingEnabled"            -Value 0 -Type DWord
Set-ItemProperty -Path $p -Name "SubscribedContentEnabled"      -Value 0 -Type DWord
Set-ItemProperty -Path $p -Name "SystemPaneSuggestionsEnabled"  -Value 0 -Type DWord

# Bestehende Singleton-Lock-Dateien entfernen (verhindert "nicht korrekt beendet"-Dialog)
Remove-Item (Join-Path $defaultDir "Singleton*") -Force -ErrorAction SilentlyContinue

& $browser `
    --kiosk $url `
    --user-data-dir=$kioskProfile `
    --noerrdialogs `
    --disable-session-crashed-bubble `
    --disable-infobars `
    --disable-translate `
    --no-first-run `
    --no-default-browser-check `
    --disable-extensions `
    --disable-notifications `
    --disable-default-apps `
    --disable-features=TranslateUI,msTranslateV3 `
    --check-for-update-interval=31536000 `
    --edge-kiosk-type=fullscreen
