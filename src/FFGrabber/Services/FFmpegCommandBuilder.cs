using FFGrabber.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FFGrabber.Services;

/// <summary>
/// Builds a structured FFmpeg argument vector. Arguments are passed through
/// ProcessStartInfo.ArgumentList; no command is executed through cmd.exe or a shell.
/// </summary>
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
                AppendAudioBitrate(args, s.AudioCodec, s.AudioBitrate);
                break;

            case "Video only":
                args.AddRange(["-an", "-c:v", "copy"]);
                break;

            default:
                args.AddRange(["-c:v", s.VideoCodec]);
                if (s.VideoCodec != "copy")
                {
                    args.AddRange(["-preset", s.Preset]);
                    if (s.RateControl == "Target bitrate")
                    {
                        if (!Regex.IsMatch(s.VideoBitrate.Trim(), @"^\d+(?:\.\d+)?(?:[kKmMgG])?$"))
                            throw new ArgumentException("Enter a valid bitrate such as 8M or 4500k.");
                        args.AddRange(["-b:v", s.VideoBitrate.Trim()]);
                    }
                    else
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
                AppendAudioBitrate(args, s.AudioCodec, s.AudioBitrate);
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

    private static void AppendAudioBitrate(List<string> args, string codec, string bitrate)
    {
        if (codec is "copy" or "flac" or "pcm_s16le" or "alac" or "wavpack" or "ape" or "truehd" or "mlp")
            return;
        if (!string.IsNullOrWhiteSpace(bitrate))
            args.AddRange(["-b:a", bitrate.Trim()]);
    }

    public static string ToDisplayCommand(string executable, IEnumerable<string> args) =>
        string.Join(" ", new[] { executable }.Concat(args).Select(Quote));

    private static string Quote(string value) =>
        value.Length == 0 || value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : value;
}
