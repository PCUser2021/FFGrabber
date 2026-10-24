# FFGrabber 0.5.4

## Encoding settings from the 14 supplied .txt guides

Read and indexed the 14 guide text files from Dropbox:

- `Encode_VFX.txt`
- `Encode_AAC.txt`
- `Encode_MP3.txt`
- `Encode_VP8.txt`
- `Encode_VP9.txt`
- `Encode_AV1.txt`
- `Encode_H.265.txt`
- `Encode_H.264.txt`
- `Encode_FFV1.txt`
- `Encode_MPEG-4.txt`
- `Encode_YouTube.txt`
- `TheoraVorbisEncodingGuide.txt`
- `EncodingForStreamingSites.txt`
- `Encode_HighQualityAudio.txt`

The GUI now has 35 starter profiles distilled from these guides, including H.264/H.265, AV1, VP8/VP9, FFV1, ProRes and MJPEG VFX intermediates, MPEG-4/Xvid, Theora/Vorbis, AAC, MP3, Opus, FLAC, YouTube and streaming output.

The searchable **Complete Settings Editor** includes 124 distinct switches/aliases referenced by the guide files. Each entry includes a description and its source guide(s). Build-time configure flags, standalone-LAME switches, and tool-help commands are identified as reference-only so they cannot accidentally be appended as normal FFmpeg encoding options. The editor also reads the imported executable's live `-h full` catalogue and component help, and maps selected settings to their recommended argument stage.

## New dedicated encoding controls

- Minimum and maximum video bitrate, VBV buffer size and GOP/keyframe interval.
- Video tune/profile, Qscale and QP.
- Audio Qscale, FDK AAC VBR mode, audio profile/cutoff, sample rate and channel count.
- Color primaries, transfer characteristics and colorspace tags.
- Additional input argument editor for multi-input, image-sequence and filter-composition workflows.
- Automated two-pass encoding for generic-pass encoders used by the guides: libx264, native MPEG-4, libvpx/VP9, libaom-av1 and FFV1. Pass 1 uses the null muxer; pass 2 writes to the configured output. Auto-managed pass-log files are deleted after the job. Encoder-specific flows such as x265 private pass/stats controls remain editable through the general settings editor.

## Temporary-file cleanup

- FFGrabber snapshots the existing matching temporary-file names before an encode and, after FFmpeg exits or is cancelled, removes only new `.tmp` files matching the output-specific name, HLS/DASH segments, or explicitly supplied segment templates.
- **Clean output .tmp files…** lists older output-related candidates and asks for confirmation before deleting.
- It never recursively clears arbitrary temporary files, and existing files are not deleted automatically.

## Build and packaging

- Windows x64 and ARM64 installers are built by GitHub Actions.
- CI checks the PE machine type, rejects bundled FFmpeg executables, compiles both installers and generates SHA-256 checksums.
- FFGrabber remains import-only: users supply their own `ffmpeg.exe`, `ffprobe.exe` and `ffplay.exe`.
