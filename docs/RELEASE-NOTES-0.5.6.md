# FFGrabber v0.5.6

## Highlights

- Added a custom glossy blue/cyan icon resource and applied it to the application executable. The same native icon now drives the main-window title bar, installer Setup executable, desktop shortcut, Start Menu shortcut and uninstall display entry.
- Added a persistent v0.5.6 badge in the main header and an About / version dialog with assembly version, framework, OS and process architecture.
- Added capture-guide profiles for Windows desktop capture with gdigrab, region capture, DirectShow raw YUYV/MJPEG webcam modes, camera plus microphone, low-latency capture, DDAgrab/D3D11 and Windows Graphics Capture gfxcapture.
- Added dedicated input-capture and hardware-acceleration controls for frame rate, size, pixel format, device stream codec, buffers, GDI region offsets, camera index, capture audio channels/sample rate, timestamp preservation, benchmark timing, noautoscale, hwaccel, hwaccel_device, hwaccel_output_format, init_hw_device, filter_hw_device and extra_hw_frames.
- Implemented source-filter capture graph support: DDAgrab/gfxcapture can run without a fake primary input; the source-filter graph is supplied through filter_complex, invalid primary-input metadata mapping is skipped, and automated two-pass is rejected for live source-filter capture.
- Added one-click DirectShow device discovery on Windows, with FFmpeg diagnostics recorded in the session log.
- Expanded the searchable option index using the six currently accessible capture/hardware reference notes.
- Preserved those six Dropbox text documents verbatim in docs/source-guides/dropbox/. The previous 14 encoding-guide profiles/index remain in the app, but their original Dropbox file IDs are unavailable in the current reference set; their raw text files are not falsely represented as newly archived.

## Compatibility and safety

Capture profiles are editable starting points, not guarantees. Camera names, modes, drivers and FFmpeg build capabilities vary. The -delay option in the historical lightning guide is not a normal FFmpeg option and is treated as reference-only. Linux/macOS examples and external tools are archived for reference, not represented as Windows features.

## Backup

The pre-change repository state is preserved on backup/pre-v0.5.6-2026-10-23. The v0.5.6 release source is preserved by GitHub's source archives and a dedicated backup/ffgrabber-v0.5.6-complete snapshot branch. See docs/backup/RESTORE.md.
