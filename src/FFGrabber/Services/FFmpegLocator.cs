using System.Text.Json;

namespace FFGrabber.Services;

/// <summary>
/// Resolves FFmpeg tools. FFGrabber ships no FFmpeg binaries: the only tool source is the complete
/// platform-native ffmpeg / ffprobe / ffplay set that the user imported. Nothing is read from the install
/// folder or from PATH.
/// </summary>
public static class FFmpegLocator
{
    private static readonly object Sync = new();

    /// <summary>Returns the platform-native filename for one supported FFmpeg tool.</summary>
    public static string GetExecutableFileName(string tool)
    {
        if (tool is not ("ffmpeg" or "ffprobe" or "ffplay"))
            throw new ArgumentOutOfRangeException(nameof(tool));
        return OperatingSystem.IsWindows() ? tool + ".exe" : tool;
    }

    public static string GetRequiredToolsDescription() =>
        string.Join(", ", new[] { "ffmpeg", "ffprobe", "ffplay" }.Select(GetExecutableFileName));
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

        var requiredTools = GetRequiredToolsDescription();
        throw new FileNotFoundException(
            $"The selected folder must contain {requiredTools}, either directly or inside its bin subfolder.");
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

        var candidate = Path.Combine(imported, GetExecutableFileName(tool));
        return File.Exists(candidate) ? candidate : null;
    }

    private static bool HasCompleteToolSet(string? directory) =>
        !string.IsNullOrWhiteSpace(directory) &&
        File.Exists(Path.Combine(directory, GetExecutableFileName("ffmpeg"))) &&
        File.Exists(Path.Combine(directory, GetExecutableFileName("ffprobe"))) &&
        File.Exists(Path.Combine(directory, GetExecutableFileName("ffplay")));

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
