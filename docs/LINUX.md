# FFGrabber on Linux

Linux builds are being added for x86-64 and ARM64.

## Package formats

- Debian/Ubuntu and compatible distributions: `.deb`
- Fedora/RHEL-compatible and other RPM-based distributions: `.rpm`
- Portable desktop package: x86-64 `.AppImage`
- Portable archive: `.tar.gz`

Packages are published on the [GitHub Releases page](https://github.com/PCUser2021/FFGrabber/releases) when a version tag is built. CI artifacts are also available from individual workflow runs.

## FFmpeg is not included

FFGrabber does not bundle FFmpeg. Install FFmpeg from your distribution's trusted repositories or another trusted source, then configure the application to use the installed `ffmpeg`, `ffprobe`, and `ffplay` executables.

## Compatibility

Linux desktop environments, display servers, native dialogs, and multimedia device access differ by distribution. CI compilation and package checks do not establish compatibility with every distribution. Runtime testing on real Linux desktops is still required before describing the Linux port as stable.
