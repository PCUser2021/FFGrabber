# FFGrabber roadmap

## Implemented in the 0.2.0 upgrade

- Advanced raw option groups for global, demuxer/input, stream mapping, video encoder, audio encoder, filters/filter graphs, muxer/container and output options.
- Runtime FFmpeg capability browser for generic help and compiled encoders, decoders, codecs, formats, muxers, demuxers, filters, bitstream filters, protocols, devices, hardware acceleration, pixel formats, sample formats, and channel layouts.
- Direct argument-vector process execution without invoking a shell.
- Untruncated append-only session logging and **Options → Save Logs…** ZIP export.
- Support bundles containing current and previous session logs, system/runtime details, current settings, queue snapshot, FFmpeg build configuration, and capability reports from ffmpeg/ffprobe/ffplay.
- Architecture-matched Windows x64 / ARM64 installers bundling the newest available stable FFmpeg release-branch static GPL builds with SHA-256 verification.
- CI checks for PE architecture, FFmpeg archive integrity, x64 tool smoke tests, installer compilation and checksum generation.

## Next development targets

- Per-stream selection UI, subtitle/attachment handling, dispositions, channel layout and sample-rate controls.
- Named presets, two-pass encoding, target file-size planning, loudness normalization and advanced rate-control UI.
- Hardware-encoder discovery and guided NVENC, Quick Sync and AMF settings when the selected FFmpeg build and device support them.
- Color primaries, transfer characteristics, HDR metadata controls and validation.
- Automated integration tests with media fixtures and executable option probes on Windows x64 and ARM64 runners.
- Accessibility, keyboard navigation, localization, signed installers, software bill of materials and a release license audit.
