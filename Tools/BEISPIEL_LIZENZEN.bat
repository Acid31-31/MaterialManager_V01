@REM Beispiel-Lizenzen generieren für schnellen Test
@REM Entfernen Sie das "@REM" am Anfang jeder Zeile, um den Test auszuführen

@REM BEISPIEL 1: Standard 1-Jahres-Lizenz
@REM dotnet run "ABC123DEF456GHI789JKL012MNO345PQR" "Musterfirma GmbH" 1

@REM BEISPIEL 2: 3-Jahres-Betriebslizenz
@REM dotnet run "XYZ789ABC456DEF123GHI654JKL321MNO" "Großunternehmen AG" 3

@REM BEISPIEL 3: Kleines Startup
@REM dotnet run "TEST123456789ABCDEFGHIJKLMNOPQRST" "StartUp GmbH" 1

@REM BEISPIEL 4: Demo/Test-Lizenz
@REM dotnet run "DEMO123456789DEMO123456789DEMO12345" "Demo Firma" 1

@echo off
echo.
echo Diese Datei zeigt Beispiele fuer Lizenzgenerierung
echo.
echo Um einen Test durchzufuehren:
echo.
echo 1. Diese Datei oeffnen (mit Editor)
echo 2. Die "@REM " am Anfang einer Zeile loeschen
echo 3. Die Zeile in PowerShell/CMD ausfuehren
echo 4. oder: Diese BAT-Datei anpassen und ausfuehren
echo.
pause
