$root = 'D:\MaterialManager_V01_komplett'
$files = Get-ChildItem -Path $root -Filter '*.xaml' -Recurse |
    Where-Object { $_.FullName -notmatch '\\Backup\\|\\USB_Installation\\|\\publish_|\\obj\\|\\bin\\' }

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    if ($null -eq $content -or $content -notmatch 'AppButton3D') { continue }
    $original = $content

    # Fixed Height on inset buttons clips content -> use MinHeight instead
    $content = $content -replace '(<Button\b[^>]*?)\sHeight="(2[6-9]|3[0-9]|4[0-6])"', '$1 MinHeight="$2"'

    if ($content -ne $original) {
        Set-Content -LiteralPath $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Output $file.FullName.Substring($root.Length)
    }
}

Write-Output 'Done.'
