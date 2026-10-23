using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using FFGrabber.Models;

namespace FFGrabber.Services;

/// <summary>Creates a support ZIP containing complete session logs and diagnostics from the installed tools.</summary>
public static class FFmpegLogArchiveService
{
    private static readonly (string FileName, string[] Arguments)[] FFmpegReports =
    [
        ("version.txt", ["-version"]),
        ("build-configuration.txt", ["-buildconf"]),
        ("full-option-help.txt", ["-h", "full"]),
        ("encoders.txt", ["-encoders"]),
        ("decoders.txt", ["-decoders"]),
        ("codecs.txt", ["-codecs"]),
        ("formats.txt", ["-formats"]),
        ("muxers.txt", ["-muxers"]),
        ("demuxers.txt", ["-demuxers"]),
        ("filters.txt", ["-filters"]),
        ("bitstream-filters.txt", ["-bsfs"]),
        ("protocols.txt", ["-protocols"]),
        ("devices.txt", ["-devices"]),
        ("hardware-accelerators.txt", ["-hwaccels"]),
        ("pixel-formats.txt", ["-pix_fmts"]),
        ("sample-formats.txt", ["-sample_fmts"]),
        ("channel-layouts.txt", ["-layouts"])
    ];

    public static async Task SaveZipAsync(
        string zipPath,
        SessionLog sessionLog,
        EncodingSettings settings,
        string queueSnapshot,
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);
        var outputPath = Path.GetFullPath(zipPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        sessionLog.Write($"Log archive export requested: {outputPath}");
        var ffmpeg = FFmpegLocator.Find("ffmpeg");
        var ffprobe = FFmpegLocator.Find("ffprobe");
        var ffplay = FFmpegLocator.Find("ffplay");
        var reportEntries = new List<(string Name, string Content)>();

        var system = new
        {
            GeneratedAtLocal = DateTimeOffset.Now,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Application = Assembly.GetEntryAssembly()?.GetName().Name ?? "FFGrabber",
            ApplicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
            OperatingSystem = RuntimeInformation.OSDescription,
            OperatingSystemArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Framework = RuntimeInformation.FrameworkDescription,
            CurrentDirectory = Environment.CurrentDirectory,
            ApplicationBaseDirectory = AppContext.BaseDirectory,
            Environment64BitProcess = Environment.Is64BitProcess,
            Environment64BitOperatingSystem = Environment.Is64BitOperatingSystem,
            FFmpegPath = ffmpeg,
            FFprobePath = ffprobe,
            FFplayPath = ffplay,
            SessionLogPath = sessionLog.CurrentLogPath,
            LogDirectory = sessionLog.LogDirectory
        };
        reportEntries.Add(("diagnostics/system.json",
            JsonSerializer.Serialize(system, new JsonSerializerOptions { WriteIndented = true })));
        reportEntries.Add(("diagnostics/current-encoding-settings.json",
            JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true })));
        reportEntries.Add(("diagnostics/queue-snapshot.txt", queueSnapshot ?? string.Empty));
        reportEntries.Add(("README-SUPPORT-BUNDLE.txt",
            "FFGrabber support bundle.\r\n\r\n" +
            "Contents include the complete current-session log, available previous FFGrabber session logs, " +
            "current encoding settings, queue snapshot, system/tool paths, and reports returned by the installed " +
            "FFmpeg/ffprobe/ffplay executables. Log data may contain local file paths, command-line options, " +
            "media metadata, and process output. Review the archive before sharing it. Media file contents are not included.\r\n"));

        if (!string.IsNullOrWhiteSpace(ffmpeg))
        {
            foreach (var report in FFmpegReports)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Invoke($"Collecting ffmpeg {report.FileName}");
                reportEntries.Add(($"diagnostics/ffmpeg/{report.FileName}",
                    await RunReportAsync(ffmpeg, report.Arguments, cancellationToken).ConfigureAwait(false)));
            }
        }
        else
        {
            reportEntries.Add(("diagnostics/ffmpeg/tool-not-found.txt",
                "ffmpeg.exe was not found beside FFGrabber or on PATH. The session log still has been preserved."));
        }

        if (!string.IsNullOrWhiteSpace(ffprobe))
            reportEntries.Add(("diagnostics/ffprobe/version.txt",
                await RunReportAsync(ffprobe, ["-version"], cancellationToken).ConfigureAwait(false)));
        else
            reportEntries.Add(("diagnostics/ffprobe/tool-not-found.txt", "ffprobe.exe was not found."));

        if (!string.IsNullOrWhiteSpace(ffplay))
            reportEntries.Add(("diagnostics/ffplay/version.txt",
                await RunReportAsync(ffplay, ["-version"], cancellationToken).ConfigureAwait(false)));
        else
            reportEntries.Add(("diagnostics/ffplay/tool-not-found.txt", "ffplay.exe was not found."));

        progress?.Invoke("Writing ZIP archive");
        await Task.Run(() =>
        {
            using var file = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            using var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: false, Encoding.UTF8);

            var currentEntry = archive.CreateEntry("logs/current-session.log", CompressionLevel.Fastest);
            using (var destination = currentEntry.Open())
                sessionLog.CopyCurrentLogTo(destination);

            foreach (var previousPath in sessionLog.GetLogFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.Equals(Path.GetFullPath(previousPath), Path.GetFullPath(sessionLog.CurrentLogPath), StringComparison.OrdinalIgnoreCase))
                    continue;

                var entry = archive.CreateEntry($"logs/history/{Path.GetFileName(previousPath)}", CompressionLevel.Fastest);
                using var destination = entry.Open();
                using var source = new FileStream(previousPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                source.CopyTo(destination);
            }

            foreach (var report in reportEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = archive.CreateEntry(report.Name, CompressionLevel.Fastest);
                using var stream = entry.Open();
                using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.Write(report.Content);
            }
        }, cancellationToken).ConfigureAwait(false);

        sessionLog.Write($"Log archive exported successfully: {outputPath}; diagnostics files={reportEntries.Count}; includes previous session logs.");
    }

    private static async Task<string> RunReportAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var command = FFmpegCommandBuilder.ToDisplayCommand(executable, arguments);
        try
        {
            // Capture from the streaming callbacks instead of the bounded ProcessRunResult buffers,
            // so the support ZIP contains the complete help/report output even for large -h full output.
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var result = await FFmpegProcessRunner.RunAsync(
                executable,
                arguments,
                onStandardOutput: line => stdout.AppendLine(line),
                onStandardError: line => stderr.AppendLine(line),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var builder = new StringBuilder();
            builder.AppendLine($"Command: {command}");
            builder.AppendLine($"Exit code: {result.ExitCode}");
            if (stdout.Length > 0)
            {
                builder.AppendLine("----- stdout -----");
                builder.Append(stdout);
            }
            if (stderr.Length > 0)
            {
                builder.AppendLine("----- stderr -----");
                builder.Append(stderr);
            }
            return builder.ToString();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"Command: {command}{Environment.NewLine}Failed to collect this report: {ex}";
        }
    }
}
