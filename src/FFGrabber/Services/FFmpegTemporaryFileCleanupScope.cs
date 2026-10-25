using System.Text.RegularExpressions;

namespace FFGrabber.Services;

/// <summary>
/// Tracks only output-specific temporary files created while a single FFmpeg job runs.
/// Existing .tmp files are snapshotted and never auto-deleted. This is primarily for
/// HLS/DASH segment and manifest files produced with muxer temporary-write options.
/// </summary>
public sealed class FFmpegTemporaryFileCleanupScope
{
    private static readonly string[] HlsExtensions =
        ["ts", "m4s", "mp4", "m4a", "aac", "vtt", "webvtt", "webm", "cmfv", "cmfa"];

    private readonly DateTime _startedUtc;
    private readonly string _protectedOutputPath;
    private readonly bool _outputExistedAtStart;
    private readonly HashSet<string> _existingAtStart;
    private readonly IReadOnlyList<string> _directories;
    private readonly IReadOnlyList<TemporaryNamePattern> _patterns;

    private FFmpegTemporaryFileCleanupScope(
        string protectedOutputPath,
        IReadOnlyList<string> directories,
        IReadOnlyList<TemporaryNamePattern> patterns)
    {
        _startedUtc = DateTime.UtcNow;
        _protectedOutputPath = Path.GetFullPath(protectedOutputPath);
        _outputExistedAtStart = File.Exists(_protectedOutputPath);
        _directories = directories;
        _patterns = patterns;
        // Snapshot every path that matches the specific output/muxer patterns, including
        // HLS/DASH segments that do not end in .tmp. Existing user files are never cleanup targets.
        _existingAtStart = EnumerateTrackedFiles(directories)
            .Where(MatchesTrackedPattern)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Start tracking the local output and any explicit HLS/DASH segment paths in an
    /// argument vector. Network targets, pipes, and device expressions are ignored.
    /// </summary>
    public static FFmpegTemporaryFileCleanupScope? Begin(
        string outputPath,
        IReadOnlyList<string> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(arguments);

        if (!TryGetLocalPath(outputPath, out var fullOutputPath))
            return null;

        var outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
            return null;

        var outputFileName = Path.GetFileName(fullOutputPath);
        var outputStem = Path.GetFileNameWithoutExtension(outputFileName);
        var outputExtension = Path.GetExtension(outputFileName);
        var outputFormat = GetLastOptionValue(arguments, "-f");
        var hlsFlags = GetLastOptionValue(arguments, "-hls_flags") ?? string.Empty;
        var isHls = outputExtension.Equals(".m3u8", StringComparison.OrdinalIgnoreCase)
            || outputExtension.Equals(".m3u", StringComparison.OrdinalIgnoreCase)
            || string.Equals(outputFormat, "hls", StringComparison.OrdinalIgnoreCase)
            || hlsFlags.Contains("temp_file", StringComparison.OrdinalIgnoreCase);
        var isDash = outputExtension.Equals(".mpd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(outputFormat, "dash", StringComparison.OrdinalIgnoreCase);

        var patterns = new List<TemporaryNamePattern>();
        var outputDirectoryFull = Path.GetFullPath(outputDirectory);

        // Generic atomic output writers may use <output filename>.tmp. Some muxers
        // append stream/segment details to the output basename before .tmp; include only
        // names starting with this output stem and a filename separator, never every .tmp
        // in the directory. Existing matching files are still protected by the start snapshot.
        patterns.Add(TemporaryNamePattern.Exact(outputDirectoryFull, outputFileName + ".tmp"));
        patterns.Add(TemporaryNamePattern.Regex(
            outputDirectoryFull,
            new Regex(
                "^" + Regex.Escape(outputStem) + "(?:[._-][^\\\\/]+)?\\.tmp$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)));

        if (isHls)
        {
            var hlsSegmentPattern = "(?:" + string.Join("|", HlsExtensions.Select(Regex.Escape)) + ")";
            patterns.Add(TemporaryNamePattern.Regex(
                outputDirectoryFull,
                new Regex(
                    "^" + Regex.Escape(outputStem) +
                    "(?:[0-9]+|[_-][0-9]+)[^\\\\/]*\\." + hlsSegmentPattern +
                    "(?:\\.tmp)?$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)));

            patterns.Add(TemporaryNamePattern.Exact(outputDirectoryFull, outputFileName + ".tmp"));
        }

        if (isDash)
        {
            // FFmpeg's default DASH segment templates are init-stream<ID> and
            // chunk-stream<ID>-<number>; these remain output-directory scoped.
            patterns.Add(TemporaryNamePattern.Regex(
                outputDirectoryFull,
                new Regex(
                    "^(?:init-stream|chunk-stream)[A-Za-z0-9_-]*\\.(?:m4s|mp4|webm|m4a|cmfv|cmfa)(?:\\.tmp)?$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)));
            patterns.Add(TemporaryNamePattern.Exact(outputDirectoryFull, outputFileName + ".tmp"));
        }

        // User-specified segment/manifest templates can live outside the manifest directory.
        // Only paths explicitly named by the current command are monitored.
        string[] pathOptions =
        [
            "-hls_segment_filename",
            "-hls_fmp4_init_filename",
            "-hls_master_pl_name",
            "-master_pl_name",
            "-init_seg_name",
            "-media_seg_name",
            "-single_file_name"
        ];

        for (var i = 0; i + 1 < arguments.Count; i++)
        {
            if (!pathOptions.Contains(arguments[i], StringComparer.OrdinalIgnoreCase))
                continue;

            AddTemplatePattern(patterns, arguments[i + 1], outputDirectoryFull);
        }

        var directories = patterns
            .Select(pattern => pattern.DirectoryPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new FFmpegTemporaryFileCleanupScope(fullOutputPath, directories, patterns);
    }

    /// <summary>
    /// Delete output-related .tmp files that did not exist when this job started.
    /// Called only after FFmpeg has exited (including after cancellation).
    /// </summary>
    public TemporaryFileCleanupResult CleanupCreatedFiles()
    {
        var deleted = new List<string>();
        var failures = new List<string>();

        foreach (var path in EnumerateTemporaryFiles(_directories))
        {
            // A user may intentionally name the final output with a .tmp suffix.
            // Never classify the exact configured output path as a cleanup candidate.
            if (string.Equals(path, _protectedOutputPath, StringComparison.OrdinalIgnoreCase)
                || _existingAtStart.Contains(path) || !MatchesTrackedPattern(path))
                continue;

            try
            {
                var lastWrite = File.GetLastWriteTimeUtc(path);
                if (lastWrite < _startedUtc.AddSeconds(-2))
                    continue;

                // An open/locked file is not an orphan. Do not force-delete it.
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }

                File.Delete(path);
                deleted.Add(path);
            }
            catch (FileNotFoundException)
            {
                // FFmpeg may have successfully renamed or deleted the temporary file itself.
            }
            catch (DirectoryNotFoundException)
            {
                // The output directory may have been removed while the job was running.
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failures.Add($"{path}: {ex.Message}");
            }
        }

        return new TemporaryFileCleanupResult(deleted, failures);
    }

    /// <summary>
    /// Cleanup path used after an explicit cancellation. First delete only job-created temp
    /// files inside the existing output-specific scope. Also remove a partial final output
    /// only when this job created it; never delete an output that was present at job start.
    /// </summary>
    public TemporaryFileCleanupResult CleanupAfterCancellation()
    {
        var cleanup = CleanupCreatedFiles();
        var deleted = cleanup.DeletedFiles.ToList();
        var failures = cleanup.FailedFiles.ToList();

        if (!_outputExistedAtStart && File.Exists(_protectedOutputPath))
        {
            try
            {
                using (new FileStream(_protectedOutputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
                File.Delete(_protectedOutputPath);
                deleted.Add(_protectedOutputPath);
            }
            catch (FileNotFoundException)
            {
                // Already removed by FFmpeg.
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failures.Add($"{_protectedOutputPath}: {ex.Message}");
            }
        }

        // HLS/DASH muxers may leave segment files without a .tmp suffix when interrupted.
        // On cancellation only, also remove newly-created artifacts matching an explicit output
        // stem/template. Existing files were snapshotted at job start and are protected.
        foreach (var path in EnumerateTrackedFiles(_directories))
        {
            if (Path.GetExtension(path).Equals(".tmp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, _protectedOutputPath, StringComparison.OrdinalIgnoreCase)
                || _existingAtStart.Contains(path)
                || !MatchesTrackedPattern(path))
                continue;

            try
            {
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
                File.Delete(path);
                deleted.Add(path);
            }
            catch (FileNotFoundException)
            {
                // Already removed by FFmpeg or another cleanup pass.
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failures.Add($"{path}: {ex.Message}");
            }
        }

        return new TemporaryFileCleanupResult(deleted, failures);
    }

    /// <summary>
    /// Find existing output-related temporary files for the explicit user-invoked cleanup
    /// action. The caller must show the list and obtain confirmation before deleting anything.
    /// </summary>
    public static IReadOnlyList<string> FindExistingCandidates(
        string outputPath,
        IReadOnlyList<string> arguments)
    {
        var scope = BeginForListing(outputPath, arguments);
        if (scope is null)
            return Array.Empty<string>();

        return EnumerateTemporaryFiles(scope._directories)
            .Where(path => !string.Equals(path, scope._protectedOutputPath, StringComparison.OrdinalIgnoreCase))
            .Where(scope.MatchesTrackedPattern)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Delete only the exact paths confirmed by the user, and only if they still match the
    /// output-specific patterns when the deletion is performed.
    /// </summary>
    public static TemporaryFileCleanupResult DeleteConfirmedCandidates(
        string outputPath,
        IReadOnlyList<string> arguments,
        IEnumerable<string> confirmedPaths)
    {
        var scope = BeginForListing(outputPath, arguments);
        if (scope is null)
            return new TemporaryFileCleanupResult(Array.Empty<string>(), Array.Empty<string>());

        var deleted = new List<string>();
        var failures = new List<string>();

        foreach (var candidate in confirmedPaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var fullPath = Path.GetFullPath(candidate);
                if (string.Equals(fullPath, scope._protectedOutputPath, StringComparison.OrdinalIgnoreCase)
                    || !scope._directories.Contains(Path.GetDirectoryName(fullPath) ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase)
                    || !fullPath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                    || !scope.MatchesTrackedPattern(fullPath)
                    || !File.Exists(fullPath))
                    continue;

                using (new FileStream(fullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }

                File.Delete(fullPath);
                deleted.Add(fullPath);
            }
            catch (FileNotFoundException)
            {
                // Already removed.
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failures.Add($"{candidate}: {ex.Message}");
            }
        }

        return new TemporaryFileCleanupResult(deleted, failures);
    }

    private static FFmpegTemporaryFileCleanupScope? BeginForListing(
        string outputPath,
        IReadOnlyList<string> arguments)
    {
        var scope = Begin(outputPath, arguments);
        if (scope is not null)
        {
            // For the confirmed cleanup view, existing files are candidates; the runtime
            // cleanup path still uses the captured baseline and will never touch them.
        }
        return scope;
    }

    private bool MatchesTrackedPattern(string path)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileName(path);
        return _patterns.Any(pattern =>
            string.Equals(pattern.DirectoryPath, directory, StringComparison.OrdinalIgnoreCase)
            && pattern.Matches(name));
    }

    private static void AddTemplatePattern(
        ICollection<TemporaryNamePattern> patterns,
        string template,
        string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(template) || !TryGetLocalPath(template, out var resolved, outputDirectory))
            return;

        var directory = Path.GetDirectoryName(resolved);
        var fileName = Path.GetFileName(resolved);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            return;

        var marker = fileName.IndexOfAny(['%', '$']);
        if (marker < 0)
        {
            patterns.Add(TemporaryNamePattern.Exact(Path.GetFullPath(directory), fileName + ".tmp"));
            patterns.Add(TemporaryNamePattern.Exact(Path.GetFullPath(directory), fileName));
            return;
        }

        var prefix = fileName[..marker];
        if (prefix.Length == 0)
            return; // Never scan every .tmp file in a directory for an unbounded template.

        var extension = Path.GetExtension(fileName);
        if (extension.Equals(".$ext$", StringComparison.OrdinalIgnoreCase))
            extension = string.Empty;

        patterns.Add(TemporaryNamePattern.PrefixAndSuffix(
            Path.GetFullPath(directory), prefix, extension + ".tmp"));
        patterns.Add(TemporaryNamePattern.PrefixAndSuffix(
            Path.GetFullPath(directory), prefix, extension));
    }

    private static bool TryGetLocalPath(
        string path,
        out string fullPath,
        string? relativeBase = null)
    {
        fullPath = string.Empty;
        var value = path.Trim().Trim('"');

        if (value.Length == 0 || value == "-" ||
            value.StartsWith("pipe:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("fd:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("udp://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("srt://", StringComparison.OrdinalIgnoreCase))
            return false;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !uri.IsFile)
            return false;

        try
        {
            fullPath = Path.IsPathRooted(value)
                ? Path.GetFullPath(value)
                : Path.GetFullPath(value, relativeBase ?? Environment.CurrentDirectory);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static string? GetLastOptionValue(IReadOnlyList<string> arguments, string name)
    {
        string? value = null;
        for (var i = 0; i + 1 < arguments.Count; i++)
        {
            if (string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
                value = arguments[i + 1];
        }
        return value;
    }

    private static IEnumerable<string> EnumerateTrackedFiles(IEnumerable<string> directories)
    {
        foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(directory))
                continue;

            string[] files;
            try
            {
                files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                string fullPath;
                try { fullPath = Path.GetFullPath(file); }
                catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException) { continue; }
                yield return fullPath;
            }
        }
    }

    private static IEnumerable<string> EnumerateTemporaryFiles(IEnumerable<string> directories)
    {
        foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(directory))
                continue;

            string[] files;
            try
            {
                files = Directory.GetFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                string fullPath;
                try { fullPath = Path.GetFullPath(file); }
                catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException) { continue; }
                yield return fullPath;
            }
        }
    }

    private sealed record TemporaryNamePattern(
        string DirectoryPath,
        string ExactName,
        string Prefix,
        string Suffix,
        Regex? NameRegex)
    {
        public bool Matches(string name)
        {
            if (NameRegex is not null)
                return NameRegex.IsMatch(name);
            if (ExactName.Length > 0)
                return string.Equals(name, ExactName, StringComparison.OrdinalIgnoreCase);
            return name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase)
                && name.Length > Prefix.Length + Suffix.Length;
        }

        public static TemporaryNamePattern Exact(string directory, string name) =>
            new(directory, name, string.Empty, string.Empty, null);

        public static TemporaryNamePattern Regex(string directory, System.Text.RegularExpressions.Regex regex) =>
            new(directory, string.Empty, string.Empty, string.Empty, regex);

        public static TemporaryNamePattern PrefixAndSuffix(string directory, string prefix, string suffix) =>
            new(directory, string.Empty, prefix, suffix, null);
    }
}

public sealed record TemporaryFileCleanupResult(
    IReadOnlyList<string> DeletedFiles,
    IReadOnlyList<string> FailedFiles)
{
    public int DeletedCount => DeletedFiles.Count;
    public int FailedCount => FailedFiles.Count;
}
