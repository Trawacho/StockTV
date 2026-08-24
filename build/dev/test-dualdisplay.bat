@echo off
REM StockTV - Dual-Display-Test starten (Doppelklick moeglich)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0test-dualdisplay.ps1" %*
pause
