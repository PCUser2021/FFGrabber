namespace FFGrabber.Models;

public sealed record EncodingSettings(
    string InputPath,
    string OutputPath,
    string Mode,
    string VideoCodec,
    string Preset,
    int Crf,
    string AudioCodec,
    string AudioBitrate,
    bool Overwrite,
    string RateControl,
    string VideoBitrate,
    string OutputSize,
    string FrameRate,
    string PixelFormat,
    bool PreserveMetadata,
    bool FastStart,
    string GlobalOptions = "",
    string InputOptions = "",
    string StreamMappingOptions = "",
    string VideoEncoderOptions = "",
    string AudioEncoderOptions = "",
    string FilterOptions = "",
    string MuxerOptions = "",
    string OutputOptions = "",
    string InputFormat = "auto",
    string OutputFormat = "auto",
    string AdditionalInputs = "");

public sealed class EncodingJob(EncodingSettings settings)
{
    public Guid Id { get; } = Guid.NewGuid();
    public EncodingSettings Settings { get; } = settings;
    public string Status { get; set; } = "Queued";
    public double Progress { get; set; }
    public string Details { get; set; } = "Waiting to start";
}
