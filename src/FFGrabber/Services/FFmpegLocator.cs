using System.Text.Json;

namespace FFGrabber.Services;

/// <summary>
/// Resolves FFmpeg tools. FFGrabber ships no FFmpeg binaries: the only tool source is the complete
/// ffmpeg.exe / ffprobe.exe / ffplay.exe set that the user imported. Nothing is read from the install
/// folder or from PATH.
/// </summary>
public static class FFmpegLocator
{
    private static readonly object Sync = new();
    private static readonly string ImportSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FFGrabber",
        "ffmpeg-import.json");
    private static string? _importedDirectory = LoadImportedDirectory();

    /// <summary>Directory holding the active user-imported executables, or null when no valid import is active.</summary>
    public static string? ImportedDirectory
    {
        get
        {
            lock (Sync)
            {
                return HasCompleteToolSet(_importedDirectory) ? _importedDirectory : null;
            }
        }
    }

    /// <summary>
    /// Normalizes a user-selected FFmpeg folder. It accepts either a directory containing
    /// all three executables or an extracted FFmpeg root with the executables under bin.
    /// </summary>
    public static string ResolveToolDirectory(string selectedDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedDirectory);
        var fullPath = Path.GetFullPath(selectedDirectory.Trim());
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException("The selected FFmpeg directory does not exist.");

        if (HasCompleteToolSet(fullPath))
            return fullPath;

        var bin = Path.Combine(fullPath, "bin");
        if (HasCompleteToolSet(bin))
            return bin;

        throw new FileNotFoundException(
            "The selected folder must contain ffmpeg.exe, ffprobe.exe, and ffplay.exe, either directly or inside its bin subfolder.");
    }

    public static void SetImportedDirectory(string directory)
    {
        var resolvedDirectory = ResolveToolDirectory(directory);
        var normalized = Path.GetFullPath(resolvedDirectory);
        var json = JsonSerializer.Serialize(new ImportSettings(normalized, DateTimeOffset.UtcNow),
            new JsonSerializerOptions { WriteIndented = true });
        var parent = Path.GetDirectoryName(ImportSettingsPath)!;

        Directory.CreateDirectory(parent);
        var temporaryPath = ImportSettingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, ImportSettingsPath, overwrite: true);
            lock (Sync)
                _importedDirectory = normalized;
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public static string? Find(string tool)
    {
        if (tool is not ("ffmpeg" or "ffprobe" or "ffplay"))
            throw new ArgumentOutOfRangeException(nameof(tool));

        var imported = ImportedDirectory;
        if (imported is null)
            return null;

        var candidate = Path.Combine(imported, tool + ".exe");
        return File.Exists(candidate) ? candidate : null;
    }

    private static bool HasCompleteToolSet(string? directory) =>
        !string.IsNullOrWhiteSpace(directory) &&
        File.Exists(Path.Combine(directory, "ffmpeg.exe")) &&
        File.Exists(Path.Combine(directory, "ffprobe.exe")) &&
        File.Exists(Path.Combine(directory, "ffplay.exe"));

    private static string? LoadImportedDirectory()
    {
        try
        {
            if (!File.Exists(ImportSettingsPath)) return null;
            var settings = JsonSerializer.Deserialize<ImportSettings>(File.ReadAllText(ImportSettingsPath));
            return settings is not null && HasCompleteToolSet(settings.Directory)
                ? Path.GetFullPath(settings.Directory)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return null;
        }
    }

    private sealed record ImportSettings(string Directory, DateTimeOffset ImportedAtUtc);
}
