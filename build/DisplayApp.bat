@echo off
REM Pfad zu Chrome anpassen
set CHROME_PATH="C:\Program Files\Google\Chrome\Application\chrome.exe"

REM URL
set URL=https://localhost:7169/

echo Starte Chrome im Kiosk-Modus...

rem start "" %CHROME_PATH% --kiosk %URL% --no-first-run --disable-extensions --disable-infobars

rem start "" %CHROME_PATH% --kiosk --window-size=1920,1080 --app=%URL% --no-first-run --disable-extensions --disable-infobars

rem start "" %CHROME_PATH% --kiosk --window-size=1200,800 --app=%URL% --no-first-run --disable-extensions --disable-infobars

rem start "" %CHROME_PATH% --kiosk --window-size=1080,1920 --app=%URL% --no-first-run --disable-extensions --disable-infobars

start "" %CHROME_PATH% --window-size=800,1080 --app=%URL% --no-first-run --disable-extensions --disable-infobars


echo.
echo Druecke eine Taste, um den Browser zu beenden...
pause >nul

echo Beende Chrome...
taskkill /IM chrome.exe /F

echo Fertig.