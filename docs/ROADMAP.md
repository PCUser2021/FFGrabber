# FFGrabber roadmap

## Preview scope

- Direct FFmpeg process execution with structured argument passing
- Convert, remux, audio extraction and video-only stream copy
- ffprobe metadata inspection and ffplay preview
- Sequential queue, cancellation and progress reporting
- Windows x64 / ARM64 installer packaging

## Encoding workflow

- Named and reusable presets
- Stream mapping, subtitles, chapters, metadata and disposition controls
- Encoder-aware settings for x264, x265, SVT-AV1, NVENC, Quick Sync and AMF
- Two-pass encoding, advanced rate control and size estimation
- Audio sample rate, channel layout, loudness normalization and per-stream selection
- Color primaries, transfer characteristics, HDR metadata and pixel-format validation

## Product quality

- Automated unit and integration tests with fixture media
- Accessibility, keyboard navigation and localization
- Update checker, signed installers, software bill of materials and reproducible builds
- Thorough FFmpeg license audit for every shipped binary build
