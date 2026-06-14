$root = 'D:\MaterialManager_V01_komplett'
$files = Get-ChildItem -Path $root -Filter '*.xaml' -Recurse |
    Where-Object { $_.FullName -notmatch '\\Backup\\|\\USB_Installation\\|\\publish_|\\obj\\|\\bin\\' }

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    if ($null -eq $content) { continue }
    $original = $content

    $content = $content -replace '(<Grid\b[^>]*?\s)Background="#0B0B0B"', '$1Background="{DynamicResource ThemeWindowBackgroundBrush}"'
    $content = $content -replace ' Background="#222"', ' Background="{DynamicResource ThemeSurfaceBrush}"'
    $content = $content -replace ' Background="#1A1A1A"', ' Background="{DynamicResource ThemeAltSurfaceBrush}"'
    $content = $content -replace ' Background="#1E1E1E"', ' Background="{DynamicResource ThemeAltSurfaceBrush}"'
    $content = $content -replace ' Background="#111111"', ' Background="{DynamicResource ThemeSurfaceBrush}"'
    $content = $content -replace ' Foreground="White"', ' Foreground="{DynamicResource ThemeForegroundBrush}"'
    $content = $content -replace ' BorderBrush="#444"', ' BorderBrush="{DynamicResource ThemeBorderBrush}"'
    $content = $content -replace 'Background="#F5F5F5"', 'Background="{DynamicResource ThemeWindowBackgroundBrush}"'

    if ($content -ne $original) {
        Set-Content -LiteralPath $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Output "Updated: $($file.FullName.Substring($root.Length))"
    }
}

Write-Output 'Done pass 2.'
