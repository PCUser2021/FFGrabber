# FFGrabber

**FFGrabber** is a Windows graphical frontend for FFmpeg. It is a pure GUI: it ships **no FFmpeg binaries at all**. You import your own `ffmpeg.exe`, `ffprobe.exe` and `ffplay.exe`, and FFGrabber runs those executables locally as separate processes.

> **Status: active preview (0.5.8).** Native Avalonia GUI with persistent System/Light/Dark themes, opaque surfaces to avoid backdrop-transparency artifacts, direct FFmpeg process invocation, transcode/remux/audio-extract modes, ffprobe metadata inspection, ffplay launch, a sequential queue, full logging, cancellation, and x64/ARM64 installers.

## Downloads

**Official download page:** [GitHub Releases](https://github.com/PCUser2021/FFGrabber/releases/latest)

- **Windows x64 (Intel/AMD):** `FFGrabber-Setup-x64-v0.5.8.exe`
- **Windows ARM64:** `FFGrabber-Setup-arm64-v0.5.8.exe`
- **Checksums:** `SHA256SUMS.txt` and a `.sha256` file next to each installer
- **Release notes:** [FFGrabber v0.5.8](docs/RELEASE-NOTES-0.5.8.md)

## MediaInfo integration and automatic update checks

FFGrabber can inspect media through the user-installed [MediaInfo CLI](https://github.com/MediaArea/MediaInfo). Click **MediaInfo…** and select the official `MediaInfo.exe` CLI when prompted. The report uses MediaInfo JSON output, with copy-to-clipboard support. MediaInfo is not bundled.

On startup, FFGrabber checks official GitHub Releases metadata for FFGrabber and an imported MediaInfo CLI at most once every 24 hours. It opens the official release page when a newer version is found; it does not silently download or replace executables.

## File Explorer context menu

The Windows installer adds current-user context-menu verbs for video/audio files: convert, remux/stream copy, extract video or audio, play, and inspect with MediaInfo. These invoke the installed FFGrabber application; FFmpeg/FFprobe/FFplay still need to be imported first.

## First run: import FFmpeg

FFmpeg is not included, not downloaded, and not read from `PATH` or the install folder.

1. Get an FFmpeg build for your platform from [ffmpeg.org/download.html](https://ffmpeg.org/download.html) and extract it.
2. Start FFGrabber and click **Import FFmpeg…**.
3. Select the folder containing `ffmpeg.exe`, `ffprobe.exe` and `ffplay.exe` (an extracted package root with a `bin` subfolder also works).
4. FFGrabber runs `-version` on all three. Only if every check passes is the folder saved to `%LOCALAPPDATA%\FFGrabber\ffmpeg-import.json`. A failed import leaves the previous selection unchanged.

Use a build that matches your CPU architecture. Until a valid import exists, encoding, probing and playback are disabled and the window says so.

## Features

- Standard controls for transcode, stream-copy/remux, audio extraction and video-only output.
- **52 encoding/audio/streaming profiles**: the original 35 profiles, 14 supplied/reconstructed profile payloads, and three full-guide-based streaming workflows (file-to-RTMP, live-input-to-RTMP, and RTMP plus local recording through the tee muxer). The 14 short payloads remain identified as reconstructions, not original historical full encoder guides.
- Dedicated guide-specific controls for minimum/maximum video bitrate, VBV buffer, GOP/keyframe interval, encoder tune/profile, video Qscale/QP, audio Qscale/VBR/profile/cutoff, sample rate/channels, and HDR color primaries/transfer/colorspace.
- Searchable **Source Guide Library** that embeds and displays the complete preserved text of every tracked `.txt` guide, searches across full document bodies, and applies associated encoder/audio/capture profiles back to the main workspace.
- **Automated two-pass encoding** for libx264, native MPEG-4, libvpx/VP9, libaom-av1 and FFV1 profiles. Pass 1 uses the null muxer, pass 2 writes the requested output; managed pass logs are removed after the job. Encoder-private workflows such as x265 pass parameters remain editable in advanced options.
- Searchable **Complete Settings Editor**: 124 distinct guide-indexed option entries with descriptions and source-file attribution, plus live options from the imported executable's `-h full` catalogue. Options are routed to their recommended input, additional-input, video, audio, filter, muxer, mapping or output field; build-time configure switches and help commands are marked reference-only.
- Scoped **output temporary-file cleanup**: removes only output-related `.tmp` files created by the current job after FFmpeg exits. Older matching files can be reviewed in a confirmation dialog and manually removed; unrelated temp files are not swept recursively.
- Video/audio encoder, input demuxer and output muxer lists discovered from the imported `ffmpeg.exe`.
- Advanced raw option fields (global, input, mapping, video/audio encoder, filters, muxer, output) passed as argument vectors, never through a shell.
- Capability browser for `-h full`, per-encoder/decoder/demuxer/muxer/filter/bitstream-filter/protocol help, encoders, decoders, formats, filters, protocols, devices, hardware accelerators, pixel/sample formats, channel layouts, and runtime device source/sink discovery from the imported build.
- Persistent **System / Light / Dark** appearance selector. System follows the Windows color theme dynamically; Light and Dark can be pinned explicitly. The UI uses opaque, theme-aware surfaces rather than desktop-dependent translucent panels.
- Windows Fluent-inspired workspace with persistent System / Light / Dark appearance, sidebar navigation, consistent rounded controls, a unified Windows 11 logo mark and a system-aware title bar.
- Direct links to the official FFmpeg command-line and full component references, ffprobe/ffplay documentation, and the FFmpeg Trac wiki.
- Local files, URLs, pipes, protocol targets and device expressions as input/output.
- Command preview, sequential queue, live log, cancellation.
- ffprobe JSON metadata inspection and ffplay preview.
- Full support ZIP via **Options → Save Logs…**.
- Live conversion progress using FFmpeg's `-progress` protocol, plus per-process CPU utilization, total system CPU load, working-set/private memory, thread count, and system RAM metrics.
- Adjustable software encoder/filter thread budget (Reduced / Balanced / Maximum) to lower peak resource use.

## Stack

- C# / .NET 10 (SDK 10.0.402), Avalonia UI 12.1.4, FluentAvaloniaUI 3.1.0
- Inno Setup 7.1.0 for installers

## Build on Windows

Prerequisites: .NET 10 SDK and PowerShell 7. Inno Setup 7.1.0+ only for local installers.

```powershell
./scripts/Build.ps1 -Architecture x64
./scripts/Build.ps1 -Architecture arm64
./scripts/Build.ps1 -Architecture x64 -Version 0.5.7 -Installer
```

## GitHub Actions

`.github/workflows/build.yml` builds and tests on pull requests and pushes to `main`, and on `v*` tags; it checks the PE machine type, **fails if any FFmpeg executable or `tools/ffmpeg` folder is present in the payload**, compiles installers with signature- and SHA-256-verified Inno Setup, and writes installer checksums. A `v*` tag also publishes a permanent GitHub Release for the configured version; an existing release is never overwritten. `.github/workflows/auto-backup.yml` creates a verified Git bundle, source ZIP, and SHA-256 manifest on every branch push, every pull request to `main`, and once daily, with manual dispatch supported.

## Automatic backups

The automatic backup workflow uploads the Git bundle, source archive, and checksum manifest as a GitHub Actions artifact retained for **90 days**. Download the artifact from the workflow run to keep an offline copy beyond that retention window. A pre-change safety branch was also created: `backup/2026-10-24-pre-auto-backup`. GitHub Actions artifacts are rolling backups, not a substitute for an independently stored long-term copy.

## FFmpeg licensing

FFGrabber does not distribute FFmpeg, so it carries no FFmpeg license obligations itself. The FFmpeg build you import is governed by its own license (LGPL or GPL, depending on how it was built). See [FFmpeg legal](https://ffmpeg.org/legal.html). This is not legal advice.

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md).

## License

FFGrabber application code is offered under the MIT License. Third-party components are covered by their own licenses; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Capture and hardware-acceleration workflows

Version 0.5.6 adds dedicated DirectShow, Windows desktop-capture and hardware-acceleration controls; capture starter profiles; an optional source-filter graph for DDAgrab/gfxcapture; and visible version/About information. The searchable settings catalogue now covers hardware frame contexts, capture buffering and device modes. Availability depends on the imported FFmpeg build, hardware, drivers and target platform.

All 79 original `.txt` files currently present in the connected Dropbox root are preserved under [docs/source-guides/dropbox](docs/source-guides/README.md), retaining their original filenames and source wording. The application embeds the files and the Source Guide Library displays each source body without inserting editorial text. The separate reconstructed profile payloads remain identified as summaries rather than being treated as replacements for historical source documents. Historical and platform-specific examples are reference material; runtime help from the imported FFmpeg remains authoritative.

The new glossy blue/cyan FFGrabber icon is used by the application executable, title bar, desktop and Start Menu shortcuts, Setup package and uninstall display entry. The main window shows the product version and includes an About/version dialog.

