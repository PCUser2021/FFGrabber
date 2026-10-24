# FFGrabber roadmap

## Implemented

- Import-only FFmpeg model (0.4.0): no FFmpeg is bundled or fetched; the app uses only the user-imported `ffmpeg.exe`, `ffprobe.exe` and `ffplay.exe`, validated with `-version` before being saved.
- Advanced raw option groups for global, demuxer/input, stream mapping, video encoder, audio encoder, filters, muxer/container and output options.
- Runtime FFmpeg capability browser and runtime-discovered encoder/demuxer/muxer selectors from the imported binary.
- Direct argument-vector process execution without invoking a shell.
- Untruncated session logging and **Options → Save Logs…** ZIP export.
- Opaque, theme-aware UI with persisted System/Light/Dark selection; all windows share the selected theme without relying on backdrop transparency.
- CI checks for PE architecture, absence of bundled FFmpeg, installer compilation and checksum generation.

## Next development targets

- Per-stream selection UI, subtitle/attachment handling, dispositions, channel layout and sample-rate controls.
- Named presets, two-pass encoding, target file-size planning, loudness normalization and advanced rate-control UI.
- Hardware-encoder discovery and guided NVENC, Quick Sync and AMF settings when the selected FFmpeg build and device support them.
- Color primaries, transfer characteristics, HDR metadata controls and validation.
- Automated integration tests with media fixtures and executable option probes on Windows x64 and ARM64 runners.
- Accessibility, keyboard navigation, localization, signed installers, software bill of materials and a release license audit.
