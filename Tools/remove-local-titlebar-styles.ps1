$files = @(
    'D:\MaterialManager_V01_komplett\Views\RegalauslastungDialog.xaml',
    'D:\MaterialManager_V01_komplett\Views\UpdateDialog.xaml',
    'D:\MaterialManager_V01_komplett\Views\EuPaletteDialog.xaml',
    'D:\MaterialManager_V01_komplett\Views\ReservierteResteDialog.xaml',
    'D:\MaterialManager_V01_komplett\Views\NiedrigeBestaendeDialog.xaml'
)

$pattern = '(?s)\s*<Style x:Key="TitleBarButtonStyle" TargetType="Button">.*?</Style>\r?\n?'

foreach ($path in $files) {
    $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    $newContent = [regex]::Replace($content, $pattern, '')
    if ($newContent -ne $content) {
        Set-Content -LiteralPath $path -Value $newContent -Encoding UTF8 -NoNewline
        Write-Output "Removed local TitleBarButtonStyle: $path"
    }
}

Write-Output 'Done.'
