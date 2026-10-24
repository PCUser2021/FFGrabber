using FFGrabber;
using FFGrabber.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FFGrabber.Tests;

[TestClass]
public sealed class FFmpegImportServiceTests
{
    private static readonly string[] ExpectedVersionArguments = ["-hide_banner", "-version"];

    [TestMethod]
    public async Task ValidateToolAsync_UsesVersionArguments_AndAcceptsValidBanner()
    {
        using var directory = new TestDirectory();
        var executable = directory.WriteDummyExecutable("ffmpeg.exe");
        const string banner = "ffmpeg version 8.1.2-test_build";

        var executor = new Mock<IFFmpegProcessExecutor>(MockBehavior.Strict);
        executor
            .Setup(process => process.RunAsync(
                executable,
                It.Is<IReadOnlyList<string>>(args => args.SequenceEqual(ExpectedVersionArguments)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessRunResult(0, banner + Environment.NewLine + "configuration: test", string.Empty));

        var version = await FFmpegImportService.ValidateToolAsync(executable, "ffmpeg", executor.Object);

        Assert.AreEqual(banner, version);
        executor.Verify(process => process.RunAsync(
            executable,
            It.Is<IReadOnlyList<string>>(args => args.SequenceEqual(ExpectedVersionArguments)),
            It.IsAny<CancellationToken>()), Times.Once);
        executor.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ValidateToolAsync_RejectsNonZeroExitCode()
    {
        using var directory = new TestDirectory();
        var executable = directory.WriteDummyExecutable("ffprobe.exe");
        var executor = CreateExecutor(executable, new ProcessRunResult(
            2, "ffprobe version 8.1.2-test_build", "fatal: simulated failure"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => FFmpegImportService.ValidateToolAsync(executable, "ffprobe", executor.Object));
    }

    [TestMethod]
    public async Task ValidateToolAsync_RejectsOutputWithoutVersionBanner()
    {
        using var directory = new TestDirectory();
        var executable = directory.WriteDummyExecutable("ffplay.exe");
        var executor = CreateExecutor(executable, new ProcessRunResult(
            0, "unexpected output", string.Empty));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => FFmpegImportService.ValidateToolAsync(executable, "ffplay", executor.Object));
    }

    [TestMethod]
    public async Task ValidateToolAsync_DoesNotInvokeProcessForMissingExecutable()
    {
        using var directory = new TestDirectory();
        var missing = System.IO.Path.Combine(directory.Path, "missing.exe");
        var executor = new Mock<IFFmpegProcessExecutor>(MockBehavior.Strict);

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => FFmpegImportService.ValidateToolAsync(missing, "ffmpeg", executor.Object));

        executor.VerifyNoOtherCalls();
    }

    private static Mock<IFFmpegProcessExecutor> CreateExecutor(string executable, ProcessRunResult result)
    {
        var executor = new Mock<IFFmpegProcessExecutor>(MockBehavior.Strict);
        executor
            .Setup(process => process.RunAsync(
                executable,
                It.Is<IReadOnlyList<string>>(args => args.SequenceEqual(ExpectedVersionArguments)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return executor;
    }

    private sealed class TestDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "FFGrabber.Tests",
            Guid.NewGuid().ToString("N"));

        public TestDirectory() => Directory.CreateDirectory(Path);

        public string WriteDummyExecutable(string name)
        {
            var filePath = System.IO.Path.Combine(Path, name);
            File.WriteAllText(filePath, "Test fixture only; this is deliberately not an executable.");
            return filePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
