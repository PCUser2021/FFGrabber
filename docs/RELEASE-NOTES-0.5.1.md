# FFGrabber 0.5.1

## Release alignment

- Build the published installers from the current `main` source, including the separate advanced-help shortcut (`-h long`) and complete option catalogue (`-h full`).
- Keep the pure GUI/import-only architecture: FFmpeg, ffprobe and ffplay are never bundled or downloaded by FFGrabber.
- Ship x64 and ARM64 installers with SHA-256 sidecar files and a combined checksum manifest.
