@echo off
REM ============================================
REM Lizenzschlüssel Generator
REM ============================================
setlocal enabledelayedexpansion

color 0E
cls

echo.
echo ============================================
echo MaterialManager R03 - Lizenzschlüssel
echo ============================================
echo.

set /p HARDWARE_ID="Geben Sie die Hardware-ID des Kunden ein: "
set /p CUSTOMER_NAME="Kundenname eingeben: "

REM Verwende PowerShell für Lizenzschlüssel-Generierung
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"Add-Type -AssemblyName System.Security; ^
$masterSecret = 'MM_R03_MASTER_SECRET_2025_PRODUCTION'; ^
$hardwareId = '%HARDWARE_ID%'; ^
$customer = '%CUSTOMER_NAME%'; ^
$expiryDate = (Get-Date).AddYears(1).ToString('yyyyMMdd'); ^
$data = '{0}|{1}|{2}|{3}' -f $hardwareId, $customer, $expiryDate, $masterSecret; ^
$hmac = New-Object System.Security.Cryptography.HMACSHA256([Text.Encoding]::UTF8.GetBytes($masterSecret)); ^
$hash = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($data)); ^
$hashString = [Convert]::ToBase64String($hash).Replace('+','').Replace('/','').Replace('=','').Substring(0, 16).ToUpper(); ^
$licenseKey = 'MM-' + $hashString.Substring(0,4) + '-' + $hashString.Substring(4,4) + '-' + $hashString.Substring(8,4) + '-' + $hashString.Substring(12,4); ^
Write-Host ''; ^
Write-Host 'Lizenzschlüssel generiert:' -ForegroundColor Green; ^
Write-Host $licenseKey -ForegroundColor Yellow; ^
Write-Host ''; ^
Write-Host 'Details:' -ForegroundColor Cyan; ^
Write-Host ('  Hardware-ID: {0}' -f $hardwareId); ^
Write-Host ('  Kunde: {0}' -f $customer); ^
Write-Host ('  Gültig bis: {0}' -f $expiryDate); ^
Write-Host '';"

echo.
echo Senden Sie diesen Schlüssel an den Kunden.
echo.
pause
