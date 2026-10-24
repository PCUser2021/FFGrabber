namespace FFGrabber;

public sealed record CaptureGuideProfile
{
    public required string Name { get; init; }
    public required string SourceFile { get; init; }
    public required string Description { get; init; }
    public string InputFormat { get; init; } = "auto";
    public string InputExpression { get; init; } = "";
    public string FrameRate { get; init; } = "";
    public string VideoSize { get; init; } = "";
    public string PixelFormat { get; init; } = "auto";
    public string StreamCodec { get; init; } = "auto";
    public string RtBufferSize { get; init; } = "";
    public string AudioBufferSize { get; init; } = "";
    public string OffsetX { get; init; } = "";
    public string OffsetY { get; init; } = "";
    public string Channels { get; init; } = "";
    public string SampleRate { get; init; } = "";
    public string VideoDeviceIndex { get; init; } = "";
    public bool ShowRegion { get; init; }
    public bool CopyTimestamps { get; init; }
    public string CaptureFilterSource { get; init; } = "";
    public string InitHardwareDevice { get; init; } = "";
    public string FilterHardwareDevice { get; init; } = "";
    public string VideoCodec { get; init; } = "libx264";
    public string RateControl { get; init; } = "CRF quality";
    public string Crf { get; init; } = "20";
    public string VideoEncoderOptions { get; init; } = "";
}
public static class CaptureGuideProfiles
{
    public static IReadOnlyList<CaptureGuideProfile> All { get; } = Array.AsReadOnly<CaptureGuideProfile>(
    [
        new() { Name = "Windows desktop — GDI (gdigrab)", SourceFile = "Capture_Desktop.txt", Description = "Capture the full Windows desktop with gdigrab at 30 fps. Leave capture size blank to use the desktop's size.", InputFormat = "gdigrab", InputExpression = "desktop", FrameRate = "30" },
        new() { Name = "Windows desktop region — GDI", SourceFile = "Capture_Desktop.txt", Description = "Capture a 1920x1080 region at origin (0,0). Change width, height and offsets for your target display.", InputFormat = "gdigrab", InputExpression = "desktop", FrameRate = "30", VideoSize = "1920x1080", OffsetX = "0", OffsetY = "0", ShowRegion = true },
        new() { Name = "Webcam — DirectShow raw YUYV", SourceFile = "DirectShow.txt; Capture_Webcam.txt", Description = "Request 1280x720 at 30 fps using yuyv422 raw pixels. The camera and requested mode must exist on the target device.", InputFormat = "dshow", InputExpression = "video=Integrated Camera", FrameRate = "30", VideoSize = "1280x720", PixelFormat = "yuyv422" },
        new() { Name = "Webcam — DirectShow MJPEG", SourceFile = "DirectShow.txt", Description = "Request an MJPEG stream; replace the example device name and verify supported sizes/frame rates.", InputFormat = "dshow", InputExpression = "video=Integrated Camera", FrameRate = "30", VideoSize = "1280x720", StreamCodec = "mjpeg" },
        new() { Name = "DirectShow camera + microphone", SourceFile = "DirectShow.txt", Description = "Use one DirectShow expression for camera/microphone capture. Replace example names with exact device names shown by discovery.", InputFormat = "dshow", InputExpression = "video=Integrated Camera:audio=Microphone", FrameRate = "30", VideoSize = "1280x720" },
        new() { Name = "Low-latency DirectShow camera + microphone", SourceFile = "DirectShow.txt", Description = "Lower the audio buffer and enlarge the realtime frame buffer. The device may reject these values; reduce buffers if instability or high memory use occurs.", InputFormat = "dshow", InputExpression = "video=Integrated Camera:audio=Microphone", FrameRate = "30", VideoSize = "1280x720", RtBufferSize = "702000k", AudioBufferSize = "50" },
        new() { Name = "Windows desktop — DDAgrab / D3D11 source", SourceFile = "Capture_Desktop.txt", Description = "Use the ddagrab source filter with D3D11 hardware frames. Requires a compatible recent FFmpeg build and supported Windows display stack.", CaptureFilterSource = "ddagrab=0", InitHardwareDevice = "d3d11va", VideoCodec = "h264_nvenc", RateControl = "Encoder-specific options", VideoEncoderOptions = "-cq:v 20" },
        new() { Name = "Windows Graphics Capture — gfxcapture", SourceFile = "Capture_Desktop.txt", Description = "Use gfxcapture for monitor 1 at 1920x1080. Filter availability depends on the imported FFmpeg package; inspect filter help before running.", CaptureFilterSource = "gfxcapture=monitor_idx=1:width=1920:height=1080:resize_mode=scale_aspect:output_fmt=10bit", InitHardwareDevice = "d3d11va", VideoCodec = "hevc_nvenc", RateControl = "Encoder-specific options", VideoEncoderOptions = "-cq 15" }
    ]);
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(All.Select(profile => profile.Name).ToArray());
    public static CaptureGuideProfile? Find(string? name) => All.FirstOrDefault(profile => string.Equals(profile.Name, name, StringComparison.Ordinal));
}