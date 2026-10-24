# Third-party software notices

## FFmpeg

FFGrabber does not include, download or redistribute FFmpeg. Users import their own `ffmpeg.exe`, `ffprobe.exe` and `ffplay.exe`, which FFGrabber starts as separate processes. The imported build keeps its own license (LGPL or GPL, depending on configuration); review it before redistributing it. See https://ffmpeg.org/legal.html and https://ffmpeg.org/download.html.

## Avalonia UI

Avalonia UI packages are separately licensed; see https://github.com/AvaloniaUI/Avalonia.

## FluentAvaloniaUI

FluentAvaloniaUI is separately licensed; see https://github.com/amwx/FluentAvalonia.

## .NET

The application is built against .NET 10 LTS and published self-contained for Windows x64 and Windows ARM64. See https://dotnet.microsoft.com/en-us/platform/support/policy.

## Inno Setup

Inno Setup 7.1.0 is the build-time installer compiler and is not bundled with the application. See https://jrsoftware.org/isinfo.php.
