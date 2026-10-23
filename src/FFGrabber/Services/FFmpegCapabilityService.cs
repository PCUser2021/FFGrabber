using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using FFGrabber.Services;

namespace FFGrabber;

public sealed record FFmpegCapabilityCatalog(
    IReadOnlyList<string> VideoEncoders,
    IReadOnlyList<string> AudioEncoders,
    IReadOnlyList<string> Muxers,
    IReadOnlyList<string> Demuxers);

/// <summary>Discovers options from the installed FFmpeg build to avoid stale hard-coded catalogues.</summary>
public static class FFmpegCapabilityService
{
    private static readonly Regex EncoderRow = new(
        @"^\s*(?<flags>\S{6})\s+(?<name>\S+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FormatRow = new(
        @"^\s*(?<flags>[DE\.]{1,2})\s+(?<name>[^\s]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static async Task<FFmpegCapabilityCatalog> LoadAsync(string executable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        if (!File.Exists(executable))
            throw new FileNotFoundException("The selected FFmpeg executable does not exist.", executable);

        var encoderOutput = await CaptureAsync(executable, ["-hide_banner", "-encoders"]).ConfigureAwait(false);
        var muxerOutput = await CaptureAsync(executable, ["-hide_banner", "-muxers"]).ConfigureAwait(false);
        var demuxerOutput = await CaptureAsync(executable, ["-hide_banner", "-demuxers"]).ConfigureAwait(false);

        var video = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var audio = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in encoderOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var match = EncoderRow.Match(line);
            if (!match.Success)
                continue;

            var flags = match.Groups["flags"].Value;
            var name = match.Groups["name"].Value;
            if (name == "=" || flags.Length != 6)
                continue;

            if (flags[0] == 'V')
                video.Add(name);
            else if (flags[0] == 'A')
                audio.Add(name);
        }

        return new FFmpegCapabilityCatalog(video.ToArray(), audio.ToArray(),
            ParseFormats(muxerOutput, 'E'), ParseFormats(demuxerOutput, 'D'));
    }

    private static IReadOnlyList<string> ParseFormats(string text, char requiredFlag)
    {
        var formats = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var match = FormatRow.Match(line);
            if (!match.Success || !match.Groups["flags"].Value.Contains(requiredFlag))
                continue;

            foreach (var name in match.Groups["name"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (name.Length > 0 && name != "=")
                    formats.Add(name);
        }
        return formats.ToArray();
    }

    private static async Task<string> CaptureAsync(string executable, IReadOnlyList<string> arguments)
    {
        var lines = new ConcurrentQueue<string>();
        var result = await FFmpegProcessRunner.RunAsync(executable, arguments,
            onStandardOutput: lines.Enqueue, onStandardError: lines.Enqueue).ConfigureAwait(false);
        var output = string.Join(Environment.NewLine, lines);
        if (result.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException($"FFmpeg exited with code {result.ExitCode} while reading {arguments[^1]}.");
        return output;
    }
}
