[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('x64', 'arm64')]
    [string] $Architecture,
    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$headers = @{
    'User-Agent' = 'FFGrabber-build'
    'Accept' = 'application/vnd.github+json'
    'X-GitHub-Api-Version' = '2022-11-28'
}
# Use the workflow's short-lived GitHub token when available to avoid anonymous API rate limits.
if (-not [string]::IsNullOrWhiteSpace($env:GH_TOKEN)) {
    $headers['Authorization'] = "Bearer $($env:GH_TOKEN)"
}

# Intentionally select the newest *stable release branch* build, never the moving master/development build.
# BtbN's daily stable builds include versioned filenames such as
# ffmpeg-n9.0.2-35-g<commit>-win64-gpl-9.0.zip.
$architectureToken = if ($Architecture -eq 'arm64') { 'winarm64' } else { 'win64' }
$pattern = '^ffmpeg-n(?<version>\d+\.\d+(?:\.\d+)?)-(?<distance>\d+)-g(?<commit>[0-9a-f]+)-' +
    [regex]::Escape($architectureToken) + '-gpl-(?<series>\d+(?:\.\d+)?)\.zip$'

Write-Host "Searching BtbN/FFmpeg-Builds stable release branches for Windows $Architecture..."
$releases = $null
$attempt = 0
while ($null -eq $releases) {
    try {
        $releases = Invoke-RestMethod -Uri 'https://api.github.com/repos/BtbN/FFmpeg-Builds/releases?per_page=30' -Headers $headers
    } catch {
        $attempt++
        $statusCode = 0
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        if ($attempt -ge 4 -or ($statusCode -notin @(403, 429, 500, 502, 503, 504))) { throw }
        $delay = [Math]::Min(30, 3 * [Math]::Pow(2, $attempt - 1))
        Write-Warning "GitHub release lookup returned HTTP $statusCode; retrying in $delay seconds (attempt $attempt of 3)."
        Start-Sleep -Seconds ([int]$delay)
    }
}
$candidates = [System.Collections.Generic.List[object]]::new()

foreach ($release in $releases) {
    if ($release.draft -or $release.prerelease) { continue }
    $checksumAsset = $release.assets | Where-Object { $_.name -eq 'checksums.sha256' } | Select-Object -First 1
    if (-not $checksumAsset) { continue }

    foreach ($asset in $release.assets) {
        if ($asset.name -notmatch $pattern) { continue }
        $versionText = $Matches['version']
        $distance = [int]$Matches['distance']
        try { $version = [version]::Parse($versionText) }
        catch { continue }

        $candidates.Add([pscustomobject]@{
            Version = $version
            VersionText = $versionText
            Distance = $distance
            Commit = $Matches['commit']
            Release = $release
            ReleaseDate = [DateTimeOffset]::Parse($release.published_at)
            Asset = $asset
            ChecksumAsset = $checksumAsset
        })
    }
}

$selected = $candidates |
    Sort-Object @{ Expression = { $_.Version }; Descending = $true },
                @{ Expression = { $_.ReleaseDate }; Descending = $true },
                @{ Expression = { $_.Distance }; Descending = $true } |
    Select-Object -First 1

if (-not $selected) {
    throw "No checksummed stable GPL FFmpeg archive for $Architecture was found in the most recent 30 BtbN releases. Refusing to fall back to an unpinned development build."
}

$asset = $selected.Asset
$checksumAsset = $selected.ChecksumAsset
$release = $selected.Release
Write-Host "Selected stable FFmpeg $($selected.VersionText), release tag '$($release.tag_name)', commit $($selected.Commit)."
Write-Host "Archive: $($asset.name)"

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("ffgrabber-ffmpeg-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $archivePath = Join-Path $tempRoot $asset.name
    $checksumPath = Join-Path $tempRoot 'checksums.sha256'

    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $archivePath -Headers @{ 'User-Agent' = 'FFGrabber-build' }
    Invoke-WebRequest -Uri $checksumAsset.browser_download_url -OutFile $checksumPath -Headers @{ 'User-Agent' = 'FFGrabber-build' }

    $matchingLine = Get-Content $checksumPath | Where-Object { $_.Contains($asset.name) } | Select-Object -First 1
    if (-not $matchingLine) {
        throw "No SHA-256 checksum found for $($asset.name) in release $($release.tag_name)."
    }
    $expectedHash = $matchingLine.Trim().Split(' ', [StringSplitOptions]::RemoveEmptyEntries)[0].ToLowerInvariant()
    $actualHash = (Get-FileHash -Path $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expectedHash -notmatch '^[0-9a-f]{64}$' -or $actualHash -ne $expectedHash) {
        throw "FFmpeg archive SHA-256 verification failed. Expected $expectedHash, got $actualHash."
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
        source = 'BtbN/FFmpeg-Builds'
        stable_version = $selected.VersionText
        release_tag = $release.tag_name
        release_name = $release.name
        release_published_at = $release.published_at
        commit = $selected.Commit
        commit_distance_from_stable_branch = $selected.Distance
        architecture = $Architecture
        variant = 'static GPL build'
        archive_name = $asset.name
        archive_url = $asset.browser_download_url
        archive_sha256 = $actualHash
        checksum_verified = $true
        fetched_utc = [DateTime]::UtcNow.ToString('o')
        important_limit = 'Build feature availability depends on the upstream configure flags and bundled third-party libraries. Runtime FFmpeg capability help is authoritative; no binary includes literally every possible codec, hardware backend, or patent-restricted component.'
    }
    $metadata | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $OutputDirectory 'ffmpeg-build.json') -Encoding utf8
    Write-Host "Fetched and SHA-256-verified stable FFmpeg $($selected.VersionText) for $Architecture."
}
finally {
    Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
