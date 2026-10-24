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

        Assert.ThrowsException<ArgumentException>(() => FFmpegCommandBuilder.BuildTwoPass(settings));
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
