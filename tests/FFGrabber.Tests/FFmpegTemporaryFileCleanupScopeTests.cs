using FFGrabber.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFGrabber.Tests;

[TestClass]
public sealed class FFmpegTemporaryFileCleanupScopeTests
{
    [TestMethod]
    public void CleanupAfterCancellation_RemovesNewPartialOutputAndItsTemporaryFile()
    {
        using var directory = new TemporaryDirectory();
        var output = System.IO.Path.Combine(directory.Path, "rendered.mkv");
        var temp = output + ".tmp";
        var scope = FFmpegTemporaryFileCleanupScope.Begin(output, ["-hide_banner"]);
        Assert.IsNotNull(scope);

        File.WriteAllText(output, "partial output from the canceled job");
        File.WriteAllText(temp, "temporary muxer data");

        var result = scope!.CleanupAfterCancellation();

        Assert.IsFalse(File.Exists(output), "A partial final output created by the canceled job should be removed.");
        Assert.IsFalse(File.Exists(temp), "The output-scoped temporary file should be removed.");
        Assert.IsTrue(result.DeletedFiles.Contains(output, StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(result.DeletedFiles.Contains(temp, StringComparer.OrdinalIgnoreCase));
        Assert.AreEqual(0, result.FailedCount);
    }

    [TestMethod]
    public void CleanupAfterCancellation_PreservesOutputThatExistedBeforeTheJob()
    {
        using var directory = new TemporaryDirectory();
        var output = System.IO.Path.Combine(directory.Path, "existing.mkv");
        var temp = output + ".tmp";
        File.WriteAllText(output, "existing user data");

        var scope = FFmpegTemporaryFileCleanupScope.Begin(output, ["-hide_banner"]);
        Assert.IsNotNull(scope);
        File.WriteAllText(temp, "temporary muxer data");

        var result = scope!.CleanupAfterCancellation();

        Assert.IsTrue(File.Exists(output), "An output that existed before the job must never be deleted by cancellation cleanup.");
        Assert.AreEqual("existing user data", File.ReadAllText(output));
        Assert.IsFalse(File.Exists(temp), "The newly created output-specific temp file should still be removed.");
        Assert.IsFalse(result.DeletedFiles.Contains(output, StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(result.DeletedFiles.Contains(temp, StringComparer.OrdinalIgnoreCase));
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
