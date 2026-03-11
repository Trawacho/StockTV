@echo off
echo ==========================================
echo   StockTvBlazor Local DEV Build
echo ==========================================
echo.

REM Speichere aktuellen Ordner (build)
set BUILD_DIR=%~dp0

REM In Projekt-Root wechseln
cd /d %BUILD_DIR%..

REM Buildx Builder erstellen (falls noch nicht vorhanden)
docker buildx create --use --name devbuilder 2>nul
docker buildx inspect --bootstrap

REM Dynamisches Tag erzeugen: StockTV_vYYYY.MM.DD.HHMM
for /f "tokens=1-6 delims=.:/ " %%a in ("%date% %time%") do (
    set YYYY=%%c
    set MM=%%a
    set DD=%%b
    set HH=%%d
    set MIN=%%e
)
set VERSION_TAG=StockTV_v%YYYY%.%MM%.%DD%.%HH%%MIN%

echo ==========================================
echo   DEV Build: amd64 lokal
echo ==========================================

REM Build nur für amd64 und lokal laden
docker buildx build ^
  --platform linux/amd64 ^
  -f build/Dockerfile ^
  -t stocktv:%VERSION_TAG% ^
  --load ^
  .

echo.
echo DEV Build abgeschlossen! Image: stocktv:%VERSION_TAG%

REM docker-compose im build-Ordner starten
echo Starte Container mit docker-compose...
cd /d %BUILD_DIR%
set STOCKTV_IMAGE=stocktv:%VERSION_TAG%
docker-compose up -d --build

pause