@echo off
:: Admin-Rechte prüfen
net session >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] Admin-Rechte bestätigt.
) else (
    echo [FEHLER] Bitte starte diese Datei als ADMINISTRATOR!
    pause
    exit
)

set /p serverip="Bitte pandoraVPN Server IP eingeben: "
pandoraClient.exe %serverip%
pause
