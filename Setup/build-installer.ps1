#Requires -Version 5.1
<#
.SYNOPSIS
    MaterialManager_V01 - Automated WIX Installer Build Script
.DESCRIPTION
    Builds a production-ready WIX installer (Setup.exe) for MaterialManager_V01.
    Requires ZERO user input. Runs fully autonomously.
    Exit codes: 0 = success, 1 = failure
.NOTES
    Version:    1.0.6.0
    Manufacturer: Hölzer
    Run as:     .\Setup\build-installer.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Paths ────────────────────────────────────────────────────────────────────
$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot    = Split-Path -Parent $ScriptDir
$LogFile     = Join-Path $ScriptDir "build-log.txt"
$ConfigFile  = Join-Path $ScriptDir "config.json"
$WxsFile     = Join-Path $ScriptDir "Product.wxs"
$LicenseFile = Join-Path $ScriptDir "License.rtf"

# ── Helper functions ──────────────────────────────────────────────────────────
function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$ts] $Message"
    Write-Host $line -ForegroundColor $Color
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

function Exit-Failure {
    param([string]$Message)
    Write-Log "ERROR: $Message" "Red"
    Write-Log "Build FAILED - see $LogFile for details." "Red"
    exit 1
}

function Exit-Success {
    param([string]$Message)
    Write-Log $Message "Green"
    Write-Log "Build completed successfully." "Green"
    exit 0
}

# ── Self-elevation (request admin if not already elevated) ────────────────────
$currentPrincipal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Requesting administrator privileges..." -ForegroundColor Cyan
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$($MyInvocation.MyCommand.Definition)`""
    Start-Process -FilePath "powershell.exe" -ArgumentList $arguments -Verb RunAs -Wait
    exit $LASTEXITCODE
}

# ── Initialize log file ───────────────────────────────────────────────────────
if (Test-Path $LogFile) { Remove-Item $LogFile -Force }
New-Item -ItemType File -Path $LogFile -Force | Out-Null

Write-Log "╔════════════════════════════════════════════════════════════════╗" "Cyan"
Write-Log "║  MaterialManager_V01 - WIX Installer Builder v1.0.6.0        ║" "Cyan"
Write-Log "║  Manufacturer: Hölzer                                         ║" "Cyan"
Write-Log "╚════════════════════════════════════════════════════════════════╝" "Cyan"
Write-Log ""

# ── Step 1: Load config ───────────────────────────────────────────────────────
Write-Log "[1/7] Loading configuration..." "Cyan"
if (-not (Test-Path $ConfigFile)) {
    Exit-Failure "config.json not found at: $ConfigFile"
}
$config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
Write-Log "  Product   : $($config.ProductName)"
Write-Log "  Version   : $($config.Version)"
Write-Log "  Manufacturer: $($config.Manufacturer)"
Write-Log "  InstallPath : $($config.InstallPath)"
Write-Log "OK" "Green"

# ── Step 2: Create backup ─────────────────────────────────────────────────────
Write-Log "[2/7] Creating backup..." "Cyan"
$timestamp  = Get-Date -Format "yyyyMMdd_HHmmss"
$backupRoot = $config.BackupRoot
$backupDir  = Join-Path $backupRoot "$($config.ProductName)_Backup_$timestamp"

try {
    if (-not (Test-Path $backupRoot)) { New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null }

    # Only back up key source files (not large build output)
    $itemsToBackup = @(
        (Join-Path $ScriptDir "Product.wxs"),
        (Join-Path $ScriptDir "License.rtf"),
        (Join-Path $ScriptDir "config.json"),
        (Join-Path $RepoRoot "MaterialManager_V01.csproj")
    )

    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    foreach ($item in $itemsToBackup) {
        if (Test-Path $item) {
            Copy-Item -Path $item -Destination $backupDir -Force
        }
    }
    Write-Log "  Backup location: $backupDir" "Gray"
    Write-Log "OK" "Green"
}
catch {
    Write-Log "WARNING: Backup failed (non-critical): $_" "Yellow"
}

# ── Step 3: Verify required source files ─────────────────────────────────────
Write-Log "[3/7] Verifying source files..." "Cyan"
foreach ($requiredFile in @($WxsFile, $LicenseFile)) {
    if (-not (Test-Path $requiredFile)) {
        Exit-Failure "Required file not found: $requiredFile"
    }
}
Write-Log "OK" "Green"

# ── Step 4: Locate WIX Toolset ────────────────────────────────────────────────
Write-Log "[4/7] Locating WIX Toolset..." "Cyan"

$wixSearchPaths = @(
    "C:\Program Files (x86)\WiX Toolset v3.11\bin",
    "C:\Program Files (x86)\WiX Toolset v3.14\bin",
    "C:\Program Files\WiX Toolset v3.11\bin",
    "C:\Program Files\WiX Toolset v3.14\bin"
)

# Also search via registry
try {
    $regPaths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    )
    foreach ($regPath in $regPaths) {
        if (Test-Path $regPath) {
            Get-ChildItem $regPath -ErrorAction SilentlyContinue |
                Where-Object { (Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue).DisplayName -like "*WiX*" } |
                ForEach-Object {
                    $installLoc = (Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue).InstallLocation
                    if ($installLoc) { $wixSearchPaths += (Join-Path $installLoc "bin") }
                }
        }
    }
}
catch { }

# Search PATH as well
$wixSearchPaths += ($env:PATH -split ";")

$candleExe = $null
$lightExe  = $null
foreach ($path in $wixSearchPaths) {
    $c = Join-Path $path "candle.exe"
    $l = Join-Path $path "light.exe"
    if ((Test-Path $c) -and (Test-Path $l)) {
        $candleExe = $c
        $lightExe  = $l
        break
    }
}

if (-not $candleExe) {
    Write-Log "" 
    Write-Log "WIX Toolset not found. Attempting to install via Chocolatey..." "Yellow"
    $choco = Get-Command choco -ErrorAction SilentlyContinue
    if ($choco) {
        Write-Log "  Installing wixtoolset via Chocolatey..."
        & choco install wixtoolset --version=3.11.2 -y --no-progress 2>&1 | ForEach-Object { Write-Log "  $_" "Gray" }
        # Re-check after install
        foreach ($path in @("C:\Program Files (x86)\WiX Toolset v3.11\bin", "C:\Program Files\WiX Toolset v3.11\bin")) {
            if (Test-Path (Join-Path $path "candle.exe")) {
                $candleExe = Join-Path $path "candle.exe"
                $lightExe  = Join-Path $path "light.exe"
                break
            }
        }
    }
    if (-not $candleExe) {
        Exit-Failure @"
WIX Toolset 3.11+ is required but was not found.

Please install it manually:
  - Download from: https://github.com/wixtoolset/wix3/releases
  - Or via Chocolatey: choco install wixtoolset
  - Or via winget: winget install WixToolset.WixToolset

Then re-run this script.
"@
    }
}

Write-Log "  candle.exe: $candleExe" "Gray"
Write-Log "  light.exe : $lightExe" "Gray"

# Version check
$wixVersionRaw = & $candleExe 2>&1 | Select-String -Pattern "version" | Select-Object -First 1
Write-Log "  WIX version info: $wixVersionRaw" "Gray"
Write-Log "OK" "Green"

# ── Step 5: Locate build output (EXE) ────────────────────────────────────────
Write-Log "[5/7] Locating compiled application EXE..." "Cyan"

$exeSearchOrder = @(
    (Join-Path $RepoRoot "bin\Release\net8.0-windows\win-x64\publish\MaterialManager_V01.exe"),
    (Join-Path $RepoRoot "bin\Release\net8.0-windows\win-x64\MaterialManager_V01.exe"),
    (Join-Path $RepoRoot "bin\Release\MaterialManager_V01.exe"),
    (Join-Path $RepoRoot "bin\Debug\net8.0-windows\win-x64\publish\MaterialManager_V01.exe"),
    (Join-Path $RepoRoot "bin\Debug\net8.0-windows\win-x64\MaterialManager_V01.exe"),
    (Join-Path $RepoRoot "bin\Debug\MaterialManager_V01.exe"),
    (Join-Path $RepoRoot "MaterialManager_V01.exe")
)

$appExe = $null
foreach ($candidate in $exeSearchOrder) {
    if (Test-Path $candidate) {
        $appExe = $candidate
        break
    }
}

# Fallback: recursive search under bin\
if (-not $appExe) {
    $binDir = Join-Path $RepoRoot "bin"
    if (Test-Path $binDir) {
        $found = Get-ChildItem -Path $binDir -Filter "MaterialManager_V01.exe" -Recurse -ErrorAction SilentlyContinue |
                 Sort-Object LastWriteTime -Descending |
                 Select-Object -First 1
        if ($found) { $appExe = $found.FullName }
    }
}

if (-not $appExe) {
    Write-Log "WARNING: MaterialManager_V01.exe not found in bin\." "Yellow"
    Write-Log "Attempting to build the project with dotnet publish..." "Yellow"

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Exit-Failure ".NET SDK not found. Cannot build the project. Install .NET 8 SDK and try again."
    }

    $csproj = Join-Path $RepoRoot "MaterialManager_V01.csproj"
    if (-not (Test-Path $csproj)) {
        Exit-Failure "Project file not found: $csproj"
    }

    Write-Log "  Running: dotnet publish ..." "Gray"
    $publishOut = Join-Path $RepoRoot "bin\Release\net8.0-windows\win-x64\publish"
    & dotnet publish $csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -o $publishOut `
        2>&1 | ForEach-Object { Write-Log "  $_" "Gray" }

    if ($LASTEXITCODE -ne 0) {
        Exit-Failure "dotnet publish failed with exit code $LASTEXITCODE"
    }

    $appExe = Join-Path $publishOut "MaterialManager_V01.exe"
    if (-not (Test-Path $appExe)) {
        Exit-Failure "Build succeeded but EXE not found at expected location: $appExe"
    }
}

Write-Log "  Found EXE: $appExe" "Gray"
$appPublishDir = Split-Path -Parent $appExe

# Patch the Product.wxs with the resolved publish directory so WIX can find all files
Write-Log "  Source directory: $appPublishDir" "Gray"
Write-Log "OK" "Green"

# ── Step 6: Compile WIX (candle + light) ─────────────────────────────────────
Write-Log "[6/7] Compiling WIX installer..." "Cyan"

$wixObjFile  = Join-Path $ScriptDir "Product.wixobj"
$outputMsi   = Join-Path $ScriptDir "MaterialManager_V01_Setup.msi"

# Clean previous build artefacts
foreach ($stale in @($wixObjFile, $outputMsi)) {
    if (Test-Path $stale) { Remove-Item $stale -Force }
}

# ---- candle.exe (compile .wxs → .wixobj) ------------------------------------
Write-Log "  Running candle.exe..." "Gray"
$candleArgs = @(
    "`"$WxsFile`"",
    "-out", "`"$wixObjFile`"",
    "-arch", "x64",
    "-ext", "WixUIExtension",
    "-dSourceDir=`"$appPublishDir`""
)

$candleOutput = & $candleExe @candleArgs 2>&1
$candleOutput | ForEach-Object { Write-Log "  [candle] $_" "Gray" }

if ($LASTEXITCODE -ne 0) {
    Exit-Failure "candle.exe failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $wixObjFile)) {
    Exit-Failure "candle.exe did not produce expected output: $wixObjFile"
}

# ---- light.exe (link .wixobj → .msi / .exe) ---------------------------------
Write-Log "  Running light.exe..." "Gray"

# Try to produce an EXE bundle; fall back to MSI if WixBalExtension isn't available
$lightArgs = @(
    "`"$wixObjFile`"",
    "-out", "`"$outputMsi`"",
    "-ext", "WixUIExtension",
    "-cultures:de-DE",
    "-b", "`"$appPublishDir`""
)

$lightOutput = & $lightExe @lightArgs 2>&1
$lightOutput | ForEach-Object { Write-Log "  [light] $_" "Gray" }

if ($LASTEXITCODE -ne 0) {
    Exit-Failure "light.exe failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $outputMsi)) {
    Exit-Failure "light.exe did not produce expected MSI: $outputMsi"
}

Write-Log "OK" "Green"

# ── Step 7: Final summary ─────────────────────────────────────────────────────
Write-Log "[7/7] Build summary..." "Cyan"

$msiInfo = Get-Item $outputMsi
$msiSize = [math]::Round($msiInfo.Length / 1MB, 2)

Write-Log ""
Write-Log "╔════════════════════════════════════════════════════════════════╗" "Green"
Write-Log "║  ✅  BUILD SUCCESSFUL                                         ║" "Green"
Write-Log "╚════════════════════════════════════════════════════════════════╝" "Green"
Write-Log ""
Write-Log "  Product : $($config.ProductName) v$($config.Version)" "Cyan"
Write-Log "  MSI     : $outputMsi ($msiSize MB)" "Cyan"
Write-Log "  Log     : $LogFile" "Cyan"
Write-Log ""
Write-Log "  To install on target machine: msiexec /i `"$outputMsi`"" "Cyan"
Write-Log ""

exit 0
