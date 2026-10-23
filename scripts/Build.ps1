[CmdletBinding()]
param(
    [ValidateSet('x64', 'arm64')]
    [string] $Architecture = 'x64',
    [ValidatePattern('^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$')]
    [string] $Version = '0.1.0',
    [switch] $SkipFfmpegDownload,
    [switch] $Installer
)
$ErrorActionPreference = 'Stop'
if ($Installer -and $SkipFfmpegDownload) { throw 'A complete installer must bundle FFmpeg. Remove -SkipFfmpegDownload when using -Installer.' }
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$rid = if ($Architecture -eq 'arm64') { 'win-arm64' } else { 'win-x64' }
$publish = Join-Path $root "artifacts/publish/$Architecture"
New-Item -ItemType Directory -Path $publish -Force | Out-Null

dotnet publish (Join-Path $root 'src/FFGrabber/FFGrabber.csproj') -c Release -r $rid --self-contained true -p:PublishSingleFile=false "-p:Version=$Version" -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
Copy-Item (Join-Path $root 'LICENSE'), (Join-Path $root 'THIRD-PARTY-NOTICES.md') -Destination $publish -Force
if (-not $SkipFfmpegDownload) {
    & (Join-Path $PSScriptRoot 'Fetch-FFmpeg.ps1') -Architecture $Architecture -OutputDirectory (Join-Path $publish "tools/ffmpeg/$Architecture")
    if ($LASTEXITCODE -ne 0) { throw 'FFmpeg download or checksum verification failed.' }
}
if ($Installer) {
    $iscc = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if (-not $iscc) { throw 'Install Inno Setup 7 from https://jrsoftware.org/isdl.php.' }
    New-Item -ItemType Directory -Path (Join-Path $root 'artifacts/installer') -Force | Out-Null
    & $iscc.Source "/DAppArch=$Architecture" "/DAppVersion=$Version" "/DAppFileVersion=$((($Version -split '[-+]')[0]) + '.0')" (Join-Path $root 'installer/FFGrabber.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compilation failed.' }
}
Write-Host "FFGrabber $Version published to $publish"
