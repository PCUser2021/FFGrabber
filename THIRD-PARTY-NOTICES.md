# Third-party software notices

## FFmpeg

FFGrabber GitHub Actions source-builds FFmpeg 9.0.2 for x64 and ARM64. GyanD/codexffmpeg tag `9.0.2` is a release mirror/support repository containing metadata, not the FFmpeg C source. Its release references the upstream FFmpeg tag `n9.0.2`, resolved to commit `946fcce07b6dcd0331c8cc609192aeff5e1924f8`. The workflow checks this tag/commit, then compiles that upstream source using a pinned revision of [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds) and its static GPL dependency profile. A JSON provenance file and SHA-256 manifest accompany the resulting binaries. The standalone local PowerShell build helper still supports downloading fixed-hash upstream prebuilt packages.

FFmpeg and each bundled third-party library retain their own licenses. GPL builds can add encoders such as libx264/libx265 where included, but **no single FFmpeg binary can contain every possible codec, hardware backend or patent-restricted component**. FFGrabber's capabilities browser displays the codecs, muxers, demuxers, filters, protocols and options actually present in the bundled build. The exact upstream license files discovered inside each archive are carried into the packaged tool directory. Review the selected build's license obligations before redistributing it.

Official references:
- GyanD mirror archive: https://github.com/GyanD/codexffmpeg/archive/refs/tags/9.0.2.zip
- Verified FFmpeg source commit: https://github.com/FFmpeg/FFmpeg/commit/946fcce07b6dcd0331c8cc609192aeff5e1924f8
- FFmpeg releases: https://ffmpeg.org/download.html
- FFmpeg legal information: https://ffmpeg.org/legal.html
- GyanD Windows prebuilt releases: https://github.com/GyanD/codexffmpeg/releases
- BtbN Windows source-build tooling/dependency profile: https://github.com/BtbN/FFmpeg-Builds

## Avalonia UI

Avalonia UI packages are separately licensed; see https://github.com/AvaloniaUI/Avalonia.

## FluentAvaloniaUI

FluentAvaloniaUI is separately licensed; see https://github.com/amwx/FluentAvalonia.

## .NET

The application is built against .NET 10 LTS and published self-contained for Windows x64 and Windows ARM64. See https://dotnet.microsoft.com/en-us/platform/support/policy.

## Inno Setup

Inno Setup 7.1.0 is the build-time installer compiler and is not bundled with the application. See https://jrsoftware.org/isinfo.php.
