using System.Runtime.InteropServices;

namespace FFGrabber.Services;

public static class FFmpegLocator
{
    public static string ArchitectureFolder =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";

    public static string? Find(string tool)
    {
        if (tool is not ("ffmpeg" or "ffprobe" or "ffplay"))
            throw new ArgumentOutOfRangeException(nameof(tool));
        var file = tool + ".exe";
        var root = AppContext.BaseDirectory;
        var paths = new[]
        {
            Path.Combine(root, "tools", "ffmpeg", ArchitectureFolder, "bin", file),
            Path.Combine(root, "tools", "ffmpeg", "bin", file),
            Path.Combine(root, "bin", file),
            Path.Combine(root, file)
        };
        foreach (var candidate in paths)
            if (File.Exists(candidate)) return candidate;
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path)) return null;
        foreach (var folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                var candidate = Path.Combine(folder, file);
                if (File.Exists(candidate)) return candidate;
            }
            catch (ArgumentException) { }
        }
        return null;
    }
}
