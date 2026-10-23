[CmdletBinding()]
param(
    [ValidateSet('x64', 'arm64')]
    [string] $Architecture = 'x64',
    [ValidatePattern('^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$')]
    [string] $Version = '0.2.0',
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
    $candidates = [System.Collections.Generic.List[string]]::new()
    $standardPath = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\\ISCC.exe'
    if (Test-Path $standardPath -PathType Leaf) { $candidates.Add($standardPath) }

    $onPath = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($onPath -and -not $candidates.Contains($onPath.Source)) { $candidates.Add($onPath.Source) }

    foreach ($programRoot in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
        if (-not [string]::IsNullOrWhiteSpace($programRoot) -and (Test-Path $programRoot)) {
            $found = Get-ChildItem -Path $programRoot -Filter 'ISCC.exe' -File -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match '[\\/]Inno Setup 7[\\/]' }
            foreach ($item in $found) {
                if (-not $candidates.Contains($item.FullName)) { $candidates.Add($item.FullName) }
            }
        }
    }

    $isccPath = $null
    $requiredInnoVersion = [version]'7.1.0'
    foreach ($candidatePath in $candidates) {
        try {
            $versionText = (Get-Item -LiteralPath $candidatePath).VersionInfo.ProductVersion
            if ($versionText -match '^(\\d+\\.\\d+\\.\\d+)') {
                $detectedVersion = [version]::Parse($Matches[1])
                if ($detectedVersion -ge $requiredInnoVersion) {
                    $isccPath = $candidatePath
                    Write-Host "Using Inno Setup $detectedVersion at $isccPath"
                    break
                }
            }
        } catch {
            Write-Verbose "Skipping Inno Setup candidate '$candidatePath': $($_.Exception.Message)"
        }
    }

    if (-not $isccPath) {
        throw 'Inno Setup 7.1.0 or newer is required for local installer builds. Install the latest official stable version from https://jrsoftware.org/isdl.php.'
    }
    New-Item -ItemType Directory -Path (Join-Path $root 'artifacts/installer') -Force | Out-Null
    & $isccPath "/DAppArch=$Architecture" "/DAppVersion=$Version" "/DAppFileVersion=$((($Version -split '[-+]')[0]) + '.0')" (Join-Path $root 'installer/FFGrabber.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compilation failed.' }
}
Write-Host "FFGrabber $Version published to $publish"
