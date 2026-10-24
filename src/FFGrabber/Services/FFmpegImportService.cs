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
    public static async Task<FFmpegImportResult> ImportAndValidateAsync(
        string selectedDirectory,
        CancellationToken cancellationToken = default)
    {
        var directory = FFmpegLocator.ResolveToolDirectory(selectedDirectory);

        // Verify every executable can actually start and reports a version before changing settings.
        var ffmpeg = await ValidateToolAsync(Path.Combine(directory, "ffmpeg.exe"), "ffmpeg", cancellationToken)
            .ConfigureAwait(false);
        var ffprobe = await ValidateToolAsync(Path.Combine(directory, "ffprobe.exe"), "ffprobe", cancellationToken)
            .ConfigureAwait(false);
        var ffplay = await ValidateToolAsync(Path.Combine(directory, "ffplay.exe"), "ffplay", cancellationToken)
            .ConfigureAwait(false);

        // Persist only after all three tool checks pass, so a failed import cannot break the existing setup.
        FFmpegLocator.SetImportedDirectory(directory);
        return new FFmpegImportResult(directory, ffmpeg, ffprobe, ffplay);
    }

    private static async Task<string> ValidateToolAsync(
        string executable,
        string toolName,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(executable))
            throw new FileNotFoundException($"Required {toolName}.exe was not found.", executable);

        var result = await FFmpegProcessRunner.RunAsync(
            executable,
            ["-hide_banner", "-version"],
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var output = string.Join(Environment.NewLine, result.StandardOutput, result.StandardError).Trim();
        var firstLine = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        if (result.ExitCode != 0 || !firstLine.Contains("version", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The selected {toolName}.exe did not pass its version test (exit code {result.ExitCode}). " +
                $"Output: {firstLine}");
        }

        return firstLine;
    }
}
