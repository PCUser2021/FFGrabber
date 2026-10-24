# FFGrabber Linux port — development notes

This branch adds Linux x86-64 and ARM64 self-contained publish jobs and packages in Debian, RPM, and tar.gz formats. An x86-64 AppImage is also assembled. FFmpeg binaries remain excluded.

These are packaging targets, not a claim of universal compatibility with every distribution or desktop session. Avalonia runtime behavior and platform-specific FFmpeg import, device, and file-dialog workflows must be validated on Linux desktops before the port is declared stable.
