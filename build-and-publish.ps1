$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'WatermarkApp\WatermarkApp.csproj'
$localDotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
$dotnetCmd = 'dotnet'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    if (Test-Path $localDotnet) {
        $dotnetCmd = $localDotnet
    }
    else {
        Write-Error 'dotnet SDK is not found. Install .NET 8 SDK first.'
    }
}

& $dotnetCmd restore $project
& $dotnetCmd publish $project -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

Write-Host ''
Write-Host 'Publish completed.'
Write-Host 'Output:'
Write-Host (Join-Path $PSScriptRoot 'WatermarkApp\bin\Release\net8.0-windows\win-x64\publish\')
