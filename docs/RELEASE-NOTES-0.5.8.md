# FFGrabber v0.5.8

**Release focus:** Linux desktop support and packages for x64 and ARM64 while retaining the existing Windows x64 and ARM64 installers.

## Linux packages

Each Linux architecture has four checksummed downloads:

- Debian / Ubuntu family: FFGrabber-0.5.8-linux-x64.deb and FFGrabber-0.5.8-linux-arm64.deb
- Fedora / RPM family: FFGrabber-0.5.8-linux-x64.rpm and FFGrabber-0.5.8-linux-arm64.rpm
- Portable single-file launcher: FFGrabber-0.5.8-linux-x64.AppImage and FFGrabber-0.5.8-linux-arm64.AppImage
- Per-user installable archive: FFGrabber-0.5.8-linux-x64.tar.gz and FFGrabber-0.5.8-linux-arm64.tar.gz

The portable archive includes install.sh and uninstall.sh; these place the application in the current user's local data directory and do not require root privileges. Native packages register a desktop launcher and install a scalable SVG icon. The release also includes a combined SHA256SUMS.txt.

## Cross-platform FFmpeg import

- FFGrabber validates and launches platform-native executable names: ffmpeg.exe, ffprobe.exe, and ffplay.exe on Windows; extensionless ffmpeg, ffprobe, and ffplay files on Linux.
- Windows import flows and installers remain available.
- The capture-profile picker hides Windows-only DirectShow/gdigrab/DDAgrab/gfxcapture examples outside Windows and adds Linux X11/XWayland desktop and V4L2 webcam examples.
- FFmpeg remains user-imported. It is not bundled, fetched, or loaded from PATH.

## Verification

CI runs unit tests on Windows and Linux, builds Windows x64/ARM64 installers and Linux x64/ARM64 packages, checks that FFmpeg executables are absent from the app payload, and publishes SHA-256 checksums.

Linux desktop support targets glibc-based distributions. Avalonia uses X11 by default and can run under Wayland via XWayland; native capture, device access, installed desktop libraries, hardware drivers, and the imported FFmpeg build remain platform-dependent.
