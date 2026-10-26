using System.Text;

namespace FFGrabber.Services;

/// <summary>
/// Append-only, timestamped session logging. The on-screen log may be abbreviated for responsiveness,
/// but this file is never truncated by the UI and is the source of the exported full log.
/// </summary>
public sealed class SessionLog : IDisposable
{
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
            _writer.WriteLine($"{DateTimeOffset.Now:O}\t{message}");
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
