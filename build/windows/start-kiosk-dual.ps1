# ============================================
#  StockTV - Kiosk-Starter
#  Wird vom Scheduled Task 'StockTV Kiosk' bei
#  jeder Benutzeranmeldung ausgefuehrt.
#
#  Ist ein zweiter Bildschirm angeschlossen und
#  Windows auf "Erweitern" gestellt, oeffnet sich
#  dort zusaetzlich die gespiegelte Anzeige
#  (/display2) fuer die gegenueberliegende
#  Bahnseite.
# ============================================

$url       = "http://localhost:__PORT__"
$mirrorUrl = "$url/display2"

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
# Jedes Fenster braucht ein eigenes Profil, sonst oeffnet die zweite Instanz
# nur einen Tab im bereits laufenden Fenster.
function New-KioskProfile {
    param([Parameter(Mandatory = $true)][string]$Path)

    $defaultDir = Join-Path $Path "Default"
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

    # Bestehende Singleton-Lock-Dateien entfernen (verhindert "nicht korrekt beendet"-Dialog)
    Remove-Item (Join-Path $defaultDir "Singleton*") -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $Path "Singleton*")       -Force -ErrorAction SilentlyContinue
}

$kioskProfile  = Join-Path $PSScriptRoot "kiosk-profile"
$mirrorProfile = Join-Path $PSScriptRoot "kiosk-profile-mirror"

New-KioskProfile -Path $kioskProfile

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

# Win32-Helfer: Fensterplatzierung und Fokus deterministisch setzen. Chromium
# platziert mit --window-position zwar meist richtig, garantiert ist das bei
# zwei Bildschirmen aber nicht - deshalb wird nachkorrigiert.
Add-Type -Namespace StockTv -Name Win -MemberDefinition @'
[DllImport("user32.dll")]
public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
[DllImport("user32.dll")]
public static extern bool SetForegroundWindow(IntPtr hWnd);
'@

function Wait-MainWindow {
    param([System.Diagnostics.Process]$Process, [int]$TimeoutSeconds = 20)

    for ($i = 0; $i -lt ($TimeoutSeconds * 4); $i++) {
        if ($Process.HasExited) { return [IntPtr]::Zero }
        $Process.Refresh()
        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) { return $Process.MainWindowHandle }
        Start-Sleep -Milliseconds 250
    }
    return [IntPtr]::Zero
}

# Gemeinsame Browser-Schalter fuer beide Fenster
$commonArgs = @(
    "--noerrdialogs"
    "--disable-session-crashed-bubble"
    "--disable-infobars"
    "--disable-translate"
    "--no-first-run"
    "--no-default-browser-check"
    "--disable-extensions"
    "--disable-notifications"
    "--disable-default-apps"
    "--disable-features=TranslateUI,msTranslateV3"
    "--check-for-update-interval=31536000"
)

# --- Zweite Anzeige (gegenueberliegende Bahnseite) ---------------------------
# Nur wenn Windows einen zweiten Bildschirm meldet (Anzeigemodus "Erweitern").
# Bei "Duplizieren" meldet Windows nur einen Bildschirm - dann bleibt es beim
# Hauptfenster.
Add-Type -AssemblyName System.Windows.Forms
$primary   = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$secondary = [System.Windows.Forms.Screen]::AllScreens | Where-Object { -not $_.Primary } | Select-Object -First 1

$mirrorProc = $null
if ($secondary) {
    New-KioskProfile -Path $mirrorProfile

    $b = $secondary.Bounds

    # Bewusst kein --start-fullscreen: das wuerde das Fenster unter Umstaenden auf
    # dem Hauptbildschirm aufziehen. Groesse und Position decken den zweiten
    # Bildschirm exakt ab, und der Explorer ist beendet - es bleibt also nichts
    # sichtbar ausser der Anzeige.
    $mirrorArgs = @(
        "--user-data-dir=$mirrorProfile"
        "--window-position=$($b.X),$($b.Y)"
        "--window-size=$($b.Width),$($b.Height)"
        "--app=$mirrorUrl"
    ) + $commonArgs

    $mirrorProc = Start-Process -FilePath $browser -ArgumentList $mirrorArgs -PassThru

    $hMirror = Wait-MainWindow -Process $mirrorProc
    if ($hMirror -ne [IntPtr]::Zero) {
        [StockTv.Win]::MoveWindow($hMirror, $b.X, $b.Y, $b.Width, $b.Height, $true) | Out-Null
    }
}

# --- Hauptanzeige (Bedienseite) ---------------------------------------------
$mainArgs = @(
    "--kiosk", $url
    "--user-data-dir=$kioskProfile"
    "--window-position=$($primary.X),$($primary.Y)"
) + $commonArgs + @("--edge-kiosk-type=fullscreen")

$mainProc = Start-Process -FilePath $browser -ArgumentList $mainArgs -PassThru

# Fokus zum Schluss ausdruecklich auf das Bedienfenster - nur dort werden
# Tastatureingaben (Ziffernblock) verarbeitet.
$hMain = Wait-MainWindow -Process $mainProc
if ($hMain -ne [IntPtr]::Zero) {
    Start-Sleep -Milliseconds 500
    [StockTv.Win]::SetForegroundWindow($hMain) | Out-Null
}

# Laufen lassen, solange die Hauptanzeige offen ist (wie beim Einzelschirm-Skript)
$mainProc.WaitForExit()
