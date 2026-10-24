[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$')]
    [string] $Version = '0.5.6'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Initializing FFGrabber $Version verification (x64 + ARM64)..." -ForegroundColor Cyan

# Ensure the test/build run starts from clean app and publish outputs.
$cleanPaths = @(
    (Join-Path $root 'src/FFGrabber/bin'),
    (Join-Path $root 'src/FFGrabber/obj'),
    (Join-Path $root 'tests/FFGrabber.Tests/bin'),
    (Join-Path $root 'tests/FFGrabber.Tests/obj'),
    (Join-Path $root 'artifacts/publish/x64'),
    (Join-Path $root 'artifacts/publish/arm64')
)
foreach ($path in $cleanPaths) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

# FFGrabber is an import-only frontend. Do not accept bundled native FFmpeg executables
# anywhere in the working tree (excluding Git's internal object database).
$prohibitedNames = @('ffmpeg.exe', 'ffprobe.exe', 'ffplay.exe')
$prohibitedItems = @(
    Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -in $prohibitedNames -and
            $_.FullName -notmatch '[\\/]\.git[\\/]'
        }
)
if ($prohibitedItems.Count -gt 0) {
    $paths = $prohibitedItems | ForEach-Object { $_.FullName }
    throw "FFmpeg payload violation: native tool binaries found in source tree: $($paths -join ', ')"
}

Write-Host 'Running mock-based unit and argument-planning tests...' -ForegroundColor Cyan
dotnet test (Join-Path $root 'tests/FFGrabber.Tests/FFGrabber.Tests.csproj') -c Release
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE."
}

Write-Host 'Building and validating x64...' -ForegroundColor Cyan
& (Join-Path $root 'scripts/Build.ps1') -Architecture x64 -Version $Version

Write-Host 'Building and validating ARM64...' -ForegroundColor Cyan
& (Join-Path $root 'scripts/Build.ps1') -Architecture arm64 -Version $Version

Write-Host 'Verification completed: tests passed and x64/ARM64 publish builds completed.' -ForegroundColor Green
