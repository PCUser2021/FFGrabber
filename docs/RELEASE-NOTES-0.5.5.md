# FFGrabber 0.5.5

## Encoding-guide settings made real GUI controls

This release incorporates the 14 supplied Dropbox encoding-guide text files into the app's settings model and interactive GUI. It includes **35 editable encoding profiles** and a **124-entry searchable guide option index**, in addition to live option discovery from the imported FFmpeg executable's `-h full` catalogue.

The profiles cover H.264/H.265, AV1, VP8/VP9, FFV1 archival, ProRes/MJPEG VFX intermediates, MPEG-4/Xvid, Theora/Vorbis, AAC, MP3, Opus, FLAC, and YouTube/streaming workflows. Dedicated fields include min/max video bitrate, VBV buffer, GOP/keyframe interval, encoder tune/profile, video Qscale/QP, audio Qscale and FDK VBR/profile/cutoff, audio sample rate/channels, HDR color primaries/transfer/colorspace, additional input sources, and a two-pass checkbox plus pass-log prefix.

The **Complete Settings Editor** can load the guide-derived index or the exact imported binary's live `-h full` options, filter entries, display their descriptions and source filenames, and add options to the recommended argument stage. Help/display and build-time configure flags are reference-only; they cannot be inserted as ordinary runtime encoding switches. Output metadata/HDR tags and muxer tags are routed to output stages, filters take precedence over suffix-based audio classification, and `-f` is routed based on the option category.

## Two-pass workflows

Automated generic `-pass` handling is available for libx264, native MPEG-4, libvpx/VP9, libaom-av1, and FFV1. The GUI previews pass 1 and pass 2 separately. Pass 1 writes to the null muxer and drops audio; final-output muxer flags are not passed to the null output. Pass 2 writes the selected output. FFGrabber-managed pass-log files use a unique prefix under the local application-data directory and are cleaned after the job. A custom prefix is respected and left in place. Rate-control validation prevents common invalid two-pass combinations; encoder-private modes remain editable for encoders such as x265 and hardware backends.

## Output temporary-file fix

- Before each FFmpeg job, FFGrabber snapshots existing temp-file names and builds a narrow set of output-specific patterns.
- After FFmpeg has exited (including after cancellation), it removes only new `.tmp` files matching the selected output, output-stem temp writes, HLS/DASH segments, or explicitly named segment templates.
- The configured output itself is protected even when its name ends in `.tmp`.
- **Clean output .tmp files…** lists existing matching candidates and requires explicit confirmation before deleting them. Unrelated temp files and subdirectories are not recursively swept.

## Build and packaging

GitHub Actions builds and validates both Windows x64 and ARM64 installers, checks the executable architecture, rejects packaged FFmpeg binaries, compiles the setup programs, and produces SHA-256 checksums. FFGrabber remains import-only: users provide their own `ffmpeg.exe`, `ffprobe.exe`, and `ffplay.exe`.
