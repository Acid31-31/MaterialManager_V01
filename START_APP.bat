@echo off
title MaterialManager starten
cd /d "%~dp0"
echo Baue und starte MaterialManager...
dotnet run --project MaterialManager_V01.csproj
pause
