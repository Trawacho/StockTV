@echo off
REM Pfad zu Chrome anpassen, je nachdem, wo es installiert ist
set CHROME_PATH="C:\Program Files\Google\Chrome\Application\chrome.exe"

REM URL, die geladen werden soll
set URL=http://localhost:8080
rem set URL=https://localhost:7169/

REM Kiosk starten (Fullscreen, ohne Menü)
%CHROME_PATH% --kiosk %URL% --no-first-run --disable-extensions --disable-infobars --incognito 
rem %CHROME_PATH% --kiosk --window-size=1920,1080 --app=%URL% --no-first-run --disable-extensions --disable-infobars


REM Alternative: Headless-Modus (ohne GUI, nützlich für automatisierte Tests)
rem %CHROME_PATH% --headless --disable-gpu --remote-debugging-port=9222 --no-sandbox --window-size=1920,1080 %URL%