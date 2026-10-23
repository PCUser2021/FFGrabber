[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('x64', 'arm64')]
    [string] $Architecture,
    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'

# FFmpeg 9.0.2 is intentionally pinned for reproducible releases.
# x64 uses the exact Gyan full_build archive requested for FFGrabber.
# That archive is x64-only; ARM64 uses a separately built, architecture-matched
# FFmpeg 9.0.2 GPL archive from BtbN. Never install x64 executables in ARM64 packages.
$builds = @{
    x64 = [pscustomobject]@{
        Source = 'GyanD/codexffmpeg'
        Version = '9.0.2'
        ReleaseTag = '9.0.2'
        Commit = '946fcce07b'
        Variant = 'full GPL release build'
        ArchiveName = 'ffmpeg-9.0.2-full_build.zip'
        Url = 'https://github.com/GyanD/codexffmpeg/releases/download/9.0.2/ffmpeg-9.0.2-full_build.zip'
        Sha256 = '759d0a9831c436a0eb331ad36f236c06cb04aaa0005da46571f0e9d3d9206f6b'
    }
    arm64 = [pscustomobject]@{
        Source = 'BtbN/FFmpeg-Builds'
        Version = '9.0.2'
        ReleaseTag = 'autobuild-2026-10-23-13-04'
        Commit = '9a9b70bb8c'
        Variant = 'static GPL build; 9.0.2 release branch'
        ArchiveName = 'ffmpeg-n9.0.2-35-g9a9b70bb8c-winarm64-gpl-9.0.zip'
        Url = 'https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-10-23-13-04/ffmpeg-n9.0.2-35-g9a9b70bb8c-winarm64-gpl-9.0.zip'
        Sha256 = 'a80c141986b846186550254a26d08cd289794f97df3c25a9bca35a1ee6e11e5f'
    }
}

$build = $builds[$Architecture]
Write-Host "Fetching pinned FFmpeg $($build.Version) for Windows $Architecture from $($build.Source)."
Write-Host "Archive: $($build.ArchiveName)"
Write-Host "SHA-256 pin: $($build.Sha256)"

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("ffgrabber-ffmpeg-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $archivePath = Join-Path $tempRoot $build.ArchiveName
    Invoke-WebRequest -Uri $build.Url -OutFile $archivePath -Headers @{ 'User-Agent' = 'FFGrabber-build' }

    $actualHash = (Get-FileHash -Path $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $build.Sha256) {
        throw "FFmpeg archive SHA-256 verification failed for $($build.ArchiveName). Expected $($build.Sha256), got $actualHash."
    }

    $extractRoot = Join-Path $tempRoot 'extract'
    Expand-Archive -Path $archivePath -DestinationPath $extractRoot -Force
    $ffmpeg = Get-ChildItem -Path $extractRoot -Filter 'ffmpeg.exe' -File -Recurse | Select-Object -First 1
    if (-not $ffmpeg) {
        throw 'The selected archive does not contain ffmpeg.exe.'
    }

    $sourceBin = $ffmpeg.Directory.FullName
    foreach ($tool in @('ffmpeg.exe', 'ffprobe.exe', 'ffplay.exe')) {
        if (-not (Test-Path (Join-Path $sourceBin $tool) -PathType Leaf)) {
            throw "The FFmpeg archive is missing $tool."
        }
    }

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    $bin = Join-Path $OutputDirectory 'bin'
    New-Item -ItemType Directory -Path $bin -Force | Out-Null
    foreach ($tool in @('ffmpeg.exe', 'ffprobe.exe', 'ffplay.exe')) {
        Copy-Item (Join-Path $sourceBin $tool) -Destination $bin -Force
    }

    $licenseDir = Join-Path $OutputDirectory 'licenses'
    $licenseFiles = @(Get-ChildItem -Path $extractRoot -File -Recurse |
        Where-Object { $_.Name -match '^(LICENSE|COPYING|NOTICE|README)(\.|-|$)' } |
        Select-Object -First 100)
    if ($licenseFiles.Count -gt 0) {
        New-Item -ItemType Directory -Path $licenseDir -Force | Out-Null
        foreach ($licenseFile in $licenseFiles) {
            $destination = Join-Path $licenseDir $licenseFile.Name
            if (-not (Test-Path $destination)) {
                Copy-Item -LiteralPath $licenseFile.FullName -Destination $destination
            }
        }
    }

    $metadata = [ordered]@{
        source = $build.Source
        stable_version = $build.Version
        release_tag = $build.ReleaseTag
        source_commit = $build.Commit
        architecture = $Architecture
        variant = $build.Variant
        archive_name = $build.ArchiveName
        archive_url = $build.Url
        archive_sha256 = $actualHash
        checksum_verified = $true
        fetched_utc = [DateTime]::UtcNow.ToString('o')
        feature_availability_note = 'The full GPL build is feature-rich, but no FFmpeg binary can include every optional library, hardware backend, proprietary component, patent-restricted codec or device driver. The GUI queries this exact binary for its available capabilities.'
    }
    $metadata | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $OutputDirectory 'ffmpeg-build.json') -Encoding utf8
    Write-Host "Fetched and SHA-256-verified FFmpeg $($build.Version) for $Architecture."
}
finally {
    Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
