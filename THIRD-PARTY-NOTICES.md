# Third-party software notices

## FFmpeg

FFGrabber bundles static GPL-enabled executables from [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds/releases). The fetch script selects the newest stable versioned release-branch build available for each architecture (currently the 9.0.2 branch), rejects development/master-only builds, and verifies the downloaded archive against the upstream SHA-256 manifest before extraction.

FFmpeg and each bundled third-party library retain their own licenses. GPL builds can add encoders such as libx264/libx265 where included, but **no single FFmpeg binary can contain every possible codec, hardware backend or patent-restricted component**. FFGrabber's capabilities browser displays the codecs, muxers, demuxers, filters, protocols and options actually present in the bundled build. The exact upstream license files discovered inside each archive are carried into the packaged tool directory. Review the selected build's license obligations before redistributing it.

Official references:
- FFmpeg releases: https://ffmpeg.org/download.html
- FFmpeg legal information: https://ffmpeg.org/legal.html
- BtbN Windows builds and configuration: https://github.com/BtbN/FFmpeg-Builds

## Avalonia UI

Avalonia UI packages are separately licensed; see https://github.com/AvaloniaUI/Avalonia.

## FluentAvaloniaUI

FluentAvaloniaUI is separately licensed; see https://github.com/amwx/FluentAvalonia.

## .NET

The application is built against .NET 10 LTS and published self-contained for Windows x64 and Windows ARM64. See https://dotnet.microsoft.com/en-us/platform/support/policy.

## Inno Setup

Inno Setup 7.1.0 is the build-time installer compiler and is not bundled with the application. See https://jrsoftware.org/isinfo.php.
