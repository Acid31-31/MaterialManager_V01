powershell -NoProfile -ExecutionPolicy Bypass -Command "
Write-Host 'âš¡ MASS-RENAME STARTET!' -ForegroundColor Cyan
Write-Host ''

$path = 'C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_1.0.x'
cd $path

\$files = Get-ChildItem -Path . -Recurse -Include *.cs, *.xaml, *.csproj, *.sln, *.txt, *.md, *.bat, *.ps1 -ErrorAction SilentlyContinue | Where-Object { \$_.FullName -notmatch 'obj\\\\|bin\\\\|\.git' }

Write-Host \"Durchsuche \$(\$files.Count) Dateien...\" -ForegroundColor Yellow

\$count = 0
foreach (\$file in \$files) {
    try {
        \$content = Get-Content \$file.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if (\$null -ne \$content) {
            \$before = \$content
            
            \$content = \$content -replace 'MaterialManager_1.0.x', 'MaterialManager_V01'
            \$content = \$content -replace 'namespace MaterialManager_1.0.x', 'namespace MaterialManager_V01'
            \$content = \$content -replace 'using MaterialManager_1.0.x', 'using MaterialManager_V01'
            \$content = \$content -replace 'MM_1.0.x_SECRET', 'MM_V01_SECRET'
            \$content = \$content -replace 'MaterialManager_1.0.x\.Services', 'MaterialManager_V01.Services'
            \$content = \$content -replace '1.0.x', 'V01'
            
            if (\$content -ne \$before) {
                Set-Content -Path \$file.FullName -Value \$content -Encoding UTF8 -Force
                Write-Host \"âœ“ \$(\$file.Name)\" -ForegroundColor Green
                \$count++
            }
        }
    } catch {
        Write-Host \"âš ï¸ \$(\$file.Name): \$(\$_.Exception.Message)\" -ForegroundColor Yellow
    }
}

Write-Host ''
Write-Host \"âœ… \$count Dateien aktualisiert!\" -ForegroundColor Green
Write-Host ''
Write-Host 'Alle Dateien sind jetzt auf V01 umgestellt!' -ForegroundColor Cyan
"

