using System.Diagnostics;
using System.Text;

namespace FFGrabber.Services;

public sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);

public static class FFmpegProcessRunner
{
    public static async Task<ProcessRunResult> RunAsync(string executable, IEnumerable<string> arguments,
        Action<string>? onStandardOutput = null, Action<string>? onStandardError = null,
        CancellationToken cancellationToken = default)
    {
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = info };
        if (!process.Start()) throw new InvalidOperationException($"Unable to start {Path.GetFileName(executable)}.");
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var readOut = ReadLines(process.StandardOutput, stdout, onStandardOutput);
        var readErr = ReadLines(process.StandardError, stderr, onStandardError);
        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        });
        await process.WaitForExitAsync().ConfigureAwait(false);
        await Task.WhenAll(readOut, readErr).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return new ProcessRunResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static async Task ReadLines(StreamReader reader, StringBuilder capture, Action<string>? callback)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            lock (capture)
                if (capture.Length < 1_000_000)
                    capture.AppendLine(line.Length > 1_000_000 - capture.Length ? line[..(1_000_000 - capture.Length)] : line);
            callback?.Invoke(line);
        }
    }
}
