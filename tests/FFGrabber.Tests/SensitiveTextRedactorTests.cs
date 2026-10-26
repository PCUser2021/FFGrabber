using FFGrabber.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFGrabber.Tests;

[TestClass]
public sealed class SensitiveTextRedactorTests
{
    [TestMethod]
    public void Redact_MasksUrlPassword()
    {
        var result = SensitiveTextRedactor.Redact("-i rtsp://admin:hunter2@192.168.1.5:554/stream");

        Assert.IsFalse(result.Contains("hunter2", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("rtsp://admin:***@192.168.1.5:554/stream", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Redact_MasksUrlUserOnlyToken()
    {
        var result = SensitiveTextRedactor.Redact("https://ghp_secrettoken@example.com/x");

        Assert.IsFalse(result.Contains("ghp_secrettoken", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("https://***@example.com/x", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Redact_MasksSecretQueryValuesButKeepsOtherParameters()
    {
        var result = SensitiveTextRedactor.Redact("srt://host:9000?streamid=live&passphrase=topsecret123&latency=200");

        Assert.IsFalse(result.Contains("topsecret123", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("streamid=live", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("latency=200", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Redact_MasksAuthorizationHeader()
    {
        var result = SensitiveTextRedactor.Redact("-headers \"Authorization: Bearer abc.def.ghi\"");

        Assert.IsFalse(result.Contains("abc.def.ghi", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("Authorization: ***", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Redact_MasksSecretOptionValue()
    {
        var result = SensitiveTextRedactor.Redact("ffmpeg -srt_passphrase \"my pass phrase\" -i in.mp4 out.mp4");

        Assert.IsFalse(result.Contains("my pass phrase", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("-i in.mp4 out.mp4", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Redact_LeavesOrdinaryPathsAndCommandsUnchanged()
    {
        const string text = @"ffmpeg -hide_banner -y -i C:\Videos\my clip.mkv -c:v libx264 -crf 20 D:\out\file.mp4";

        Assert.AreEqual(text, SensitiveTextRedactor.Redact(text));
    }

    [TestMethod]
    public void Redact_HandlesNullAndEmpty()
    {
        Assert.AreEqual(string.Empty, SensitiveTextRedactor.Redact(null));
        Assert.AreEqual(string.Empty, SensitiveTextRedactor.Redact(string.Empty));
    }
}
