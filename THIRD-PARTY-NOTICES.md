# Third-party software notices

## FFmpeg

FFGrabber 0.3.0 pins FFmpeg 9.0.2. The Windows x64 package uses Gyan's full GPL archive at [GyanD/codexffmpeg release 9.0.2](https://github.com/GyanD/codexffmpeg/releases/tag/9.0.2). Windows ARM64 uses the architecture-matched GPL build from [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds/releases) at release tag `autobuild-2026-10-23-13-04` (FFmpeg 9.0.2 release branch). The exact archive URLs and SHA-256 digests are pinned in `scripts/Fetch-FFmpeg.ps1`; the build script rejects a mismatched download before extraction.

FFmpeg and each bundled third-party library retain their own licenses. GPL builds can add encoders such as libx264/libx265 where included, but **no single FFmpeg binary can contain every possible codec, hardware backend or patent-restricted component**. FFGrabber's capabilities browser displays the codecs, muxers, demuxers, filters, protocols and options actually present in the bundled build. The exact upstream license files discovered inside each archive are carried into the packaged tool directory. Review the selected build's license obligations before redistributing it.

Official references:
- FFmpeg releases: https://ffmpeg.org/download.html
- FFmpeg legal information: https://ffmpeg.org/legal.html
- GyanD Windows full builds: https://github.com/GyanD/codexffmpeg/releases
- BtbN Windows ARM64 build source: https://github.com/BtbN/FFmpeg-Builds

## Avalonia UI

Avalonia UI packages are separately licensed; see https://github.com/AvaloniaUI/Avalonia.

## FluentAvaloniaUI

FluentAvaloniaUI is separately licensed; see https://github.com/amwx/FluentAvalonia.

## .NET

The application is built against .NET 10 LTS and published self-contained for Windows x64 and Windows ARM64. See https://dotnet.microsoft.com/en-us/platform/support/policy.

## Inno Setup

Inno Setup 7.1.0 is the build-time installer compiler and is not bundled with the application. See https://jrsoftware.org/isinfo.php.
