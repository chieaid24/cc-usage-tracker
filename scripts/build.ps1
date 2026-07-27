[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root "artifacts"
$publish = Join-Path $artifacts "publish"

Push-Location $root
try {
    dotnet restore CCUsageTracker.slnx
    dotnet build CCUsageTracker.slnx -c $Configuration --no-restore
    dotnet test tests/CCUsageTracker.Tests/CCUsageTracker.Tests.csproj `
        -c $Configuration `
        --no-build `
        --logger "trx;LogFileName=test-results.trx" `
        --results-directory (Join-Path $artifacts "test-results")
    dotnet publish src/CCUsageTracker/CCUsageTracker.csproj `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=false `
        -o $publish

    & (Join-Path $PSScriptRoot "package.ps1") -SkipInstaller
}
finally {
    Pop-Location
}
