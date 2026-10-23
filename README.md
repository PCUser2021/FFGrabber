# FFGrabber

**FFGrabber** is a Windows-first graphical frontend for the real FFmpeg command-line tools. It launches `ffmpeg.exe`, `ffprobe.exe`, and `ffplay.exe` directly; it does not embed a separate media engine.

> **Status: early preview (0.1.0).** The initial source implements the Avalonia GUI, direct process invocation, basic transcode/remux/extract modes, ffprobe metadata inspection, ffplay launch, a sequential queue, process logging, cancellation, tool discovery, and x64/ARM64 installer automation. It is not yet a feature-complete replacement for a professional mastering suite.

## Stack

- C# / .NET 10
- Avalonia UI 12.1.4
- FluentAvaloniaUI 3.1.0
- Inno Setup 7 for Windows installers
- FFmpeg builds from [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds/releases)

## Features

- Convert using H.264 (`libx264`), HEVC (`libx265`) or AV1 (`libsvtav1`) where the chosen FFmpeg build supports them.
- Configure encoder preset, CRF or target bitrate, output resolution, frame rate, pixel format, audio codec and bitrate.
- Remux with stream copy, extract audio, or copy video without audio.
- Preview generated FFmpeg arguments before running.
- Queue jobs sequentially, stream process output into the log, and cancel the active process.
- Inspect container and stream metadata using ffprobe JSON output.
- Launch ffplay to preview a source file.
- Find tools on PATH, in bundled folders, or at a user-selected location.
- Build self-contained Windows x64 and ARM64 packages.

## Build on Windows

Prerequisites: .NET 10 SDK, PowerShell 7 recommended, and network access to fetch FFmpeg. Inno Setup 7 is required only when building an installer locally.

```powershell
./scripts/Build.ps1 -Architecture x64
./scripts/Build.ps1 -Architecture arm64
./scripts/Build.ps1 -Architecture x64 -Version 0.1.0 -Installer
```

Add `-SkipFfmpegDownload` to publish the application without bundling FFmpeg. A complete installer should bundle the corresponding architecture's FFmpeg tools.

## GitHub Actions

The workflow in `.github/workflows/build.yml` builds and tests Windows x64 and ARM64 packages on GitHub-hosted Windows runners. It checks PE machine headers, smoke-tests the x64 FFmpeg tools, compiles Inno Setup installers, and uploads installers with SHA-256 files as workflow artifacts. Pushing a tag such as `v0.1.0` is configured to publish a GitHub Release with the installers.

**Important:** The project must pass its first Windows CI build before installers should be described as verified or production-ready.

## FFmpeg licensing

The build helper selects a static GPL FFmpeg build to include common codecs such as x264/x265. FFmpeg and its bundled third-party libraries have separate license obligations. Check the bundled license notices and applicable source-distribution obligations before redistributing installers. This is not legal advice.

References: [FFmpeg legal](https://ffmpeg.org/legal.html) · [BtbN builds](https://github.com/BtbN/FFmpeg-Builds) · [Avalonia](https://avaloniaui.net/) · [FluentAvalonia](https://github.com/amwx/FluentAvalonia) · [Inno Setup](https://jrsoftware.org/isinfo.php)

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md).

## License

FFGrabber application code is offered under the MIT License. Bundled FFmpeg and third-party components are not covered by that application license; see third-party notices and upstream licenses.