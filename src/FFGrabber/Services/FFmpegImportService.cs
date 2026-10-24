using FFGrabber.Services;

namespace FFGrabber;

/// <summary>Validates a user-provided FFmpeg tool set before making it the active persistent tool source.</summary>
public sealed record FFmpegImportResult(
    string Directory,
    string FFmpegVersion,
    string FFprobeVersion,
    string FFplayVersion);

public static class FFmpegImportService
{
    private static readonly IFFmpegProcessExecutor DefaultProcessExecutor = new FFmpegProcessExecutor();

    public static async Task<FFmpegImportResult> ImportAndValidateAsync(
        string selectedDirectory,
        CancellationToken cancellationToken = default)
    {
        var directory = FFmpegLocator.ResolveToolDirectory(selectedDirectory);

        // Verify every executable can actually start and reports a version before changing settings.
        var ffmpeg = await ValidateToolAsync(Path.Combine(directory, FFmpegLocator.GetExecutableFileName("ffmpeg")), "ffmpeg", DefaultProcessExecutor, cancellationToken)
            .ConfigureAwait(false);
        var ffprobe = await ValidateToolAsync(Path.Combine(directory, FFmpegLocator.GetExecutableFileName("ffprobe")), "ffprobe", DefaultProcessExecutor, cancellationToken)
            .ConfigureAwait(false);
        var ffplay = await ValidateToolAsync(Path.Combine(directory, FFmpegLocator.GetExecutableFileName("ffplay")), "ffplay", DefaultProcessExecutor, cancellationToken)
            .ConfigureAwait(false);

        // Persist only after all three tool checks pass, so a failed import cannot break the existing setup.
        FFmpegLocator.SetImportedDirectory(directory);
        return new FFmpegImportResult(directory, ffmpeg, ffprobe, ffplay);
    }

    internal static async Task<string> ValidateToolAsync(
        string executable,
        string toolName,
        IFFmpegProcessExecutor processExecutor,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(processExecutor);

        if (!File.Exists(executable))
            throw new FileNotFoundException($"Required {Path.GetFileName(executable)} was not found.", executable);

        // Keep the import probe behind a seam so tests can exercise success/failure
        // responses without shipping, importing or running native FFmpeg binaries.
        var result = await processExecutor.RunAsync(
            executable,
            ["-hide_banner", "-version"],
            cancellationToken).ConfigureAwait(false);

        var output = string.Join(Environment.NewLine, result.StandardOutput, result.StandardError).Trim();
        var firstLine = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        if (result.ExitCode != 0 || !firstLine.Contains("version", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The selected {Path.GetFileName(executable)} did not pass its version test (exit code {result.ExitCode}). " +
                $"Output: {firstLine}");
        }

        return firstLine;
    }
}
