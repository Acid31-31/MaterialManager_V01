@echo off
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
REM ULTRA-FAST RENAME: Alle MaterialManager_1.0.x â†’ MaterialManager_V01
REM Mit PowerShell fÃ¼r ALLE Dateien gleichzeitig
REM Â© 2025 Alexander HÃ¶lzer
REM â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

cls
echo.
echo â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
echo â•‘                   âš¡ ULTRA-FAST MASS-RENAME: 1.0.x ^â†’ V01                    â•‘
echo â•‘                                                                               â•‘
echo â•‘              Ã„ndert ALLE Dateien in diesem Ordner                            â•‘
echo â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
"$files = Get-ChildItem -Recurse -Include *.cs, *.xaml, *.csproj, *.sln, *.txt, *.md | Where-Object { $_.FullName -notmatch 'obj\\|bin\\' }; $count = 0; foreach ($file in $files) { $content = Get-Content $file.FullName -Raw -Encoding UTF8; if ($content -match 'MaterialManager_1.0.x') { $content = $content -replace 'MaterialManager_1.0.x', 'MaterialManager_V01' -replace 'namespace MaterialManager_1.0.x', 'namespace MaterialManager_V01' -replace 'using MaterialManager_1.0.x', 'using MaterialManager_V01'; Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -Force; Write-Host \"âœ“ $($file.Name)\" -ForegroundColor Green; $count++ } } Write-Host \"`nâœ“ $count Dateien aktualisiert!\" -ForegroundColor Green"

echo.
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo âœ… MASS-RENAME KOMPLETT!
echo â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
echo.

pause

