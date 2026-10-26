using FFGrabber.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFGrabber.Tests;

[TestClass]
public sealed class UpdateCheckServiceTests
{
    [TestMethod]
    [DataRow("https://github.com/PCUser2021/FFGrabber/releases/tag/v0.6.0")]
    [DataRow("https://github.com/pcuser2021/ffgrabber/releases/tag/v0.6.0")]
    [DataRow("https://github.com/MediaArea/MediaInfo/releases/tag/v26.01")]
    public void IsTrustedReleaseUrl_AcceptsOfficialReleasePages(string url) =>
        Assert.IsTrue(UpdateCheckService.IsTrustedReleaseUrl(url));

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("http://github.com/PCUser2021/FFGrabber/releases/tag/v1")]
    [DataRow("file:///C:/Windows/System32/calc.exe")]
    [DataRow(@"C:\Windows\System32\calc.exe")]
    [DataRow("ms-msdt:/id PCDiagnostic")]
    [DataRow("https://github.com.evil.example/PCUser2021/FFGrabber/releases")]
    [DataRow("https://user:pw@github.com/PCUser2021/FFGrabber/releases")]
    [DataRow("https://github.com:8443/PCUser2021/FFGrabber/releases")]
    [DataRow("https://github.com/someone-else/FFGrabber/releases/tag/v1")]
    public void IsTrustedReleaseUrl_RejectsEverythingElse(string? url) =>
        Assert.IsFalse(UpdateCheckService.IsTrustedReleaseUrl(url));
}
