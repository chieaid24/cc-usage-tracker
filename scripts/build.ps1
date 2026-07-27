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
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }
    dotnet build CCUsageTracker.slnx -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }
    dotnet test tests/CCUsageTracker.Tests/CCUsageTracker.Tests.csproj `
        -c $Configuration `
        --no-build `
        --logger "trx;LogFileName=test-results.trx" `
        --results-directory (Join-Path $artifacts "test-results")
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE."
    }
    dotnet publish src/CCUsageTracker/CCUsageTracker.csproj `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=false `
        -o $publish
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    & (Join-Path $PSScriptRoot "package.ps1") -SkipInstaller
}
finally {
    Pop-Location
}
