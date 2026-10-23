using FFGrabber.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FFGrabber.Services;

public static class FFmpegCommandBuilder
{
    public static List<string> Build(EncodingSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.InputPath) || !File.Exists(s.InputPath))
            throw new ArgumentException("Select an existing input media file.");
        if (string.IsNullOrWhiteSpace(s.OutputPath))
            throw new ArgumentException("Specify an output file path.");
        if (string.Equals(Path.GetFullPath(s.InputPath), Path.GetFullPath(s.OutputPath), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Input and output paths must be different.");

        var args = new List<string> { "-hide_banner" };
        if (s.Overwrite) args.Add("-y"); else args.Add("-n");
        args.AddRange(["-i", s.InputPath]);

        switch (s.Mode)
        {
            case "Remux / stream copy":
                args.AddRange(["-map", "0", "-c", "copy"]);
                break;
            case "Extract audio":
                args.Add("-vn");
                args.AddRange(["-c:a", s.AudioCodec]);
                if (s.AudioCodec != "copy" && !string.IsNullOrWhiteSpace(s.AudioBitrate))
                    args.AddRange(["-b:a", s.AudioBitrate]);
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
                    else args.AddRange(["-crf", s.Crf.ToString(CultureInfo.InvariantCulture)]);
                    if (s.OutputSize != "source")
                    {
                        var d = s.OutputSize.Split('x');
                        if (d.Length != 2 || !int.TryParse(d[0], out var w) || !int.TryParse(d[1], out var h))
                            throw new ArgumentException("Output size must be 'source' or WIDTHxHEIGHT.");
                        args.AddRange(["-vf", $"scale={w}:{h}:force_original_aspect_ratio=decrease:force_divisible_by=2,pad={w}:{h}:(ow-iw)/2:(oh-ih)/2"]);
                    }
                    if (s.FrameRate != "source") args.AddRange(["-r", s.FrameRate]);
                    if (s.PixelFormat != "auto") args.AddRange(["-pix_fmt", s.PixelFormat]);
                }
                args.AddRange(["-c:a", s.AudioCodec]);
                if (s.AudioCodec is not ("copy" or "pcm_s16le") && !string.IsNullOrWhiteSpace(s.AudioBitrate))
                    args.AddRange(["-b:a", s.AudioBitrate]);
                break;
        }
        if (s.PreserveMetadata) args.AddRange(["-map_metadata", "0", "-map_chapters", "0"]);
        if (s.FastStart && new[] { ".mp4", ".mov", ".m4a" }.Contains(Path.GetExtension(s.OutputPath), StringComparer.OrdinalIgnoreCase))
            args.AddRange(["-movflags", "+faststart"]);
        args.Add(s.OutputPath);
        return args;
    }

    public static string ToDisplayCommand(string executable, IEnumerable<string> args) =>
        string.Join(" ", new[] { executable }.Concat(args).Select(Quote));

    private static string Quote(string value) =>
        value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : value;
}
