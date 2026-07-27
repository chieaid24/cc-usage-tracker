[CmdletBinding()]
param(
    [string]$InnoSetupPath,
    [string]$Version = "0.1.0",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root "artifacts"

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot "build.ps1")
}
& (Join-Path $PSScriptRoot "package.ps1") -InnoSetupPath $InnoSetupPath -Version $Version

$assetNames = @(
    "CCUsageTracker-Setup-x64.exe",
    "CCUsageTracker-win-x64.zip"
)
$checksumLines = foreach ($name in $assetNames) {
    $path = Join-Path $artifacts $name
    if (-not (Test-Path $path)) {
        throw "Release asset not found: $path"
    }
    $hash = (Get-FileHash -Algorithm SHA256 $path).Hash.ToLowerInvariant()
    "$hash  $name"
}
$checksumLines | Set-Content -Encoding ascii (Join-Path $artifacts "SHA256SUMS.txt")
