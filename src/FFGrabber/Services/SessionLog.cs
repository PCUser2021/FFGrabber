using System.Text;

namespace FFGrabber.Services;

/// <summary>
/// Append-only, timestamped session logging. The on-screen log may be abbreviated for responsiveness,
/// but this file is never truncated by the UI and is the source of the exported full log.
/// </summary>
public sealed class SessionLog : IDisposable
{
    // Keep the log folder bounded: old sessions are pruned when a new session starts.
    private const int MaxRetainedLogFiles = 60;
    private static readonly TimeSpan MaxLogAge = TimeSpan.FromDays(60);

    private readonly object _gate = new();
    private readonly StreamWriter _writer;
    private bool _disposed;

    public string LogDirectory { get; }
    public string CurrentLogPath { get; }

    public SessionLog()
    {
        LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FFGrabber", "Logs");
        Directory.CreateDirectory(LogDirectory);
        PruneOldLogs(LogDirectory);
        CurrentLogPath = Path.Combine(LogDirectory, $"FFGrabber-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log");
        _writer = new StreamWriter(
            new FileStream(CurrentLogPath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite),
            new UTF8Encoding(false))
        {
            AutoFlush = true
        };
        Write("=== FFGrabber session started ===");
        Write($"SessionLog={CurrentLogPath}");
    }

    public void Write(string message)
    {
        lock (_gate)
        {
            if (_disposed) return;
            // Credentials in URLs, headers and options must never reach the on-disk log.
            _writer.WriteLine($"{DateTimeOffset.Now:O}\t{SensitiveTextRedactor.Redact(message)}");
        }
    }

    public IReadOnlyList<string> GetLogFiles()
    {
        lock (_gate)
        {
            if (!Directory.Exists(LogDirectory))
                return Array.Empty<string>();
            return Directory.GetFiles(LogDirectory, "*.log", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public void CopyCurrentLogTo(Stream destination)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                using var oldFile = File.OpenRead(CurrentLogPath);
                oldFile.CopyTo(destination);
                return;
            }

            _writer.Flush();
            using var file = new FileStream(CurrentLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            file.CopyTo(destination);
        }
    }

    private static void PruneOldLogs(string directory)
    {
        try
        {
            var cutoff = DateTime.UtcNow - MaxLogAge;
            var files = new DirectoryInfo(directory)
                .EnumerateFiles("FFGrabber-*.log", SearchOption.TopDirectoryOnly)
                .Where(file => (file.Attributes & FileAttributes.ReparsePoint) == 0)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();

            for (var i = 0; i < files.Count; i++)
            {
                if (i < MaxRetainedLogFiles && files[i].LastWriteTimeUtc >= cutoff)
                    continue;
                try { files[i].Delete(); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Log retention is housekeeping only and must never prevent the application from starting.
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _writer.Flush();
            _writer.Dispose();
            _disposed = true;
        }
    }
}
