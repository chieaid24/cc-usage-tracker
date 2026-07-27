[CmdletBinding()]
param(
    [switch]$SkipInstaller,
    [string]$InnoSetupPath,
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root "artifacts"
$publish = Join-Path $artifacts "publish"
$portable = Join-Path $artifacts "portable"
$executable = Join-Path $publish "CCUsageTracker.exe"

if (-not (Test-Path $executable)) {
    throw "Published executable not found at $executable. Run scripts/build.ps1 first."
}

if (Test-Path $portable) {
    Remove-Item -LiteralPath $portable -Recurse -Force
}
New-Item -ItemType Directory -Path $portable | Out-Null
Copy-Item $executable (Join-Path $portable "CCUsageTracker.exe")
Copy-Item (Join-Path $root "installer/README.txt") (Join-Path $portable "README.txt")
Copy-Item (Join-Path $root "LICENSE") (Join-Path $portable "LICENSE.txt")

$zipPath = Join-Path $artifacts "CCUsageTracker-win-x64.zip"
Compress-Archive -Path (Join-Path $portable "*") -DestinationPath $zipPath -Force

if (-not $SkipInstaller) {
    if (-not $InnoSetupPath) {
        $InnoSetupPath = Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6/ISCC.exe"
    }
    if (-not (Test-Path $InnoSetupPath)) {
        throw "Inno Setup 6 was not found. Pass -InnoSetupPath with the path to ISCC.exe."
    }

    & $InnoSetupPath "/DMyAppVersion=$Version" (Join-Path $root "installer/CCUsageTracker.iss")
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup failed with exit code $LASTEXITCODE."
    }
}
