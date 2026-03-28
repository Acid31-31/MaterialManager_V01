param(
    [Parameter(Mandatory = $true)]
    [string]$CertificatePath,

    [Parameter(Mandatory = $true)]
    [string]$CertificatePassword,

    [string]$TimestampUrl = "http://timestamp.digicert.com",

    [switch]$SignInstaller,

    [switch]$SignUpdateInstaller,

    [switch]$SignMainApp,

    [switch]$RemoveExistingSignature
)

$ErrorActionPreference = "Stop"

function Resolve-SignTool {
    $cmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $candidates = @(
        "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe",
        "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
        "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe",
        "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"
    )

    foreach ($path in $candidates) {
        if (Test-Path $path) { return $path }
    }

    throw "signtool.exe nicht gefunden. Bitte Windows SDK installieren."
}

function Sign-File {
    param(
        [string]$SignTool,
        [string]$FilePath
    )

    if (!(Test-Path $FilePath)) {
        Write-Warning "Datei nicht gefunden: $FilePath"
        return
    }

    if ($RemoveExistingSignature) {
        & $SignTool remove /s $FilePath | Out-Null
    }

    & $SignTool sign /fd SHA256 /f $CertificatePath /p $CertificatePassword /tr $TimestampUrl /td SHA256 $FilePath | Out-Null

    $sig = Get-AuthenticodeSignature $FilePath
    Write-Host "$FilePath => $($sig.Status) | $($sig.SignerCertificate.Subject)"
}

if (!(Test-Path $CertificatePath)) {
    throw "Zertifikat nicht gefunden: $CertificatePath"
}

$signTool = Resolve-SignTool
Write-Host "SignTool: $signTool"

$targets = @()

if (!$SignInstaller -and !$SignUpdateInstaller -and !$SignMainApp) {
    $targets += "USB_Installation\MaterialManager\MaterialManager_V01.exe"
    $targets += "USB_Installation\UpdateInstaller.exe"
    $targets += "USB_Installation\Installer.exe"
}
else {
    if ($SignMainApp) { $targets += "USB_Installation\MaterialManager\MaterialManager_V01.exe" }
    if ($SignUpdateInstaller) { $targets += "USB_Installation\UpdateInstaller.exe" }
    if ($SignInstaller) { $targets += "USB_Installation\Installer.exe" }
}

foreach ($target in $targets) {
    Sign-File -SignTool $signTool -FilePath $target
}

Write-Host "Signierung abgeschlossen."
