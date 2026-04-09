param(
    [Parameter(Mandatory = $true)]
    [string]$CertificatePath,

    [Parameter(Mandatory = $true)]
    [string]$CertificatePassword,

    [string]$TimestampUrl = "http://timestamp.digicert.com",

    [switch]$SignInstaller,

    [switch]$SignUpdateInstaller,

    [switch]$SignMainApp,

    [switch]$RemoveExistingSignature,

    [switch]$SkipPublicCertificateExport,

    [string]$PublicCertificateOutput = "USB_Installation\MaterialManager_CodeSigning_PUBLIC.cer"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

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

function Resolve-RepoPath {
    param([string]$RelativePath)

    if ([System.IO.Path]::IsPathRooted($RelativePath)) {
        return $RelativePath
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $RelativePath))
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

function Export-PublicCertificate {
    param([string]$OutputPath)

    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
    $cert.Import($CertificatePath, $CertificatePassword, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)

    $dir = Split-Path -Parent $OutputPath
    if (![string]::IsNullOrWhiteSpace($dir) -and !(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir | Out-Null
    }

    Export-Certificate -Cert $cert -FilePath $OutputPath -Force | Out-Null
    Write-Host "Öffentliches Zertifikat exportiert: $OutputPath"
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
    $resolvedTarget = Resolve-RepoPath $target
    Sign-File -SignTool $signTool -FilePath $resolvedTarget
}

if (!$SkipPublicCertificateExport) {
    $publicCertPath = Resolve-RepoPath $PublicCertificateOutput
    Export-PublicCertificate -OutputPath $publicCertPath
}

Write-Host "Signierung abgeschlossen."
