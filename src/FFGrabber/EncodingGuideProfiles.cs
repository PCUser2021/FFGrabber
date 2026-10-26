namespace FFGrabber;

/// <summary>
/// Curated, editable one-click recipes mapped from applicable FFmpeg source-guide examples.
/// The complete source text is separately preserved in the Source Guide Library; explanatory
/// and historical documents do not automatically become one-click profiles. Every profile
/// is a starting point, not a guarantee that the imported FFmpeg build includes its encoder.
/// </summary>
public sealed record EncodingGuideProfile
{
    public required string Name { get; init; }
    public required string SourceFile { get; init; }
    public required string Description { get; init; }

    public string Mode { get; init; } = "Convert / encode";
    public string VideoCodec { get; init; } = "libx264";
    public string AudioCodec { get; init; } = "copy";
    public string Preset { get; init; } = "medium";
    public string RateControl { get; init; } = "CRF quality";
    public string Crf { get; init; } = "23";
    public string VideoBitrate { get; init; } = "8M";
    public string OutputSize { get; init; } = "source";
    public string FrameRate { get; init; } = "source";
    public string PixelFormat { get; init; } = "auto";
    public string AudioBitrate { get; init; } = "192k";
    public string InputFormat { get; init; } = "auto";
    public string OutputFormat { get; init; } = "auto";
    public bool PreserveMetadata { get; init; } = true;
    public bool FastStart { get; init; }
    public bool TwoPass { get; init; }
    public string PassLogFile { get; init; } = "";

    public string GlobalOptions { get; init; } = "-threads 0";
    public string InputOptions { get; init; } = "";
    public string AdditionalInputs { get; init; } = "";
    public string MappingOptions { get; init; } = "";
    public string VideoEncoderOptions { get; init; } = "";
    public string AudioEncoderOptions { get; init; } = "";
    public string FilterOptions { get; init; } = "";
    public string MuxerOptions { get; init; } = "";
    public string OutputOptions { get; init; } = "";

    public string MinVideoBitrate { get; init; } = "";
    public string MaxVideoBitrate { get; init; } = "";
    public string BufferSize { get; init; } = "";
    public string GopSize { get; init; } = "";
    public string VideoTune { get; init; } = "";
    public string VideoProfile { get; init; } = "";
    public string VideoQscale { get; init; } = "";
    public string VideoQp { get; init; } = "";

    public string AudioQscale { get; init; } = "";
    public string AudioVbr { get; init; } = "";
    public string AudioProfile { get; init; } = "";
    public string AudioCutoff { get; init; } = "";
    public string AudioSampleRate { get; init; } = "";
    public string AudioChannels { get; init; } = "";

    public string ColorPrimaries { get; init; } = "";
    public string ColorTransfer { get; init; } = "";
    public string ColorSpace { get; init; } = "";
}

public static class EncodingGuideProfiles
{
    public static IReadOnlyList<EncodingGuideProfile> All { get; } = Array.AsReadOnly<EncodingGuideProfile>(
    [
        new()
        {
            Name = "H.264 — general quality (CRF 23)",
            SourceFile = "Encode_H.264.txt",
            Description = "Single-pass H.264 using a balanced preset. CRF 23 is the libx264 default; lower values improve quality and increase size.",
            VideoCodec = "libx264", AudioCodec = "copy", Preset = "medium",
            RateControl = "CRF quality", Crf = "23", PixelFormat = "yuv420p"
        },
        new()
        {
            Name = "H.264 — high-quality upload / YouTube",
            SourceFile = "Encode_YouTube.txt; Encode_H.264.txt",
            Description = "The guide's high-quality upload recipe: libx264 slow, CRF 18, broadly compatible 4:2:0 pixel format. Re-encoding audio is optional.",
            VideoCodec = "libx264", AudioCodec = "aac", Preset = "slow",
            RateControl = "CRF quality", Crf = "18", PixelFormat = "yuv420p",
            AudioBitrate = "192k", FastStart = true
        },
        new()
        {
            Name = "H.264 — two-pass target bitrate",
            SourceFile = "Encode_H.264.txt; Encode_YouTube.txt",
            Description = "Automated two-pass libx264 at a 2 Mbit/s target, slow preset, yuv420p and AAC 128k. FFGrabber runs pass 1 to the null muxer, then pass 2 to the chosen output; generated pass logs are cleaned up afterward.",
            VideoCodec = "libx264", AudioCodec = "aac", Preset = "slow",
            RateControl = "Target bitrate", VideoBitrate = "2000k", PixelFormat = "yuv420p",
            AudioBitrate = "128k", TwoPass = true, FastStart = true
        },
        new()
        {
            Name = "H.264 — constrained quality / VBV",
            SourceFile = "Encode_H.264.txt",
            Description = "Targets CRF 23 while constraining bitrate peaks with a 1 Mbit/s maxrate and 2 Mbit buffer.",
            VideoCodec = "libx264", AudioCodec = "copy", Preset = "slow",
            RateControl = "CRF quality", Crf = "23", PixelFormat = "yuv420p",
            MaxVideoBitrate = "1M", BufferSize = "2M"
        },
        new()
        {
            Name = "H.264 — low-latency live stream",
            SourceFile = "EncodingForStreamingSites.txt",
            Description = "RTMP starter: veryfast, 3 Mbit/s target and cap, 6 Mbit buffer, 2-second GOP at 30 fps, AAC stereo 44.1 kHz. Set the Output field to your RTMP URL.",
            VideoCodec = "libx264", AudioCodec = "aac", Preset = "veryfast",
            RateControl = "Target bitrate", VideoBitrate = "3000k", PixelFormat = "yuv420p",
            AudioBitrate = "128k", AudioSampleRate = "44100", AudioChannels = "2",
            MaxVideoBitrate = "3000k", BufferSize = "6000k", GopSize = "60",
            VideoTune = "zerolatency", OutputFormat = "flv"
        },
        new()
        {
            Name = "H.264 — lossless / QP 0",
            SourceFile = "Encode_H.264.txt",
            Description = "Uses libx264 quantizer 0. Lossless H.264 can create very large files and is not widely supported by non-FFmpeg players.",
            VideoCodec = "libx264", AudioCodec = "copy", Preset = "veryslow",
            RateControl = "Encoder-specific options", PixelFormat = "yuv444p",
            VideoQp = "0"
        },
        new()
        {
            Name = "H.264 — frame-by-frame / all-I VFX review",
            SourceFile = "Encode_VFX.txt; Encode_H.264.txt",
            Description = "All-intra GOP with no B-frames and still-image tune to improve frame-by-frame scrubbing; greatly increases bitrate.",
            VideoCodec = "libx264", AudioCodec = "copy", Preset = "slow",
            RateControl = "CRF quality", Crf = "9", PixelFormat = "yuv420p",
            GopSize = "1", VideoTune = "stillimage",
            VideoEncoderOptions = "-bf 0"
        },
        new()
        {
            Name = "H.265 / HEVC — quality (CRF 26)",
            SourceFile = "Encode_H.265.txt",
            Description = "libx265 CRF 26 with fast preset and AAC 128k, following the H.265 guide example. Requires an FFmpeg build containing libx265.",
            VideoCodec = "libx265", AudioCodec = "aac", Preset = "fast",
            RateControl = "CRF quality", Crf = "26", AudioBitrate = "128k",
            FastStart = true
        },
        new()
        {
            Name = "H.265 / HEVC — lossless",
            SourceFile = "Encode_H.265.txt",
            Description = "True lossless libx265 video using the H.265 guide's -x265-params lossless=1. CRF 0 is not required. Audio is stream-copied to avoid adding a lossy audio encode; output can be very large. Requires an imported FFmpeg build containing libx265.",
            VideoCodec = "libx265", AudioCodec = "copy", Preset = "medium",
            RateControl = "Encoder-specific options", PixelFormat = "auto",
            VideoEncoderOptions = "-x265-params lossless=1", FastStart = true
        },
        new()
        {
            Name = "H.265 / HEVC — Apple hvc1 compatibility",
            SourceFile = "Encode_H.265.txt",
            Description = "H.265 quality encode plus the hvc1 sample-entry tag recommended by the guide for Apple / Final Cut compatibility.",
            VideoCodec = "libx265", AudioCodec = "aac", Preset = "fast",
            RateControl = "CRF quality", Crf = "26", AudioBitrate = "128k",
            OutputOptions = "-tag:v hvc1", FastStart = true
        },
        new()
        {
            Name = "AV1 — SVT-AV1 quality",
            SourceFile = "Encode_AV1.txt",
            Description = "SVT-AV1 CRF 35 with preset 8 and visual-quality tuning. Actual preset/parameter ranges depend on the imported FFmpeg and SVT-AV1 versions.",
            VideoCodec = "libsvtav1", AudioCodec = "copy", Preset = "8",
            RateControl = "CRF quality", Crf = "35",
            VideoEncoderOptions = "-svtav1-params tune=0"
        },
        new()
        {
            Name = "AV1 — libaom constant quality",
            SourceFile = "Encode_AV1.txt",
            Description = "libaom CRF 30 with row multithreading and a speed/quality control. Query encoder help to check which options your build supports.",
            VideoCodec = "libaom-av1", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options",
            VideoEncoderOptions = "-crf 30 -cpu-used 4 -row-mt 1"
        },
        new()
        {
            Name = "AV1 — HDR10 / 10-bit BT.2020 PQ",
            SourceFile = "Encode_AV1.txt",
            Description = "10-bit 4:2:0 AV1 with BT.2020 non-constant luminance, BT.2020 primaries and SMPTE ST 2084 transfer tags. Requires genuine HDR source/mastering data for accurate HDR output.",
            VideoCodec = "libsvtav1", AudioCodec = "copy", Preset = "8",
            RateControl = "CRF quality", Crf = "35", PixelFormat = "yuv420p10le",
            VideoTune = "", ColorPrimaries = "bt2020", ColorTransfer = "smpte2084",
            ColorSpace = "bt2020nc"
        },
        new()
        {
            Name = "VP9 — constant quality",
            SourceFile = "Encode_VP9.txt",
            Description = "libvpx-vp9 single-pass constant quality needs both -crf and -b:v 0. Use libopus for WebM audio where available.",
            VideoCodec = "libvpx-vp9", AudioCodec = "libopus", Preset = "encoder default",
            RateControl = "Encoder-specific options", AudioBitrate = "128k",
            OutputFormat = "webm",
            VideoEncoderOptions = "-crf 30 -b:v 0 -deadline good -cpu-used 1 -row-mt 1"
        },
        new()
        {
            Name = "VP9 — two-pass target bitrate",
            SourceFile = "Encode_VP9.txt",
            Description = "Automated two-pass libvpx-vp9 at 2 Mbit/s with WebM/Opus. FFGrabber manages the null-muxer analysis pass and reuses its pass log for the final output.",
            VideoCodec = "libvpx-vp9", AudioCodec = "libopus", Preset = "encoder default",
            RateControl = "Target bitrate", VideoBitrate = "2000k", AudioBitrate = "128k",
            OutputFormat = "webm", TwoPass = true,
            VideoEncoderOptions = "-deadline good -cpu-used 1 -row-mt 1"
        },
        new()
        {
            Name = "VP9 — constrained quality / bitrate cap",
            SourceFile = "Encode_VP9.txt",
            Description = "VP9 CRF 30 constrained by target and maximum bitrates. Adjust values to the actual bandwidth/file-size target.",
            VideoCodec = "libvpx-vp9", AudioCodec = "libopus", Preset = "encoder default",
            RateControl = "Encoder-specific options", AudioBitrate = "128k",
            OutputFormat = "webm",
            VideoEncoderOptions = "-crf 30 -b:v 2000k",
            MinVideoBitrate = "500k", MaxVideoBitrate = "2500k"
        },
        new()
        {
            Name = "VP9 — lossless",
            SourceFile = "Encode_VP9.txt",
            Description = "libvpx-vp9 lossless mode. Output files will be much larger than typical lossy WebM encodes.",
            VideoCodec = "libvpx-vp9", AudioCodec = "libopus", Preset = "encoder default",
            RateControl = "Encoder-specific options", AudioBitrate = "128k",
            OutputFormat = "webm", VideoEncoderOptions = "-lossless 1"
        },
        new()
        {
            Name = "VP8 — constrained quality",
            SourceFile = "Encode_VP8.txt",
            Description = "VP8 constant-quality starter: CRF 10 with a 1 Mbit/s target cap and relaxed quantizer limits.",
            VideoCodec = "libvpx", AudioCodec = "libvorbis", Preset = "encoder default",
            RateControl = "Encoder-specific options", AudioBitrate = "192k",
            OutputFormat = "webm",
            VideoEncoderOptions = "-crf 10 -b:v 1M -qmin 0 -qmax 50"
        },
        new()
        {
            Name = "VP8 — alpha channel",
            SourceFile = "Encode_VP8.txt",
            Description = "VP8 with an alpha plane for compatible browsers/players. Input should contain alpha, such as a PNG image sequence.",
            VideoCodec = "libvpx", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Target bitrate", VideoBitrate = "1M", PixelFormat = "yuva420p",
            OutputFormat = "webm", OutputOptions = "-metadata:s:v:0 alpha_mode=1"
        },
        new()
        {
            Name = "FFV1 — two-pass archival",
            SourceFile = "Encode_FFV1.txt",
            Description = "Two-pass lossless FFV1 v3 archival encode with range coder, large context, GOP 1 and slice CRC. FFmpeg uses a managed pass log; no bitrate target is required by this profile.",
            VideoCodec = "ffv1", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputFormat = "matroska", TwoPass = true,
            VideoEncoderOptions = "-level 3 -coder 1 -context 1 -g 1 -slices 24 -slicecrc 1"
        },
        new()
        {
            Name = "FFV1 — archival / preservation",
            SourceFile = "Encode_FFV1.txt",
            Description = "Lossless FFV1 v3 with range coder, large context, GOP 1 and slice CRC. Keep the source pixel format unless you intentionally want a format conversion.",
            VideoCodec = "ffv1", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputFormat = "matroska",
            VideoEncoderOptions = "-level 3 -coder 1 -context 1 -g 1 -slices 24 -slicecrc 1"
        },
        new()
        {
            Name = "ProRes — VFX review / HQ 422",
            SourceFile = "Encode_VFX.txt",
            Description = "ProRes 422 HQ profile with 10-bit 4:2:2, vendor tag, 2K resolution and 48 fps as in the VFX guide example. Set the input to an image sequence if appropriate.",
            VideoCodec = "prores_ks", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputSize = "2048x1152",
            FrameRate = "48", PixelFormat = "yuv422p10le",
            InputFormat = "image2", OutputFormat = "mov", InputOptions = "-r 48 -force_fps",
            VideoEncoderOptions = "-vendor apl0",
            VideoProfile = "3", VideoQscale = "11"
        },
        new()
        {
            Name = "ProRes — VFX 4444 / alpha",
            SourceFile = "Encode_VFX.txt",
            Description = "ProRes 4444-style profile using an alpha-capable 4:4:4:4 pixel format; verify the imported prores_ks help for supported combinations.",
            VideoCodec = "prores_ks", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputSize = "2048x1152",
            FrameRate = "48", PixelFormat = "yuva444p10le",
            InputFormat = "image2", OutputFormat = "mov", InputOptions = "-r 48 -force_fps",
            VideoEncoderOptions = "-vendor apl0",
            VideoProfile = "4", VideoQscale = "11"
        },
        new()
        {
            Name = "Photo JPEG — VFX intermediate",
            SourceFile = "Encode_VFX.txt",
            Description = "High-quality MJPEG review/intermediate at qscale 1 and 4:2:2 JPEG pixel format. The original guide warns that 2K+ files can be very large.",
            VideoCodec = "mjpeg", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputSize = "2048x1152",
            FrameRate = "48", PixelFormat = "yuvj422p",
            InputFormat = "image2", OutputFormat = "mov", InputOptions = "-r 48 -force_fps",
            VideoEncoderOptions = "", VideoQscale = "1"
        },
        new()
        {
            Name = "MPEG-4 Part 2 — two-pass AVI",
            SourceFile = "Encode_MPEG-4.txt",
            Description = "Automated two-pass native MPEG-4 Part 2 at a 1.5 Mbit/s target, with Xvid FourCC for legacy AVI compatibility.",
            VideoCodec = "mpeg4", AudioCodec = "libmp3lame", Preset = "encoder default",
            RateControl = "Target bitrate", VideoBitrate = "1500k", OutputFormat = "avi",
            VideoEncoderOptions = "-vtag xvid", AudioBitrate = "128k", TwoPass = true
        },
        new()
        {
            Name = "MPEG-4 Part 2 / Xvid-compatible AVI",
            SourceFile = "Encode_MPEG-4.txt",
            Description = "Legacy AVI/Xvid compatibility profile with native MPEG-4, FourCC xvid, and qscale 3. Modern devices usually support H.264/HEVC/AV1 better.",
            VideoCodec = "mpeg4", AudioCodec = "libmp3lame", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputFormat = "avi",
            VideoEncoderOptions = "-vtag xvid",
            VideoProfile = "", VideoQscale = "3",
            AudioBitrate = "", AudioQscale = "4"
        },
        new()
        {
            Name = "Theora + Vorbis — Ogg",
            SourceFile = "TheoraVorbisEncodingGuide.txt",
            Description = "Theora video qscale 7 with libvorbis audio qscale 5. Both libraries must be present in the imported FFmpeg build.",
            VideoCodec = "libtheora", AudioCodec = "libvorbis", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputFormat = "ogg",
            VideoEncoderOptions = "", AudioEncoderOptions = "",
            VideoQscale = "7", AudioQscale = "5", AudioBitrate = ""
        },
        new()
        {
            Name = "MP3 — high-quality VBR",
            SourceFile = "Encode_MP3.txt; Encode_HighQualityAudio.txt",
            Description = "libmp3lame VBR quality 2 (lower qscale means higher quality). The cited guide considers q values 0–3 generally transparent on suitable material.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "libmp3lame",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioQscale = "2", AudioBitrate = "", OutputFormat = "mp3"
        },
        new()
        {
            Name = "MP3 — CBR 320 kbit/s",
            SourceFile = "Encode_MP3.txt",
            Description = "Maximum common MP3 CBR bitrate. The guide notes that this is often wasteful compared with transparent VBR or lossless FLAC.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "libmp3lame",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioBitrate = "320k", OutputFormat = "mp3"
        },
        new()
        {
            Name = "AAC LC — music / general compatibility",
            SourceFile = "Encode_AAC.txt; Encode_HighQualityAudio.txt",
            Description = "AAC-LC at 160 kbit/s. Adjust the bitrate to the program material, channel count and target devices.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "aac",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioBitrate = "160k", OutputFormat = "m4a", FastStart = true
        },
        new()
        {
            Name = "AAC — native encoder VBR",
            SourceFile = "Encode_AAC.txt",
            Description = "Native FFmpeg AAC with q:a 2 VBR; the guide describes its VBR range as experimental/build-dependent. This profile deliberately leaves CBR bitrate blank.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "aac",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioQscale = "2", AudioBitrate = "", OutputFormat = "m4a", FastStart = true
        },
        new()
        {
            Name = "AAC HE — low bitrate (FDK AAC required)",
            SourceFile = "Encode_AAC.txt",
            Description = "HE-AAC v1 profile at 64 kbit/s with cutoff 18 kHz. Requires libfdk_aac and an HE-AAC-capable playback target; ordinary AAC-LC is more widely compatible.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "libfdk_aac",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioBitrate = "64k", AudioProfile = "aac_he", AudioCutoff = "18000",
            OutputFormat = "m4a", FastStart = true
        },
        new()
        {
            Name = "Opus — high-quality lossy audio",
            SourceFile = "Encode_HighQualityAudio.txt",
            Description = "libopus at 160 kbit/s, a reasonable high-quality stereo-music starting point from the high-quality-audio guide. MKV/WebM support is generally the best fit.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "libopus",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioBitrate = "160k", OutputFormat = "matroska"
        },
        new()
        {
            Name = "FLAC — lossless audio extraction",
            SourceFile = "Encode_HighQualityAudio.txt",
            Description = "Lossless audio output; no lossy transcode is introduced. FLAC does not use an audio bitrate target.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "flac",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioBitrate = "", OutputFormat = "flac"
        },
        new()
        {
            Name = "Streaming-site file — H.264 720p ladder starter",
            SourceFile = "EncodingForStreamingSites.txt",
            Description = "Guide-style streaming file preset: H.264 medium, 3 Mbit/s target, 3 Mbit/s maxrate, 6 Mbit buffer, 2-channel AAC 128k/44.1 kHz, 2-second GOP at 25 fps. Confirm the platform's current ingest limits before streaming.",
            VideoCodec = "libx264", AudioCodec = "aac", Preset = "medium",
            RateControl = "Target bitrate", VideoBitrate = "3000k",
            PixelFormat = "yuv420p", AudioBitrate = "128k",
            AudioChannels = "2", AudioSampleRate = "44100",
            MaxVideoBitrate = "3000k", BufferSize = "6000k", GopSize = "50",
            OutputFormat = "flv"
        },
        new()
        {
            Name = "Stream-copy remux — avoid lossy re-encoding",
            SourceFile = "Encode_HighQualityAudio.txt; Encode_AAC.txt; Encode_YouTube.txt",
            Description = "Copies supported audio/video streams without re-encoding. Choose a container that supports every stream; the command preview shows the exact muxing options.",
            Mode = "Remux / stream copy", VideoCodec = "copy", AudioCodec = "copy",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioBitrate = "", OutputFormat = "auto", FastStart = true
        },
        // Reconstructed payloads supplied during recovery. These profiles map each
        // summary's recommended flags into the normal GUI controls so the settings can
        // be reviewed, adjusted and previewed before a job is queued.
        new()
        {
            Name = "Recovered — H.264 Master Reference",
            SourceFile = "h264_master_guide.txt",
            Description = "Recovered profile payload: libx264 slow, CRF 22 and yuv420p. A general compatibility-oriented H.264 starting point; review the target's requirements before encoding.",
            VideoCodec = "libx264", AudioCodec = "copy", Preset = "slow",
            RateControl = "CRF quality", Crf = "22", PixelFormat = "yuv420p"
        },
        new()
        {
            Name = "Recovered — H.265 HEVC Deep Dive",
            SourceFile = "h265_hevc_deep_dive.txt",
            Description = "Recovered profile payload: libx265 medium, CRF 28 and 10-bit yuv420p10le. Ten-bit output does not by itself make SDR content HDR; preserve color metadata only when it matches the source/master.",
            VideoCodec = "libx265", AudioCodec = "copy", Preset = "medium",
            RateControl = "CRF quality", Crf = "28", PixelFormat = "yuv420p10le"
        },
        new()
        {
            Name = "Recovered — AV1 NextGen Delivery",
            SourceFile = "av1_nextgen_delivery.txt",
            Description = "Recovered profile payload: libaom-av1 constant-quality mode with CRF 30 and bitrate zero. The supplied -strict experimental flag is retained as a raw option for traceability; modern FFmpeg builds may not need it.",
            VideoCodec = "libaom-av1", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options",
            VideoEncoderOptions = "-crf 30 -b:v 0 -strict experimental"
        },
        new()
        {
            Name = "Recovered — VP9 WebM Publishing",
            SourceFile = "vp9_webm_publishing.txt",
            Description = "Recovered profile payload: libvpx-vp9 CRF 31 with bitrate zero in WebM. Confirm that the imported FFmpeg build includes libvpx-vp9.",
            VideoCodec = "libvpx-vp9", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputFormat = "webm",
            VideoEncoderOptions = "-crf 31 -b:v 0"
        },
        new()
        {
            Name = "Recovered — FFV1 Lossless Archive",
            SourceFile = "ffv1_lossless_archive.txt",
            Description = "Recovered profile payload: FFV1 level 3 with range coder and context 1. This is a lossless video encode; keep the source pixel format unless a deliberate conversion is required.",
            VideoCodec = "ffv1", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputFormat = "matroska",
            VideoEncoderOptions = "-level 3 -coder 1 -context 1"
        },
        new()
        {
            Name = "Recovered — ProRes VFX Intermediate",
            SourceFile = "prores_vfx_intermediate.txt",
            Description = "Recovered profile payload: ProRes HQ profile 3, ap10 vendor tag and 10-bit 4:2:2 pixel format. The supplied vendor tag is preserved exactly; inspect the imported encoder help if it rejects that value.",
            VideoCodec = "prores_ks", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputFormat = "mov",
            PixelFormat = "yuv422p10le", VideoProfile = "3",
            VideoEncoderOptions = "-vendor ap10"
        },
        new()
        {
            Name = "Recovered — MPEG-4 Legacy Compatibility",
            SourceFile = "mpeg4_legacy_compatibility.txt",
            Description = "Recovered profile payload: native MPEG-4 Part 2 with Xvid FourCC and qscale 4. This is a legacy compatibility profile; modern delivery targets usually prefer H.264, HEVC or AV1.",
            VideoCodec = "mpeg4", AudioCodec = "copy", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputFormat = "avi",
            VideoQscale = "4", VideoEncoderOptions = "-vtag xvid"
        },
        new()
        {
            Name = "Recovered — Theora Ogg Fallback",
            SourceFile = "theora_ogg_fallback.txt",
            Description = "Recovered profile payload: Theora video qscale 6 and Vorbis audio qscale 5 in Ogg. The exact quality scale and library availability depend on the imported build.",
            VideoCodec = "libtheora", AudioCodec = "libvorbis", Preset = "encoder default",
            RateControl = "Encoder-specific options", OutputFormat = "ogg",
            VideoQscale = "6", AudioQscale = "5", AudioBitrate = ""
        },
        new()
        {
            Name = "Recovered — AAC Audio Standards",
            SourceFile = "aac_audio_standards.txt",
            Description = "Recovered profile payload: extract audio using the native AAC encoder at 192 kbit/s. The output container defaults to M4A for a common audio-only delivery target.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "aac",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioBitrate = "192k", OutputFormat = "ipod", FastStart = true
        },
        new()
        {
            Name = "Recovered — MP3 Vintage Distribution",
            SourceFile = "mp3_vintage_distribution.txt",
            Description = "Recovered profile payload: libmp3lame VBR quality scale 2. Lower LAME qscale values generally mean higher quality and larger files; no fixed bitrate is added.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "libmp3lame",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioQscale = "2", AudioBitrate = "", OutputFormat = "mp3"
        },
        new()
        {
            Name = "Recovered — Opus Interactive Telephony",
            SourceFile = "opus_interactive_telephony.txt",
            Description = "Recovered profile payload: Opus at 128 kbit/s with VBR enabled. Low latency also depends on the surrounding input, buffering and muxing choices; this profile does not force a real-time protocol.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "libopus",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioBitrate = "128k", AudioVbr = "on",
            AudioEncoderOptions = "-b:a 128k", OutputFormat = "matroska"
        },
        new()
        {
            Name = "Recovered — FLAC Bit-Perfect Master",
            SourceFile = "flac_bit_perfect_master.txt",
            Description = "Recovered profile payload: FLAC audio extraction with compression level 5. FLAC preserves decoded audio samples, but cannot restore information already lost in a lossy source.",
            Mode = "Extract audio", VideoCodec = "copy", AudioCodec = "flac",
            Preset = "encoder default", RateControl = "Encoder-specific options",
            AudioBitrate = "", AudioEncoderOptions = "-compression_level 5",
            OutputFormat = "flac"
        },
        new()
        {
            Name = "Recovered — YouTube Upload Recipes",
            SourceFile = "youtube_upload_recipes.txt",
            Description = "Recovered profile payload: libx264 slow CRF 18, B-frames 2, GOP 30 and AAC 384 kbit/s. GOP 30 is a one-second interval only for 30 fps material; verify current platform ingest guidance for the target upload.",
            VideoCodec = "libx264", AudioCodec = "aac", Preset = "slow",
            RateControl = "CRF quality", Crf = "18", PixelFormat = "yuv420p",
            GopSize = "30", VideoEncoderOptions = "-bf 2",
            AudioBitrate = "384k", OutputFormat = "mp4", FastStart = true
        },
        new()
        {
            Name = "Recovered — Streaming Live Delivery",
            SourceFile = "streaming_live_delivery.txt",
            Description = "Recovered profile payload: libx264 veryfast, zerolatency tune, 4 Mbit/s target and maxrate with an 8 Mbit buffer. FLV is set as a typical RTMP target; switch the output muxer to HLS when producing an HLS playlist instead.",
            VideoCodec = "libx264", AudioCodec = "copy", Preset = "veryfast",
            RateControl = "Target bitrate", VideoBitrate = "4000k",
            MaxVideoBitrate = "4000k", BufferSize = "8000k",
            VideoTune = "zerolatency", OutputFormat = "flv"
        },
        new()
        {
            Name = "Streaming — File to RTMP (full guides)",
            SourceFile = "EncodingForStreamingSites.txt; StreamingGuide.txt",
            Description = "For a prerecorded file only. Adds -re so the file is sent in real time; do not use -re on a live capture input. At 30 fps, GOP 60 is a two-second keyframe interval. Tune bitrate against measured upload capacity and the service's ingest limits. Replace the output with your RTMP endpoint; never commit a stream key.",
            Mode = "Convert / encode", VideoCodec = "libx264", AudioCodec = "aac",
            Preset = "veryfast", RateControl = "Target bitrate", VideoBitrate = "3000k",
            FrameRate = "30", PixelFormat = "yuv420p", AudioBitrate = "128k",
            InputOptions = "-re", MaxVideoBitrate = "3000k", BufferSize = "6000k",
            GopSize = "60", AudioSampleRate = "44100", AudioChannels = "2",
            OutputFormat = "flv"
        },
        new()
        {
            Name = "Streaming — Live input to RTMP (full guides)",
            SourceFile = "EncodingForStreamingSites.txt; StreamingGuide.txt",
            Description = "For a live input device or already-live source; do not set -re because that option is intended to pace file input. InputFormat is set to dshow as a Windows starting point. Replace Input with the exact device expression discovered on this PC. Values assume a 30 fps, 3 Mbit/s stream and should be adjusted to the service's current requirements.",
            Mode = "Convert / encode", InputFormat = "dshow",
            VideoCodec = "libx264", AudioCodec = "aac", Preset = "veryfast",
            RateControl = "Target bitrate", VideoBitrate = "3000k",
            FrameRate = "30", PixelFormat = "yuv420p", AudioBitrate = "128k",
            MaxVideoBitrate = "3000k", BufferSize = "6000k", GopSize = "60",
            VideoTune = "zerolatency", AudioSampleRate = "44100", AudioChannels = "2",
            OutputFormat = "flv"
        },
        new()
        {
            Name = "Streaming — RTMP plus local recording (tee)",
            SourceFile = "EncodingForStreamingSites.txt; StreamingGuide.txt",
            Description = "Advanced multi-destination workflow. Set Output to a tee target such as [f=flv:onfail=ignore]rtmp://YOUR-SERVER/YOUR-STREAM-KEY|local-recording.mkv, with the target in one argument. This encodes once for the stream and local recording. +global_header is included as recommended by the historical tee example; confirm each target's muxer and encoder requirements before running.",
            Mode = "Convert / encode", VideoCodec = "libx264", AudioCodec = "aac",
            Preset = "veryfast", RateControl = "Target bitrate", VideoBitrate = "3000k",
            FrameRate = "30", PixelFormat = "yuv420p", AudioBitrate = "128k",
            MaxVideoBitrate = "3000k", BufferSize = "6000k", GopSize = "60",
            AudioSampleRate = "44100", AudioChannels = "2", OutputFormat = "tee",
            OutputOptions = "-flags +global_header"
        }
    ]);

    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(
        All.Select(profile => profile.Name).ToArray());

    public static EncodingGuideProfile? Find(string? name) =>
        All.FirstOrDefault(profile => string.Equals(profile.Name, name, StringComparison.Ordinal));
}
