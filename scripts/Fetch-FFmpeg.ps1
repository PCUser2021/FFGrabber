[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('x64', 'arm64')]
    [string] $Architecture,
    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)
$ErrorActionPreference = 'Stop'
$headers = @{ 'User-Agent' = 'FFGrabber-build'; 'Accept' = 'application/vnd.github+json' }
$release = Invoke-RestMethod -Uri 'https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest' -Headers $headers
$pattern = if ($Architecture -eq 'arm64') { 'winarm64-gpl' } else { 'win64-gpl' }
$asset = $release.assets | Where-Object { $_.name -match $pattern -and $_.name -match '\.zip
if (-not $asset) { throw "No GPL FFmpeg zip for $Architecture was found in release $($release.tag_name)." }
$checksumAsset = $release.assets | Where-Object { $_.name -match '^checksums\.sha256$' } | Select-Object -First 1
if (-not $checksumAsset) { throw "The selected FFmpeg release has no checksums.sha256 asset; refusing an unverified download." }

$tempRoot = Join-Path $env:RUNNER_TEMP ("ffgrabber-ffmpeg-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $archivePath = Join-Path $tempRoot $asset.name
    $checksumPath = Join-Path $tempRoot 'checksums.sha256'
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $archivePath -Headers @{ 'User-Agent' = 'FFGrabber-build' }
    Invoke-WebRequest -Uri $checksumAsset.browser_download_url -OutFile $checksumPath -Headers @{ 'User-Agent' = 'FFGrabber-build' }
    $matchingLine = Get-Content $checksumPath | Where-Object { $_ -match [regex]::Escape($asset.name) } | Select-Object -First 1
    if (-not $matchingLine) { throw "No SHA-256 checksum found for $($asset.name)." }
    $expectedHash = (($matchingLine.Trim() -replace '^\*', '') -split '\s+')[0].ToLowerInvariant()
    $actualHash = (Get-FileHash -Path $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expectedHash -notmatch '^[0-9a-f]{64}$' -or $actualHash -ne $expectedHash) {
        throw "FFmpeg archive SHA-256 verification failed. Expected $expectedHash, got $actualHash."
    }

    $extractRoot = Join-Path $tempRoot 'extract'
    Expand-Archive -Path $archivePath -DestinationPath $extractRoot -Force
    $ffmpeg = Get-ChildItem -Path $extractRoot -Filter 'ffmpeg.exe' -File -Recurse | Select-Object -First 1
    if (-not $ffmpeg) { throw 'The archive does not contain ffmpeg.exe.' }
    $sourceBin = $ffmpeg.Directory.FullName
    foreach ($tool in @('ffmpeg.exe', 'ffprobe.exe', 'ffplay.exe')) {
        $source = Join-Path $sourceBin $tool
        if (-not (Test-Path $source -PathType Leaf)) { throw "The FFmpeg archive is missing $tool." }
    }

    $bin = Join-Path $OutputDirectory 'bin'
    New-Item -ItemType Directory -Path $bin -Force | Out-Null
    foreach ($tool in @('ffmpeg.exe', 'ffprobe.exe', 'ffplay.exe')) {
        Copy-Item (Join-Path $sourceBin $tool) -Destination $bin -Force
    }
    $licenses = Get-ChildItem -Path $extractRoot -Directory -Recurse | Where-Object { $_.Name -match '^(licenses|license)$' } | Select-Object -First 1
    if ($licenses) { Copy-Item $licenses.FullName -Destination (Join-Path $OutputDirectory 'licenses') -Recurse -Force }
    $metadata = [ordered]@{
        source = 'BtbN/FFmpeg-Builds'
        release_tag = $release.tag_name
        architecture = $Architecture
        archive_name = $asset.name
        archive_url = $asset.browser_download_url
        archive_sha256 = $actualHash
        checksum_verified = $true
        fetched_utc = [DateTime]::UtcNow.ToString('o')
    }
    $metadata | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $OutputDirectory 'ffmpeg-build.json') -Encoding utf8
    Write-Host "Fetched and SHA-256-verified FFmpeg $($release.tag_name) for $Architecture."
}
finally {
    Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
 -and $_.name -notmatch '-shared' } | Select-Object -First 1
if (-not $asset) { throw "No GPL FFmpeg zip for $Architecture was found in release $($release.tag_name)." }
$checksumAsset = $release.assets | Where-Object { $_.name -match '^checksums\.sha256$' } | Select-Object -First 1
if (-not $checksumAsset) { throw "The selected FFmpeg release has no checksums.sha256 asset; refusing an unverified download." }

$tempRoot = Join-Path $env:RUNNER_TEMP ("ffgrabber-ffmpeg-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $archivePath = Join-Path $tempRoot $asset.name
    $checksumPath = Join-Path $tempRoot 'checksums.sha256'
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $archivePath -Headers @{ 'User-Agent' = 'FFGrabber-build' }
    Invoke-WebRequest -Uri $checksumAsset.browser_download_url -OutFile $checksumPath -Headers @{ 'User-Agent' = 'FFGrabber-build' }
    $matchingLine = Get-Content $checksumPath | Where-Object { $_ -match [regex]::Escape($asset.name) } | Select-Object -First 1
    if (-not $matchingLine) { throw "No SHA-256 checksum found for $($asset.name)." }
    $expectedHash = (($matchingLine.Trim() -replace '^\*', '') -split '\s+')[0].ToLowerInvariant()
    $actualHash = (Get-FileHash -Path $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expectedHash -notmatch '^[0-9a-f]{64}$' -or $actualHash -ne $expectedHash) {
        throw "FFmpeg archive SHA-256 verification failed. Expected $expectedHash, got $actualHash."
    }

    $extractRoot = Join-Path $tempRoot 'extract'
    Expand-Archive -Path $archivePath -DestinationPath $extractRoot -Force
    $ffmpeg = Get-ChildItem -Path $extractRoot -Filter 'ffmpeg.exe' -File -Recurse | Select-Object -First 1
    if (-not $ffmpeg) { throw 'The archive does not contain ffmpeg.exe.' }
    $sourceBin = $ffmpeg.Directory.FullName
    foreach ($tool in @('ffmpeg.exe', 'ffprobe.exe', 'ffplay.exe')) {
        $source = Join-Path $sourceBin $tool
        if (-not (Test-Path $source -PathType Leaf)) { throw "The FFmpeg archive is missing $tool." }
    }

    $bin = Join-Path $OutputDirectory 'bin'
    New-Item -ItemType Directory -Path $bin -Force | Out-Null
    foreach ($tool in @('ffmpeg.exe', 'ffprobe.exe', 'ffplay.exe')) {
        Copy-Item (Join-Path $sourceBin $tool) -Destination $bin -Force
    }
    $licenses = Get-ChildItem -Path $extractRoot -Directory -Recurse | Where-Object { $_.Name -match '^(licenses|license)$' } | Select-Object -First 1
    if ($licenses) { Copy-Item $licenses.FullName -Destination (Join-Path $OutputDirectory 'licenses') -Recurse -Force }
    $metadata = [ordered]@{
        source = 'BtbN/FFmpeg-Builds'
        release_tag = $release.tag_name
        architecture = $Architecture
        archive_name = $asset.name
        archive_url = $asset.browser_download_url
        archive_sha256 = $actualHash
        checksum_verified = $true
        fetched_utc = [DateTime]::UtcNow.ToString('o')
    }
    $metadata | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $OutputDirectory 'ffmpeg-build.json') -Encoding utf8
    Write-Host "Fetched and SHA-256-verified FFmpeg $($release.tag_name) for $Architecture."
}
finally {
    Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
