@echo off
REM ═══════════════════════════════════════════════════════════════════════════════
REM ULTRA-FAST RENAME: Alle MaterialManager_R03 → MaterialManager_V01
REM Mit PowerShell für ALLE Dateien gleichzeitig
REM © 2025 Alexander Hölzer
REM ═══════════════════════════════════════════════════════════════════════════════

cls
echo.
echo ╔═══════════════════════════════════════════════════════════════════════════════╗
echo ║                   ⚡ ULTRA-FAST MASS-RENAME: R03 ^→ V01                    ║
echo ║                                                                               ║
echo ║              Ändert ALLE Dateien in diesem Ordner                            ║
echo ╚═══════════════════════════════════════════════════════════════════════════════╝
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"$files = Get-ChildItem -Recurse -Include *.cs, *.xaml, *.csproj, *.sln, *.txt, *.md | Where-Object { $_.FullName -notmatch 'obj\\|bin\\' }; $count = 0; foreach ($file in $files) { $content = Get-Content $file.FullName -Raw -Encoding UTF8; if ($content -match 'MaterialManager_R03') { $content = $content -replace 'MaterialManager_R03', 'MaterialManager_V01' -replace 'namespace MaterialManager_R03', 'namespace MaterialManager_V01' -replace 'using MaterialManager_R03', 'using MaterialManager_V01'; Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -Force; Write-Host \"✓ $($file.Name)\" -ForegroundColor Green; $count++ } } Write-Host \"`n✓ $count Dateien aktualisiert!\" -ForegroundColor Green"

echo.
echo ═══════════════════════════════════════════════════════════════════════════════
echo ✅ MASS-RENAME KOMPLETT!
echo ═══════════════════════════════════════════════════════════════════════════════
echo.

pause
