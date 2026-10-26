using FFGrabber;
using FFGrabber.Models;
using FFGrabber.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFGrabber.Tests;

[TestClass]
public sealed class FFmpegCommandBuilderTests
{
    [TestMethod]
    public void H265LosslessGuideProfile_IsSelectableAndBuildsLosslessX265Arguments()
    {
        var profile = EncodingGuideProfiles.Find("H.265 / HEVC — lossless");
        Assert.IsNotNull(profile, "The explicit lossless mode in Encode_H.265.txt must have a selectable profile.");
        Assert.AreEqual("Encode_H.265.txt", profile!.SourceFile);
        Assert.AreEqual("libx265", profile.VideoCodec);
        Assert.AreEqual("Encoder-specific options", profile.RateControl);
        Assert.AreEqual("copy", profile.AudioCodec);
        Assert.AreEqual("-x265-params lossless=1", profile.VideoEncoderOptions);

        using var directory = new TemporaryDirectory();
        var settings = new EncodingSettings(
            InputPath: "input.mkv",
            OutputPath: System.IO.Path.Combine(directory.Path, "lossless.mkv"),
            Mode: profile.Mode,
            VideoCodec: profile.VideoCodec,
            Preset: profile.Preset,
            Crf: 23,
            AudioCodec: profile.AudioCodec,
            AudioBitrate: profile.AudioBitrate,
            Overwrite: true,
            RateControl: profile.RateControl,
            VideoBitrate: profile.VideoBitrate,
            OutputSize: profile.OutputSize,
            FrameRate: profile.FrameRate,
            PixelFormat: profile.PixelFormat,
            PreserveMetadata: profile.PreserveMetadata,
            FastStart: profile.FastStart,
            GlobalOptions: profile.GlobalOptions,
            InputOptions: profile.InputOptions,
            StreamMappingOptions: profile.MappingOptions,
            VideoEncoderOptions: profile.VideoEncoderOptions,
            AudioEncoderOptions: profile.AudioEncoderOptions,
            FilterOptions: profile.FilterOptions,
            MuxerOptions: profile.MuxerOptions,
            OutputOptions: profile.OutputOptions,
            InputFormat: profile.InputFormat,
            OutputFormat: profile.OutputFormat,
            AdditionalInputs: profile.AdditionalInputs,
            TwoPass: profile.TwoPass,
            PassLogFile: profile.PassLogFile);

        var args = FFmpegCommandBuilder.Build(settings);

        Assert.AreEqual("lossless=1", GetOptionValue(args, "-x265-params"));
        Assert.AreEqual("libx265", GetOptionValue(args, "-c:v"));
        Assert.AreEqual("copy", GetOptionValue(args, "-c:a"));
        Assert.IsFalse(args.Contains("-crf", StringComparer.OrdinalIgnoreCase),
            "The source guide explicitly says CRF 0 is not required for x265 lossless mode.");
        Assert.IsFalse(args.Contains("-b:v", StringComparer.OrdinalIgnoreCase),
            "Lossless mode must not accidentally force a target bitrate.");
        Assert.AreEqual(1, args.Count(option => string.Equals(option, "-x265-params", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Build_UsesBalancedThreadBudgetAndHonorsManualOverride()
    {
        using var directory = new TemporaryDirectory();
        var settings = new EncodingSettings(
            InputPath: "input.mp4",
            OutputPath: System.IO.Path.Combine(directory.Path, "output.mkv"),
            Mode: "Convert / encode",
            VideoCodec: "libx264",
            Preset: "medium",
            Crf: 20,
            AudioCodec: "aac",
            AudioBitrate: "192k",
            Overwrite: true,
            RateControl: "CRF quality",
            VideoBitrate: "8M",
            OutputSize: "source",
            FrameRate: "source",
            PixelFormat: "auto",
            PreserveMetadata: true,
            FastStart: true);

        var balanced = FFmpegCommandBuilder.Build(settings);
        var expectedThreads = Math.Max(1, (int)Math.Floor(Environment.ProcessorCount * 0.75));
        Assert.AreEqual(expectedThreads.ToString(System.Globalization.CultureInfo.InvariantCulture),
            GetOptionValue(balanced, "-threads:v"));
        Assert.AreEqual(expectedThreads.ToString(System.Globalization.CultureInfo.InvariantCulture),
            GetOptionValue(balanced, "-filter_threads"));

        var manual = FFmpegCommandBuilder.Build(settings with { VideoEncoderOptions = "-threads:v 2" });
        Assert.AreEqual("2", GetOptionValue(manual, "-threads:v"));
        Assert.AreEqual(1, manual.Count(option => string.Equals(option, "-threads:v", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void BuildTwoPass_CreatesOrderedAnalysisAndFinalPassArguments()
    {
        using var directory = new TemporaryDirectory();
        var output = System.IO.Path.Combine(directory.Path, "output.mp4");
        var settings = CreateTwoPassSettings(output, System.IO.Path.Combine(directory.Path, "pass-stats"));

        var plan = FFmpegCommandBuilder.BuildTwoPass(settings);

        Assert.IsFalse(plan.IsAutoManagedPassLog);
        Assert.AreEqual("1", GetOptionValue(plan.FirstPassArguments, "-pass"));
        Assert.AreEqual("2", GetOptionValue(plan.SecondPassArguments, "-pass"));
        Assert.AreEqual(plan.PassLogPrefix, GetOptionValue(plan.FirstPassArguments, "-passlogfile"));
        Assert.AreEqual(plan.PassLogPrefix, GetOptionValue(plan.SecondPassArguments, "-passlogfile"));
        Assert.AreEqual("null", GetOptionValue(plan.FirstPassArguments, "-f"));
        Assert.AreEqual("NUL", plan.FirstPassArguments[^1]);
        Assert.IsTrue(plan.FirstPassArguments.Contains("-an"), "The analysis pass must not encode audio.");
        Assert.IsFalse(plan.SecondPassArguments.Contains("-an"), "The final pass should retain configured audio.");
        Assert.AreEqual(output, plan.SecondPassArguments[^1]);
    }

    [TestMethod]
    public void BuildTwoPass_RejectsLiveCaptureFilterGraph()
    {
        using var directory = new TemporaryDirectory();
        var settings = CreateTwoPassSettings(
            System.IO.Path.Combine(directory.Path, "output.mp4"),
            System.IO.Path.Combine(directory.Path, "pass-stats")) with
        {
            CaptureFilterSource = "ddagrab=0"
        };

        Assert.ThrowsExactly<ArgumentException>(() => FFmpegCommandBuilder.BuildTwoPass(settings));
    }

    [TestMethod]
    public void CaptureGuideProfiles_RetainDirectShowAndWindowsDesktopEntryPoints()
    {
        var profiles = CaptureGuideProfiles.All;

        Assert.IsTrue(profiles.Any(profile => string.Equals(profile.InputFormat, "dshow", StringComparison.OrdinalIgnoreCase)),
            "DirectShow profiles should remain available.");
        Assert.IsTrue(profiles.Any(profile => string.Equals(profile.InputFormat, "gdigrab", StringComparison.OrdinalIgnoreCase)),
            "Windows gdigrab desktop capture should remain available.");
        Assert.IsTrue(profiles.Any(profile => profile.CaptureFilterSource.StartsWith("ddagrab=", StringComparison.OrdinalIgnoreCase)),
            "The DDAgrab desktop capture profile should remain available.");
        Assert.IsTrue(profiles.Any(profile => profile.CaptureFilterSource.StartsWith("gfxcapture=", StringComparison.OrdinalIgnoreCase)),
            "The Windows Graphics Capture profile should remain available.");
    }

    [TestMethod]
    public void RecoveredGuideProfiles_AreEmbeddedAndMapAllFourteenPayloads()
    {
        string[] expectedSources =
        [
            "h264_master_guide.txt",
            "h265_hevc_deep_dive.txt",
            "av1_nextgen_delivery.txt",
            "vp9_webm_publishing.txt",
            "ffv1_lossless_archive.txt",
            "prores_vfx_intermediate.txt",
            "mpeg4_legacy_compatibility.txt",
            "theora_ogg_fallback.txt",
            "aac_audio_standards.txt",
            "mp3_vintage_distribution.txt",
            "opus_interactive_telephony.txt",
            "flac_bit_perfect_master.txt",
            "youtube_upload_recipes.txt",
            "streaming_live_delivery.txt"
        ];

        var profiles = EncodingGuideProfiles.All
            .Where(profile => expectedSources.Contains(profile.SourceFile, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(profile => profile.SourceFile, StringComparer.OrdinalIgnoreCase);
        var resources = typeof(EncodingGuideProfiles).Assembly.GetManifestResourceNames();

        Assert.AreEqual(14, profiles.Count, "Every recovered text profile needs a matching one-click GUI profile.");
        foreach (var source in expectedSources)
        {
            Assert.IsTrue(profiles.ContainsKey(source), $"No encoder settings profile maps to {source}.");
            Assert.IsTrue(resources.Contains(
                "FFGrabber.source-guides.dropbox." + source,
                StringComparer.OrdinalIgnoreCase), $"The source payload {source} must be embedded in the app.");
        }

        Assert.AreEqual("libx264", profiles["h264_master_guide.txt"].VideoCodec);
        Assert.AreEqual("22", profiles["h264_master_guide.txt"].Crf);
        Assert.AreEqual("yuv420p", profiles["h264_master_guide.txt"].PixelFormat);
        Assert.IsTrue(profiles["av1_nextgen_delivery.txt"].VideoEncoderOptions.Contains("-b:v 0", StringComparison.Ordinal));
        Assert.AreEqual("webm", profiles["vp9_webm_publishing.txt"].OutputFormat);
        Assert.AreEqual("ogg", profiles["theora_ogg_fallback.txt"].OutputFormat);
        Assert.AreEqual("192k", profiles["aac_audio_standards.txt"].AudioBitrate);
        Assert.AreEqual("flac", profiles["flac_bit_perfect_master.txt"].OutputFormat);
        Assert.AreEqual("384k", profiles["youtube_upload_recipes.txt"].AudioBitrate);
        Assert.AreEqual("4000k", profiles["streaming_live_delivery.txt"].VideoBitrate);
    }

    [TestMethod]
    public void RecoveredStreamingGuides_MapToEditableWorkflowProfiles()
    {
        var resources = typeof(EncodingGuideProfiles).Assembly.GetManifestResourceNames();
        foreach (var source in new[] { "EncodingForStreamingSites.txt", "StreamingGuide.txt", "ffserver.txt" })
        {
            Assert.IsTrue(resources.Contains(
                "FFGrabber.source-guides.dropbox." + source,
                StringComparer.OrdinalIgnoreCase), $"The recovered source {source} must be embedded.");
        }

        var fileProfile = EncodingGuideProfiles.Find("Streaming — File to RTMP (full guides)")
            ?? throw new AssertFailedException("File-to-RTMP profile missing.");
        Assert.AreEqual("-re", fileProfile.InputOptions);
        Assert.AreEqual("flv", fileProfile.OutputFormat);
        Assert.AreEqual("60", fileProfile.GopSize);
        Assert.IsTrue(fileProfile.SourceFile.Contains("StreamingGuide.txt", StringComparison.OrdinalIgnoreCase));

        var liveProfile = EncodingGuideProfiles.Find("Streaming — Live input to RTMP (full guides)")
            ?? throw new AssertFailedException("Live-input-to-RTMP profile missing.");
        Assert.AreEqual("dshow", liveProfile.InputFormat);
        Assert.IsFalse(liveProfile.InputOptions.Contains("-re", StringComparison.OrdinalIgnoreCase),
            "A live device input must not be paced as a prerecorded file.");

        var teeProfile = EncodingGuideProfiles.Find("Streaming — RTMP plus local recording (tee)")
            ?? throw new AssertFailedException("Tee-muxer profile missing.");
        Assert.AreEqual("tee", teeProfile.OutputFormat);
        Assert.IsTrue(teeProfile.OutputOptions.Contains("+global_header", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(53, EncodingGuideProfiles.All.Count);
    }

    [TestMethod]
    public void EmbeddedGuideLibrary_ContainsAllCurrentGuideAndProfileDocuments()
    {
        var resources = typeof(EncodingGuideProfiles).Assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".source-guides.dropbox.", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        string[] preservedReferences =
        [
            "Capture_ALSA.txt", "Capture_Desktop.txt", "Capture_Lightning.txt",
            "Capture_Webcam.txt", "DirectShow.txt", "HWAccelIntro.txt",
            "EncodingForStreamingSites.txt", "StreamingGuide.txt", "ffserver.txt"
        ];

        Assert.IsTrue(resources.Length >= 93,
            "All 79 Dropbox source texts and the reconstructed profile payloads must be embedded.");
        foreach (var source in preservedReferences)
            Assert.IsTrue(resources.Contains(
                "FFGrabber.source-guides.dropbox." + source,
                StringComparer.OrdinalIgnoreCase), $"The full reference {source} must be embedded.");
    }


    [TestMethod]
    public void EmbeddedSourceGuideText_MatchesEveryTrackedTxtFileExactly()
    {
        var repositoryRoot = FindRepositoryRootForGuideSources();
        var sourceDirectory = System.IO.Path.Combine(repositoryRoot, "docs", "source-guides", "dropbox");
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.txt", SearchOption.TopDirectoryOnly);
        Assert.IsTrue(sourceFiles.Length >= 79,
            "The complete set of 79 original Dropbox source texts must be tracked.");

        var assembly = typeof(EncodingGuideProfiles).Assembly;
        foreach (var filePath in sourceFiles)
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            var resourceName = "FFGrabber.source-guides.dropbox." + fileName;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            Assert.IsNotNull(stream, $"Missing embedded resource for {fileName}.");
            using var reader = new StreamReader(stream!);
            var sourceText = File.ReadAllText(filePath);
            var embeddedText = reader.ReadToEnd();
            Assert.AreEqual(sourceText, embeddedText,
                $"Embedded resource {fileName} must match its tracked .txt source character for character.");
        }
    }

    private static string FindRepositoryRootForGuideSources()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            DirectoryInfo? directory = new(start);
            while (directory is not null)
            {
                if (Directory.Exists(System.IO.Path.Combine(directory.FullName, "docs", "source-guides", "dropbox")))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }

        Assert.Fail("Unable to find docs/source-guides/dropbox from the test runner directories.");
        return string.Empty;
    }

    private static EncodingSettings CreateTwoPassSettings(string output, string passLogPrefix) => new(
        InputPath: "input.mp4",
        OutputPath: output,
        Mode: "Convert / encode",
        VideoCodec: "libx264",
        Preset: "slow",
        Crf: 23,
        AudioCodec: "aac",
        AudioBitrate: "128k",
        Overwrite: true,
        RateControl: "Target bitrate",
        VideoBitrate: "2000k",
        OutputSize: "source",
        FrameRate: "source",
        PixelFormat: "yuv420p",
        PreserveMetadata: true,
        FastStart: true,
        TwoPass: true,
        PassLogFile: passLogPrefix);

    private static string? GetOptionValue(IReadOnlyList<string> arguments, string option)
    {
        for (var index = 0; index + 1 < arguments.Count; index++)
        {
            if (string.Equals(arguments[index], option, StringComparison.OrdinalIgnoreCase))
                return arguments[index + 1];
        }

        return null;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "FFGrabber.Tests",
            Guid.NewGuid().ToString("N"));

        public TemporaryDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
