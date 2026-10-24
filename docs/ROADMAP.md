# FFGrabber roadmap

## Implemented

- Capture and hardware acceleration controls (0.5.6): Windows gdigrab/DirectShow starter profiles, DDAgrab/gfxcapture source-filter workflows, DirectShow device discovery, capture size/rate/pixel format/codec, realtime/audio buffers, GDI region controls, timestamp preservation, camera index, ALSA-reference audio settings, hardware decode API/output format/device initialization, filter hardware device and extra frames.
- Custom glossy blue/cyan product icon and unified icon tiles applied to the app UI; the native EXE icon drives the title bar, Setup icon, desktop/Start Menu shortcuts and uninstall display icon. Main window shows version and an About dialog.
- The six currently available capture/hardware Dropbox .txt sources are preserved verbatim in docs/source-guides/dropbox. The previous encoder guide profiles/index remain integrated; the original 14 text-file objects are not currently addressable through the Dropbox connection, so raw copies are not claimed in this snapshot.

- Import-only FFmpeg model (0.4.0): no FFmpeg is bundled or fetched; the app uses only the user-imported `ffmpeg.exe`, `ffprobe.exe` and `ffplay.exe`, validated with `-version` before being saved.
- Advanced raw option groups for global, demuxer/input, additional inputs, stream mapping, video encoder, audio encoder, filters, muxer/container and output options, with a searchable editor that exposes live `-h full` plus 124 indexed switches drawn from 14 supplied encoding-guide text files.
- Runtime FFmpeg capability browser and runtime-discovered encoder/demuxer/muxer selectors from the imported binary.
- Direct argument-vector process execution without invoking a shell.
- Untruncated session logging and **Options → Save Logs…** ZIP export.
- Opaque, theme-aware UI with persisted System/Light/Dark selection; all windows share the selected theme without relying on backdrop transparency.
- Guide-derived profiles for the documented H.264/H.265, AV1, VP8/VP9, FFV1, ProRes/MJPEG/VFX, MPEG-4/Xvid, Theora/Vorbis, AAC, MP3, Opus, FLAC, YouTube, and streaming workflows.
- Dedicated controls for VBV limits/buffer, GOP, tune/profile, quantizers, audio VBR/profile/cutoff/sample rate/channels, and HDR color tags.
- Automated two-pass orchestration for libx264, native MPEG-4, libvpx/VP9, libaom-av1, and FFV1, including managed pass-log cleanup.
- Scoped automatic cleanup for output-related temporary .tmp files and a confirmation-based manual cleanup action.
- CI checks for PE architecture, absence of bundled FFmpeg, installer compilation and checksum generation.

## Next development targets

- Per-stream selection UI, subtitle/attachment handling, dispositions, and custom channel-layout controls.
- Target file-size planning, loudness normalization, and a guided two-pass private-parameter workflow for x265 and hardware encoders.
- Hardware-encoder discovery and guided NVENC, Quick Sync and AMF settings when the selected FFmpeg build and device support them.
- HDR mastering metadata controls and color-tag validation for source/target compatibility.
- Automated integration tests with media fixtures and executable option probes on Windows x64 and ARM64 runners.
- Accessibility, keyboard navigation, localization, signed installers, software bill of materials and a release license audit.
