powershell -NoProfile -ExecutionPolicy Bypass -Command "
Write-Host '⚡ MASS-RENAME STARTET!' -ForegroundColor Cyan
Write-Host ''

$path = 'C:\Users\hoelz.WIN-G2OC48399EJ\MaterialManager_R03'
cd $path

\$files = Get-ChildItem -Path . -Recurse -Include *.cs, *.xaml, *.csproj, *.sln, *.txt, *.md, *.bat, *.ps1 -ErrorAction SilentlyContinue | Where-Object { \$_.FullName -notmatch 'obj\\\\|bin\\\\|\.git' }

Write-Host \"Durchsuche \$(\$files.Count) Dateien...\" -ForegroundColor Yellow

\$count = 0
foreach (\$file in \$files) {
    try {
        \$content = Get-Content \$file.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if (\$null -ne \$content) {
            \$before = \$content
            
            \$content = \$content -replace 'MaterialManager_R03', 'MaterialManager_V01'
            \$content = \$content -replace 'namespace MaterialManager_R03', 'namespace MaterialManager_V01'
            \$content = \$content -replace 'using MaterialManager_R03', 'using MaterialManager_V01'
            \$content = \$content -replace 'MM_R03_SECRET', 'MM_V01_SECRET'
            \$content = \$content -replace 'MaterialManager_R03\.Services', 'MaterialManager_V01.Services'
            \$content = \$content -replace 'R03', 'V01'
            
            if (\$content -ne \$before) {
                Set-Content -Path \$file.FullName -Value \$content -Encoding UTF8 -Force
                Write-Host \"✓ \$(\$file.Name)\" -ForegroundColor Green
                \$count++
            }
        }
    } catch {
        Write-Host \"⚠️ \$(\$file.Name): \$(\$_.Exception.Message)\" -ForegroundColor Yellow
    }
}

Write-Host ''
Write-Host \"✅ \$count Dateien aktualisiert!\" -ForegroundColor Green
Write-Host ''
Write-Host 'Alle Dateien sind jetzt auf V01 umgestellt!' -ForegroundColor Cyan
"
