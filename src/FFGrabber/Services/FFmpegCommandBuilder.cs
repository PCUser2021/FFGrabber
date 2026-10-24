using FFGrabber.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FFGrabber.Services;

/// <summary>
/// Builds a structured FFmpeg argument vector. Arguments are passed through
/// ProcessStartInfo.ArgumentList; no command is executed through cmd.exe or a shell.
/// </summary>
public sealed record TwoPassEncodingPlan(
    List<string> FirstPassArguments,
    List<string> SecondPassArguments,
    string PassLogPrefix,
    bool IsAutoManagedPassLog);

public static class FFmpegCommandBuilder
{
    private static readonly string[] FastStartExtensions = [".mp4", ".mov", ".m4a"];

    public static List<string> Build(EncodingSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.InputPath))
            throw new ArgumentException("Specify a source path, URL, device expression, pipe, or other FFmpeg input.");
        if (string.IsNullOrWhiteSpace(s.OutputPath))
            throw new ArgumentException("Specify an output path, URL, or other FFmpeg target.");
        if (string.Equals(s.InputPath.Trim(), s.OutputPath.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Input and output locations must be different.");
        if (IsLocalPath(s.InputPath) && IsLocalPath(s.OutputPath) &&
            string.Equals(NormalizeLocalPath(s.InputPath), NormalizeLocalPath(s.OutputPath), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Input and output paths must be different.");

        var args = new List<string> { "-hide_banner" };
        args.AddRange(FFmpegArgumentParser.Parse(s.GlobalOptions));
        args.Add(s.Overwrite ? "-y" : "-n");

        // FFmpeg input/demuxer options must be placed before the corresponding -i.
        args.AddRange(FFmpegArgumentParser.Parse(s.InputOptions));
        if (!string.IsNullOrWhiteSpace(s.InputFormat) && !string.Equals(s.InputFormat, "auto", StringComparison.OrdinalIgnoreCase))
            args.AddRange(["-f", s.InputFormat.Trim()]);
        args.AddRange(["-i", s.InputPath]);

        // Any additional inputs are real argument tokens, not shell text. This lets
        // image-sequence, stereo/multicam, overlay, and separate audio workflows use
        // multiple -i sources while preserving the option ordering supplied by the user.
        args.AddRange(FFmpegArgumentParser.Parse(s.AdditionalInputs));

        var customMapping = FFmpegArgumentParser.Parse(s.StreamMappingOptions);
        if (customMapping.Count > 0)
            args.AddRange(customMapping);

        switch (s.Mode)
        {
            case "Remux / stream copy":
                if (customMapping.Count == 0)
                    args.AddRange(["-map", "0"]);
                args.AddRange(["-c", "copy"]);
                break;

            case "Extract audio":
                args.Add("-vn");
                args.AddRange(["-c:a", s.AudioCodec]);
                AppendAudioBitrate(args, s.AudioCodec, s.AudioBitrate, s.AudioEncoderOptions);
                break;

            case "Video only":
                args.AddRange(["-an", "-c:v", "copy"]);
                break;

            default:
                args.AddRange(["-c:v", s.VideoCodec]);
                if (s.VideoCodec != "copy")
                {
                    if (!string.IsNullOrWhiteSpace(s.Preset)
                        && !string.Equals(s.Preset, "encoder default", StringComparison.OrdinalIgnoreCase)
                        && !HasOption(s.VideoEncoderOptions, "-preset"))
                    {
                        args.AddRange(["-preset", s.Preset]);
                    }

                    var hasExplicitVideoRateControl = HasAnyOption(s.VideoEncoderOptions,
                        "-b:v", "-crf", "-qp", "-qscale:v", "-q:v", "-lossless");

                    if (s.RateControl == "Target bitrate" && !HasOption(s.VideoEncoderOptions, "-b:v"))
                    {
                        if (!Regex.IsMatch(s.VideoBitrate.Trim(), @"^\d+(?:\.\d+)?(?:[kKmMgG])?$"))
                            throw new ArgumentException("Enter a valid bitrate such as 8M or 4500k.");
                        args.AddRange(["-b:v", s.VideoBitrate.Trim()]);
                    }
                    else if (s.RateControl == "CRF quality" && !hasExplicitVideoRateControl)
                    {
                        args.AddRange(["-crf", s.Crf.ToString(CultureInfo.InvariantCulture)]);
                    }

                    if (s.OutputSize != "source")
                    {
                        var dimensions = s.OutputSize.Split('x');
                        if (dimensions.Length != 2 ||
                            !int.TryParse(dimensions[0], out var width) ||
                            !int.TryParse(dimensions[1], out var height) ||
                            width < 2 || height < 2)
                            throw new ArgumentException("Output size must be 'source' or WIDTHxHEIGHT.");

                        args.AddRange(["-vf",
                            $"scale={width}:{height}:force_original_aspect_ratio=decrease:force_divisible_by=2,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2"]);
                    }

                    if (!string.Equals(s.FrameRate, "source", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(s.FrameRate))
                        args.AddRange(["-r", s.FrameRate]);

                    if (!string.Equals(s.PixelFormat, "auto", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(s.PixelFormat))
                        args.AddRange(["-pix_fmt", s.PixelFormat]);
                }

                args.AddRange(["-c:a", s.AudioCodec]);
                AppendAudioBitrate(args, s.AudioCodec, s.AudioBitrate, s.AudioEncoderOptions);
                break;
        }

        // Advanced options are additive and intentionally applied after presets, so a user can
        // override a generated value with the matching FFmpeg flag (for example -preset or -crf).
        args.AddRange(FFmpegArgumentParser.Parse(s.VideoEncoderOptions));
        args.AddRange(FFmpegArgumentParser.Parse(s.AudioEncoderOptions));
        args.AddRange(FFmpegArgumentParser.Parse(s.FilterOptions));

        if (s.PreserveMetadata)
            args.AddRange(["-map_metadata", "0", "-map_chapters", "0"]);

        if (s.FastStart && FastStartExtensions.Contains(Path.GetExtension(s.OutputPath), StringComparer.OrdinalIgnoreCase))
            args.AddRange(["-movflags", "+faststart"]);

        args.AddRange(FFmpegArgumentParser.Parse(s.MuxerOptions));
        args.AddRange(FFmpegArgumentParser.Parse(s.OutputOptions));
        if (!string.IsNullOrWhiteSpace(s.OutputFormat) && !string.Equals(s.OutputFormat, "auto", StringComparison.OrdinalIgnoreCase))
            args.AddRange(["-f", s.OutputFormat.Trim()]);
        args.Add(s.OutputPath);
        return args;
    }

    /// <summary>
    /// Build a standard two-pass plan for encoders that support FFmpeg's generic -pass
    /// interface. Pass 1 writes video analysis to the null muxer with audio disabled;
    /// pass 2 writes the requested output. Encoder-specific pass mechanisms (for example
    /// x265's private pass/stats parameters) must use the searchable manual option editor.
    /// </summary>
    public static TwoPassEncodingPlan BuildTwoPass(EncodingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.TwoPass)
            throw new ArgumentException("Enable the Two-pass video encoding control first.", nameof(settings));
        if (!string.Equals(settings.Mode, "Convert / encode", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Automated two-pass encoding is currently available for Convert / encode jobs only.");
        if (settings.VideoCodec is not ("libx264" or "mpeg4" or "libvpx" or "libvpx-vp9" or "libaom-av1" or "ffv1"))
            throw new ArgumentException(
                $"The selected encoder '{settings.VideoCodec}' is not in FFGrabber's verified generic -pass allow-list. " +
                "For x265 or hardware encoders use the encoder's private pass/stats options in the searchable settings editor.");

        if (HasAnyOption(settings.VideoEncoderOptions, "-pass", "-passlogfile")
            || HasAnyOption(settings.OutputOptions, "-pass", "-passlogfile"))
        {
            throw new ArgumentException(
                "Remove -pass and -passlogfile from the raw option fields when using the Two-pass checkbox; FFGrabber adds both values separately for each pass.");
        }

        var autoManaged = string.IsNullOrWhiteSpace(settings.PassLogFile);
        string passLogPrefix;
        if (autoManaged)
        {
            var passDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FFGrabber", "PassLogs");
            Directory.CreateDirectory(passDirectory);
            var outputStem = Path.GetFileNameWithoutExtension(settings.OutputPath);
            foreach (var invalid in Path.GetInvalidFileNameChars())
                outputStem = outputStem.Replace(invalid, '_');
            if (string.IsNullOrWhiteSpace(outputStem))
                outputStem = "output";
            passLogPrefix = Path.Combine(passDirectory, $"{outputStem}-{Guid.NewGuid():N}");
        }
        else
        {
            passLogPrefix = settings.PassLogFile.Trim();
            if (!Path.IsPathRooted(passLogPrefix))
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(settings.OutputPath))
                    ?? Environment.CurrentDirectory;
                passLogPrefix = Path.GetFullPath(passLogPrefix, outputDirectory);
            }
        }

        var quotedPrefix = "\"" + passLogPrefix.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        var firstPassOutputOptions = CombineOptionLines(settings.OutputOptions, $"-passlogfile {quotedPrefix} -pass 1 -an");
        var secondPassOutputOptions = CombineOptionLines(settings.OutputOptions, $"-passlogfile {quotedPrefix} -pass 2");

        var firstPassSettings = settings with
        {
            OutputPath = "NUL",
            OutputFormat = "null",
            Overwrite = true,
            FastStart = false,
            PreserveMetadata = false,
            OutputOptions = firstPassOutputOptions,
            TwoPass = false,
            PassLogFile = string.Empty
        };
        var secondPassSettings = settings with
        {
            OutputOptions = secondPassOutputOptions,
            TwoPass = false,
            PassLogFile = string.Empty
        };

        return new TwoPassEncodingPlan(
            Build(firstPassSettings),
            Build(secondPassSettings),
            passLogPrefix,
            autoManaged);
    }

    private static string CombineOptionLines(params string?[] values) =>
        string.Join(Environment.NewLine, values.Select(value => value?.Trim()).Where(value => !string.IsNullOrWhiteSpace(value)));

    private static bool IsLocalPath(string value)
    {
        // FFmpeg accepts files, network URLs, pipes, capture devices and protocol-specific sources.
        // Avoid filesystem normalization for non-file URI schemes; pass them as individual arguments.
        return !Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.IsFile;
    }

    private static string NormalizeLocalPath(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.IsFile)
            return Path.GetFullPath(uri.LocalPath);
        return Path.GetFullPath(value);
    }

    private static void AppendAudioBitrate(List<string> args, string codec, string bitrate, string encoderOptions)
    {
        if (codec is "copy" or "flac" or "pcm_s16le" or "alac" or "wavpack" or "ape" or "truehd" or "mlp")
            return;

        // Preserve guide-selected VBR/Qscale modes and explicit bitrates. Never append
        // a second automatic bitrate that silently overrides a chosen quality setting.
        if (HasAnyOption(encoderOptions, "-b:a", "-qscale:a", "-q:a", "-vbr"))
            return;

        if (!string.IsNullOrWhiteSpace(bitrate))
            args.AddRange(["-b:a", bitrate.Trim()]);
    }

    private static bool HasOption(string? optionText, string name) =>
        HasAnyOption(optionText, name);

    private static bool HasAnyOption(string? optionText, params string[] names)
    {
        IReadOnlyList<string> tokens;
        try
        {
            tokens = FFmpegArgumentParser.Parse(optionText);
        }
        catch (ArgumentException)
        {
            // Build will report malformed raw text through its ordinary parser call;
            // do not mask that original validation path here.
            return false;
        }

        return tokens.Any(token => names.Contains(token, StringComparer.OrdinalIgnoreCase));
    }

    public static string ToDisplayCommand(string executable, IEnumerable<string> args) =>
        string.Join(" ", new[] { executable }.Concat(args).Select(Quote));

    private static string Quote(string value) =>
        value.Length == 0 || value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : value;
}
