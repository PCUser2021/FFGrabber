using FFGrabber;
using FFGrabber.Models;
using FFGrabber.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFGrabber.Tests;

[TestClass]
public sealed class FFmpegCommandBuilderTests
{
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
        Assert.AreEqual(52, EncodingGuideProfiles.All.Count);
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

        Assert.IsTrue(resources.Length >= 23,
            "Six capture/hardware references, two full streaming guides, the archival ffserver guide, and fourteen reconstructed profile payloads must all be embedded.");
        foreach (var source in preservedReferences)
            Assert.IsTrue(resources.Contains(
                "FFGrabber.source-guides.dropbox." + source,
                StringComparer.OrdinalIgnoreCase), $"The full reference {source} must be embedded.");
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
