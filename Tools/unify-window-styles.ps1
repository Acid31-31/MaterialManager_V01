$root = 'D:\MaterialManager_V01_komplett'
$files = Get-ChildItem -Path $root -Filter '*.xaml' -Recurse |
    Where-Object { $_.FullName -notmatch '\\Backup\\|\\USB_Installation\\|\\publish_|\\obj\\|\\bin\\' }

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    if ($null -eq $content) { continue }
    $original = $content

    # Window backgrounds
    $content = $content -replace '(<Window\b[^>]*?\s)Background="#(?:0B0B0B|0A0A0A|0F0F0F|1E1E1E|10151A)"', '$1Background="{DynamicResource ThemeWindowBackgroundBrush}"'
    $content = $content -replace '(<Window\b[^>]*?\s)Foreground="White"', '$1Foreground="{DynamicResource ThemeForegroundBrush}"'
    $content = $content -replace '(<Window\b[^>]*?\s)Foreground="Black"', '$1Foreground="{DynamicResource ThemeForegroundBrush}"'

    # Common borders
    $content = $content -replace 'BorderBrush="#2A2A2A"', 'BorderBrush="{DynamicResource ThemeBorderBrush}"'
    $content = $content -replace 'BorderBrush="#333(?:333)?"', 'BorderBrush="{DynamicResource ThemeBorderBrush}"'

    # Shell / outer border backgrounds
    $content = $content -replace '(<Border\b[^>]*?\s)Background="#0B0B0B"', '$1Background="{DynamicResource ThemeWindowBackgroundBrush}"'

    # Title bars
    $content = $content -replace '(<Border\b[^>]*?\s)Background="#(?:111111|1F1F1F)"', '$1Background="{DynamicResource ThemeSurfaceBrush}"'

    # Content panels
    $content = $content -replace '(<Border\b[^>]*?\s)Background="#(?:1E1E1E|1A1A1A|222222|222)"', '$1Background="{DynamicResource ThemeAltSurfaceBrush}"'

    # Menu bars in module windows
    $content = $content -replace '(<Style TargetType="Menu">\s*<Setter Property="Background" Value=")#(?:111111|1F1F1F)("\s*/>)', '${1}{DynamicResource ThemeSurfaceBrush}${2}'

    if ($content -ne $original) {
        Set-Content -LiteralPath $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Output "Updated: $($file.FullName.Substring($root.Length))"
    }
}

Write-Output 'Done.'
