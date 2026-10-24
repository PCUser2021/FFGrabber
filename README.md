# FFGrabber

**FFGrabber** is a Windows graphical frontend for FFmpeg. It is a pure GUI: it ships **no FFmpeg binaries at all**. You import your own `ffmpeg.exe`, `ffprobe.exe` and `ffplay.exe`, and FFGrabber runs those executables locally as separate processes.

> **Status: active preview (0.5.4).** Native Avalonia GUI with persistent System/Light/Dark themes, opaque surfaces to avoid backdrop-transparency artifacts, direct FFmpeg process invocation, transcode/remux/audio-extract modes, ffprobe metadata inspection, ffplay launch, a sequential queue, full logging, cancellation, and x64/ARM64 installers.

## Downloads

**Official download page:** [GitHub Releases](https://github.com/PCUser2021/FFGrabber/releases/latest)

- **Windows x64 (Intel/AMD):** `FFGrabber-Setup-x64-v0.5.4.exe`
- **Windows ARM64:** `FFGrabber-Setup-arm64-v0.5.4.exe`
- **Checksums:** `SHA256SUMS.txt` and a `.sha256` file next to each installer
- **Release notes:** [FFGrabber v0.5.4](docs/RELEASE-NOTES-0.5.3.md)

## First run: import FFmpeg

FFmpeg is not included, not downloaded, and not read from `PATH` or the install folder.

1. Get an FFmpeg build for your platform from [ffmpeg.org/download.html](https://ffmpeg.org/download.html) and extract it.
2. Start FFGrabber and click **Import FFmpeg…**.
3. Select the folder containing `ffmpeg.exe`, `ffprobe.exe` and `ffplay.exe` (an extracted package root with a `bin` subfolder also works).
4. FFGrabber runs `-version` on all three. Only if every check passes is the folder saved to `%LOCALAPPDATA%\FFGrabber\ffmpeg-import.json`. A failed import leaves the previous selection unchanged.

Use a build that matches your CPU architecture. Until a valid import exists, encoding, probing and playback are disabled and the window says so.

## Features

- Standard controls for transcode, stream-copy/remux, audio extraction and video-only output.
- **35 encoding-guide profiles** distilled from all 14 supplied Dropbox `.txt` guides (H.264/H.265, AV1, VP8/VP9, FFV1, ProRes/MJPEG/VFX, MPEG-4, Theora/Vorbis, AAC, MP3, Opus, FLAC, YouTube and streaming recipes).
- Dedicated guide-specific controls for minimum/maximum video bitrate, VBV buffer, GOP/keyframe interval, encoder tune/profile, video Qscale/QP, audio Qscale/VBR/profile/cutoff, sample rate/channels, and HDR color primaries/transfer/colorspace.
- **Automated two-pass encoding** for libx264, native MPEG-4, libvpx/VP9, libaom-av1 and FFV1 profiles. Pass 1 uses the null muxer, pass 2 writes the requested output; managed pass logs are removed after the job. Encoder-private workflows such as x265 pass parameters remain editable in advanced options.
- Searchable **Complete Settings Editor**: 124 distinct guide-indexed option entries with descriptions and source-file attribution, plus live options from the imported executable's `-h full` catalogue. Options are routed to their recommended input, additional-input, video, audio, filter, muxer, mapping or output field; build-time configure switches and help commands are marked reference-only.
- Scoped **output temporary-file cleanup**: removes only output-related `.tmp` files created by the current job after FFmpeg exits. Older matching files can be reviewed in a confirmation dialog and manually removed; unrelated temp files are not swept recursively.
- Video/audio encoder, input demuxer and output muxer lists discovered from the imported `ffmpeg.exe`.
- Advanced raw option fields (global, input, mapping, video/audio encoder, filters, muxer, output) passed as argument vectors, never through a shell.
- Capability browser for `-h full`, per-encoder/decoder/demuxer/muxer/filter/bitstream-filter/protocol help, encoders, decoders, formats, filters, protocols, devices, hardware accelerators, pixel/sample formats, channel layouts, and runtime device source/sink discovery from the imported build.
- Persistent **Options → Application theme** selector for System, Light, and Dark. The UI uses opaque surfaces and theme-aware colors rather than desktop-dependent translucent panels; the preference is saved under `%LOCALAPPDATA%\\FFGrabber\\ui-theme.txt`.
- Direct links to the official FFmpeg command-line and full component references, ffprobe/ffplay documentation, and the FFmpeg Trac wiki.
- Local files, URLs, pipes, protocol targets and device expressions as input/output.
- Command preview, sequential queue, live log, cancellation.
- ffprobe JSON metadata inspection and ffplay preview.
- Full support ZIP via **Options → Save Logs…**.

## Stack

- C# / .NET 10 (SDK 10.0.402), Avalonia UI 12.1.4, FluentAvaloniaUI 3.1.0
- Inno Setup 7.1.0 for installers

## Build on Windows

Prerequisites: .NET 10 SDK and PowerShell 7. Inno Setup 7.1.0+ only for local installers.

```powershell
./scripts/Build.ps1 -Architecture x64
./scripts/Build.ps1 -Architecture arm64
./scripts/Build.ps1 -Architecture x64 -Version 0.5.4 -Installer
```

## GitHub Actions

`.github/workflows/build.yml` builds the x64 and ARM64 apps, checks the PE machine type, **fails if any FFmpeg executable or `tools/ffmpeg` folder is present in the payload**, compiles the installers with a signature- and SHA-256-verified Inno Setup, and writes installer checksums. Pushes to `main` and `v*` tags publish a GitHub Release for the configured version; an existing release is never overwritten.

## FFmpeg licensing

FFGrabber does not distribute FFmpeg, so it carries no FFmpeg license obligations itself. The FFmpeg build you import is governed by its own license (LGPL or GPL, depending on how it was built). See [FFmpeg legal](https://ffmpeg.org/legal.html). This is not legal advice.

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md).

## License

FFGrabber application code is offered under the MIT License. Third-party components are covered by their own licenses; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
