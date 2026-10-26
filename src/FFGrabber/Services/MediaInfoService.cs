using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FFGrabber.Services;

public sealed record MediaInfoInspection(string ExecutablePath, string Version, string JsonReport);

/// <summary>
/// Manages a user-owned MediaInfo CLI executable. FFGrabber does not silently
/// download or replace executables; the user explicitly selects the official CLI. Automatic
/// discovery is limited to the Program Files install locations.
/// </summary>
public static class MediaInfoService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FFGrabber",
        "mediainfo-import.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly object Sync = new();
    private static string? _importedPath = LoadImportedPath();

    public static string? ImportedPath
    {
        get
        {
            lock (Sync)
                return IsValidPath(_importedPath) ? Path.GetFullPath(_importedPath!) : DiscoverInstalledPath();
        }
    }

    public static async Task<string> ImportAndValidateAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path.Trim());
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The selected MediaInfo CLI executable does not exist.", fullPath);

        var result = await FFmpegProcessRunner.RunAsync(fullPath, ["--Version"], cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var output = string.Join(Environment.NewLine, result.StandardOutput, result.StandardError).Trim();
        if (result.ExitCode != 0 || !output.Contains("MediaInfo", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The selected executable did not identify itself as MediaInfo CLI. " + output);

        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = SettingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath,
                JsonSerializer.Serialize(new ImportSettings(fullPath, DateTimeOffset.UtcNow), JsonOptions),
                cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, SettingsPath, overwrite: true);
            lock (Sync) _importedPath = fullPath;
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }

        return FirstLine(output);
    }

    public static async Task<MediaInfoInspection> InspectAsync(string mediaPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaPath);
        var executable = ImportedPath ?? throw new FileNotFoundException(
            "MediaInfo CLI is not configured. Import MediaInfo.exe from the official MediaArea distribution first.");

        var versionResult = await FFmpegProcessRunner.RunAsync(executable, ["--Version"], cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var versionText = string.Join(Environment.NewLine, versionResult.StandardOutput, versionResult.StandardError).Trim();
        if (versionResult.ExitCode != 0 || !versionText.Contains("MediaInfo", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MediaInfo CLI failed its version check: " + versionText);

        var report = await FFmpegProcessRunner.RunAsync(executable, ["--Output=JSON", mediaPath], cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (report.ExitCode != 0)
            throw new InvalidOperationException("MediaInfo could not inspect the selected file. " + report.StandardError);

        var json = report.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("MediaInfo returned an empty JSON report.");
        // Reject malformed JSON before the report is shown, while preserving the original
        // report text for user-visible copy/export.
        using var _ = JsonDocument.Parse(json);
        return new MediaInfoInspection(executable, FirstLine(versionText), json);
    }

    public static async Task<string?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var executable = ImportedPath;
        if (executable is null) return null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        ProcessRunResult result;
        try
        {
            result = await FFmpegProcessRunner.RunAsync(
                executable, ["--Version"], cancellationToken: timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null; // Never block startup on a stale or hung MediaInfo executable.
        }
        if (result.ExitCode != 0) return null;
        var text = string.Join(Environment.NewLine, result.StandardOutput, result.StandardError);
        var match = Regex.Match(text, @"(?i)MediaInfo(?:Lib)?[^0-9]{0,24}(\d{2,4}\.\d{1,2}(?:\.\d+)?)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string FirstLine(string value) =>
        value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? value;

    private static bool IsValidPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(path);

    private static string? DiscoverInstalledPath()
    {
        var candidates = new List<string>();
        foreach (var root in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        })
        {
            if (!string.IsNullOrWhiteSpace(root))
            {
                candidates.Add(Path.Combine(root, "MediaInfo.exe"));
                candidates.Add(Path.Combine(root, "MediaInfo", "MediaInfo.exe"));
            }
        }

        // Only protected system install locations are probed. PATH, the application folder and the
        // imported FFmpeg folder are intentionally not searched: any of them may be user-writable, so
        // a planted MediaInfo.exe would be executed automatically on startup. Users can still select
        // a MediaInfo.exe explicitly with the import button.
        return candidates.FirstOrDefault(path => File.Exists(path));
    }

    private static string? LoadImportedPath()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return null;
            var settings = JsonSerializer.Deserialize<ImportSettings>(File.ReadAllText(SettingsPath));
            return settings is not null && IsValidPath(settings.Path) ? Path.GetFullPath(settings.Path) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return null;
        }
    }

    private sealed record ImportSettings(string Path, DateTimeOffset ImportedAtUtc);
}
