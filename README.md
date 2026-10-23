# FFGrabber

**FFGrabber** is a Windows-first graphical frontend for the real FFmpeg command-line tools. It launches `ffmpeg.exe`, `ffprobe.exe`, and `ffplay.exe` directly; it does not embed a separate media engine.

> **Status: active preview (0.3.0).** FFGrabber provides a native Avalonia GUI, direct FFmpeg process invocation, basic transcode/remux/audio-extract modes, ffprobe metadata inspection, ffplay launch, a sequential queue, process logging, cancellation, and x64/ARM64 installer builds. It is not yet a feature-complete replacement for a professional mastering suite.

## Downloads

**Official permanent download page:** [GitHub Releases](https://github.com/PCUser2021/FFGrabber/releases/latest)

- **Windows x64 (Intel/AMD):** [Download the installer](https://github.com/PCUser2021/FFGrabber/releases/download/v0.3.0/FFGrabber-Setup-x64-v0.3.0.exe) · [SHA-256 file](https://github.com/PCUser2021/FFGrabber/releases/download/v0.3.0/FFGrabber-Setup-x64-v0.3.0.exe.sha256)
- **Windows ARM64:** [Download the installer](https://github.com/PCUser2021/FFGrabber/releases/download/v0.3.0/FFGrabber-Setup-arm64-v0.3.0.exe) · [SHA-256 file](https://github.com/PCUser2021/FFGrabber/releases/download/v0.3.0/FFGrabber-Setup-arm64-v0.3.0.exe.sha256)
- **Combined checksum manifest:** [SHA256SUMS.txt](https://github.com/PCUser2021/FFGrabber/releases/download/v0.3.0/SHA256SUMS.txt)

The Windows CI workflow publishes this release only after both architecture builds, FFmpeg verification, PE architecture checks, and Inno Setup compilation pass. Release assets remain available from the Releases page rather than depending on short-lived Actions artifact retention.

## Stack

- C# / .NET 10.0.13 runtime and .NET SDK 10.0.402
- Avalonia UI 12.1.4 (latest stable checked 2026-10-23)
- FluentAvaloniaUI 3.1.0 (latest stable checked 2026-10-23)
- Inno Setup 7.1.0 for Windows installers
- FFmpeg 9.0.2 full GPL build from [GyanD/codexffmpeg](https://github.com/GyanD/codexffmpeg/releases) (x64), plus a pinned [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds/releases) ARM64 build

## Features

- Advanced raw FFmpeg options for global/input-demuxer, stream mapping, video encoder, audio encoder, filter graph, muxer/container, and output settings.
- FFmpeg capability browser for full help, encoders, decoders, formats, muxers, demuxers, filters, protocols, devices, hardware accelerators, pixel formats, sample formats, bitstream filters and channel layouts.
- Full support ZIP export through **Options → Save Logs…**, including the untruncated session log, prior local session logs, system/runtime details, current settings, active and pending job settings, FFmpeg build configuration, and tool capability reports.
- Runtime-discovered video/audio encoder lists and complete muxer/demuxer selectors populated from the FFmpeg binary actually installed.
- Pinned FFmpeg 9.0.2: GyanD full GPL build for x64 and an architecture-matched BtbN GPL build for ARM64; both archives are verified against fixed SHA-256 digests.

- Convert using H.264 (`libx264`), HEVC (`libx265`) or AV1 (`libsvtav1`) where the chosen FFmpeg build supports them.
- Configure encoder preset, CRF or target bitrate, output resolution, frame rate, pixel format, audio codec and bitrate.
- Remux with stream copy, extract audio, or copy video without audio.
- Accept local files, network URLs, pipes, FFmpeg protocol targets and device expressions in the input/output fields; protocol and capture-device flags can be supplied in advanced input/output options.
- Preview generated FFmpeg arguments before running.
- Queue jobs sequentially, stream process output into the log, and cancel the active process.
- Inspect container and stream metadata using ffprobe JSON output.
- Launch ffplay to preview a source file.
- Discover bundled tools or executables available on `PATH`.
- Build self-contained Windows x64 and ARM64 packages with pinned, checksum-verified FFmpeg 9.0.2 executables.

## Build on Windows

Prerequisites: .NET 10 SDK, PowerShell 7 recommended, and network access to fetch FFmpeg. Inno Setup 7 is required only when building an installer locally.

```powershell
./scripts/Build.ps1 -Architecture x64
./scripts/Build.ps1 -Architecture arm64
./scripts/Build.ps1 -Architecture x64 -Version 0.2.0 -Installer
```

Add `-SkipFfmpegDownload` to publish the application without bundling FFmpeg. A complete installer bundles pinned FFmpeg 9.0.2 tools matching the target architecture.

## GitHub Actions

The workflow in `.github/workflows/build.yml` builds Windows x64 and ARM64 on GitHub-hosted runners, verifies pinned FFmpeg 9.0.2 archive digests and provenance metadata, checks PE machine headers, smoke-tests x64 FFmpeg tools and confirms the FFmpeg version, compiles installers with the latest stable Inno Setup 7.x release, exports checksums, and publishes a permanent release only after both builds pass. A push of a new feature commit to `main` creates the version configured in the workflow; later releases are produced by changing the project version or pushing a version tag. Existing releases are not overwritten by routine pushes to `main`.

The previous `v0.1.0` and `v0.2.0` installers and source remain preserved in release history, and the pre-expansion source snapshot is kept on branch `backup/2026-10-23-pre-encoder-expansion`.

## FFmpeg licensing

The build helper selects a static GPL FFmpeg build to include common codecs such as x264/x265. FFmpeg and its bundled third-party libraries have separate license obligations. Check the bundled license notices and applicable source-distribution obligations before redistributing installers. This is not legal advice.

References: [FFmpeg legal](https://ffmpeg.org/legal.html) · [BtbN builds](https://github.com/BtbN/FFmpeg-Builds) · [Avalonia](https://avaloniaui.net/) · [FluentAvalonia](https://github.com/amwx/FluentAvalonia) · [Inno Setup](https://jrsoftware.org/isinfo.php)

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md).

## License

FFGrabber application code is offered under the MIT License. Bundled FFmpeg and third-party components are not covered by that application license; see third-party notices and upstream licenses.