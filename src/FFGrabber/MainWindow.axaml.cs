using System.Text;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FFGrabber.Models;
using FFGrabber.Services;

namespace FFGrabber;

public partial class MainWindow : Window
{
    private readonly SessionLog _sessionLog = new();
    private readonly Button _importFfmpegButton = new() { Content = "Import FFmpeg…" };
    private readonly TextBlock _ffmpegStatus = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Brush("#A4A9B6") };
    private readonly TextBox _input = new() { PlaceholderText = "Local file, URL, device expression, pipe, or FFmpeg input", MinWidth = 420 };
    private readonly TextBox _output = new() { PlaceholderText = "Output file path, URL, or FFmpeg target", MinWidth = 420 };
    private readonly ComboBox _mode = Combo(["Convert / encode", "Remux / stream copy", "Extract audio", "Video only"]);
    private readonly ComboBox _videoCodec = Combo(["libx264", "libx265", "libsvtav1", "libaom-av1", "libvpx-vp9", "libvpx", "ffv1", "prores_ks", "mjpeg", "mpeg4", "libtheora", "h264_nvenc", "av1_amf", "copy"]);
    private readonly ComboBox _inputFormat = Combo(["auto"]);
    private readonly ComboBox _outputFormat = Combo(["auto"]);
    private readonly ComboBox _preset = Combo(["medium", "slow", "fast", "veryfast", "ultrafast", "superfast", "faster", "slower", "veryslow", "placebo", "encoder default", "p1", "p2", "p3", "p4", "p5", "p6", "p7", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13"]);
    private readonly ComboBox _rateControl = Combo(["CRF quality", "Target bitrate", "Encoder-specific options"]);
    private readonly TextBox _crf = new() { Text = "20", Width = 100 };
    private readonly TextBox _videoBitrate = new() { Text = "8M", Width = 100 };
    private readonly ComboBox _size = Combo(["source", "3840x2160", "2560x1440", "2048x1152", "1920x1080", "1280x720", "854x480"]);
    private readonly TextBox _frameRate = new() { Text = "source", Width = 100 };
    private readonly ComboBox _pixelFormat = Combo(["auto", "yuv420p", "yuv420p10le", "yuv422p10le", "yuv444p", "yuv444p10le", "yuvj422p", "yuva420p", "yuva444p10le"]);
    private readonly ComboBox _audioCodec = Combo(["aac", "copy", "libopus", "libmp3lame", "flac", "pcm_s16le"]);
    private readonly TextBox _audioBitrate = new() { Text = "192k", Width = 100 };
    private readonly CheckBox _metadata = new() { Content = "Preserve metadata and chapters", IsChecked = true };
    private readonly CheckBox _fastStart = new() { Content = "MP4/MOV fast start", IsChecked = true };
    private readonly CheckBox _overwrite = new() { Content = "Overwrite output", IsChecked = false };
    private readonly CheckBox _twoPass = new() { Content = "Automated two-pass video encode", IsChecked = false };
    private readonly TextBox _passLogFile = new()
    {
        PlaceholderText = "Pass-log prefix (blank = managed temporary path)",
        MinWidth = 320
    };
    private readonly ComboBox _guideProfile = new()
    {
        ItemsSource = EncodingGuideProfiles.Names.Prepend("Manual / custom").ToArray(),
        SelectedIndex = 0,
        MinWidth = 360
    };
    private readonly TextBlock _guideProfileDescription = new()
    {
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brush("#A4A9B6")
    };
    private HashSet<string> _availableVideoEncoders = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _availableAudioEncoders = new(StringComparer.OrdinalIgnoreCase);

    private readonly TextBox _minVideoBitrate = new() { PlaceholderText = "optional, e.g. 500k", Width = 150 };
    private readonly TextBox _maxVideoBitrate = new() { PlaceholderText = "optional, e.g. 3M", Width = 150 };
    private readonly TextBox _bufferSize = new() { PlaceholderText = "optional, e.g. 6M", Width = 150 };
    private readonly TextBox _gopSize = new() { PlaceholderText = "optional, e.g. 60", Width = 150 };
    private readonly ComboBox _videoTune = Combo(["auto", "film", "animation", "grain", "stillimage", "fastdecode", "zerolatency", "psnr", "ssim"]);
    private readonly TextBox _videoProfile = new() { PlaceholderText = "optional, e.g. high, main, 3", Width = 150 };
    private readonly TextBox _videoQscale = new() { PlaceholderText = "encoder-specific qscale", Width = 150 };
    private readonly TextBox _videoQp = new() { PlaceholderText = "encoder-specific QP", Width = 150 };
    private readonly TextBox _audioQscale = new() { PlaceholderText = "encoder-specific qscale", Width = 150 };
    private readonly TextBox _audioVbr = new() { PlaceholderText = "FDK AAC VBR 1-5", Width = 150 };
    private readonly ComboBox _audioProfile = Combo(["auto", "aac_low", "aac_he", "aac_he_v2"]);
    private readonly TextBox _audioCutoff = new() { PlaceholderText = "cutoff Hz, e.g. 18000", Width = 150 };
    private readonly TextBox _audioSampleRate = new() { PlaceholderText = "Hz, e.g. 44100", Width = 150 };
    private readonly TextBox _audioChannels = new() { PlaceholderText = "e.g. 2", Width = 150 };
    private readonly TextBox _colorPrimaries = new() { PlaceholderText = "e.g. bt2020", Width = 150 };
    private readonly TextBox _colorTransfer = new() { PlaceholderText = "e.g. smpte2084", Width = 150 };
    private readonly TextBox _colorSpace = new() { PlaceholderText = "e.g. bt2020nc", Width = 150 };
    private readonly TextBox _additionalInputs = MultiLine("Optional extra FFmpeg inputs, e.g. -i \"right-eye.exr\" -i audio.m4a", 56);

    private readonly ComboBox _capturePreset = new()
    {
        ItemsSource = CaptureGuideProfiles.Names.Prepend("Manual / custom").ToArray(),
        SelectedIndex = 0,
        MinWidth = 390
    };
    private readonly TextBox _captureFrameRate = new() { PlaceholderText = "e.g. 30 or 30000/1001", Width = 150 };
    private readonly TextBox _captureVideoSize = new() { PlaceholderText = "e.g. 1920x1080", Width = 150 };
    private readonly ComboBox _capturePixelFormat = Combo(["auto", "yuyv422", "nv12", "mjpeg", "bgra", "rgb24", "yuv420p", "p010le"]);
    private readonly ComboBox _captureVideoCodec = Combo(["auto", "rawvideo", "mjpeg", "h264", "hevc"]);
    private readonly TextBox _captureRtBufferSize = new() { PlaceholderText = "e.g. 702000k", Width = 150 };
    private readonly TextBox _captureAudioBufferSize = new() { PlaceholderText = "milliseconds; e.g. 50", Width = 150 };
    private readonly TextBox _captureOffsetX = new() { PlaceholderText = "optional; e.g. 0", Width = 100 };
    private readonly TextBox _captureOffsetY = new() { PlaceholderText = "optional; e.g. 0", Width = 100 };
    private readonly TextBox _captureChannels = new() { PlaceholderText = "ALSA: e.g. 2", Width = 100 };
    private readonly TextBox _captureSampleRate = new() { PlaceholderText = "ALSA: e.g. 44100", Width = 130 };
    private readonly TextBox _captureVideoDeviceIndex = new() { PlaceholderText = "AVFoundation index", Width = 150 };
    private readonly CheckBox _captureShowRegion = new() { Content = "Show capture region (-show_region)" };
    private readonly CheckBox _captureCopyTimestamps = new() { Content = "Preserve input timestamps (-copyts)" };
    private readonly CheckBox _captureBenchmark = new() { Content = "Benchmark (-benchmark)" };
    private readonly CheckBox _disableAutoScale = new() { Content = "Disable FFmpeg automatic output scaling (-noautoscale)" };
    private readonly ComboBox _hwAccel = Combo(["None / disabled", "auto", "cuda", "d3d11va", "dxva2", "qsv", "vaapi", "vdpau", "vulkan", "videotoolbox"]);
    private readonly ComboBox _hwAccelOutputFormat = Combo(["auto", "cuda", "d3d11", "dxva2_vld", "qsv", "vaapi", "vulkan"]);
    private readonly TextBox _hwAccelDevice = new() { PlaceholderText = "device index/name; e.g. 0", Width = 150 };
    private readonly TextBox _initHwDevice = new() { PlaceholderText = "e.g. d3d11va or cuda=gpu:0", Width = 220 };
    private readonly TextBox _filterHwDevice = new() { PlaceholderText = "optional named device", Width = 190 };
    private readonly TextBox _extraHwFrames = new() { PlaceholderText = "e.g. 8", Width = 90 };
    private readonly TextBox _captureFilterSource = MultiLine("Optional source filter graph, e.g. ddagrab=0 or gfxcapture=monitor_idx=1:max_framerate=60. When set, FFGrabber does not add the primary -i input.", 60);

    private readonly ComboBox _cpuBudget = Combo(["Balanced (75%)", "Reduced (50%)", "Maximum (100%)"]);
    private readonly TextBox _globalOptions = MultiLine("-loglevel warning", 56);
    private readonly TextBox _inputOptions = MultiLine("-ss 00:00:05 -probesize 50M -analyzeduration 100M", 56);
    private readonly TextBox _mappingOptions = MultiLine("-map 0:v:0 -map 0:a? -map_metadata 0 -map_chapters 0", 56);
    private readonly TextBox _videoEncoderOptions = MultiLine(@"e.g. -profile:v high -level:v 5.1 -x264-params ""keyint=240:min-keyint=24""", 56);
    private readonly TextBox _audioEncoderOptions = MultiLine("-compression_level 8 -application audio", 56);
    private readonly TextBox _filterOptions = MultiLine(@"e.g. -vf ""scale=1920:-2"" OR -filter_complex ""[0:v]scale=1920:-2[v]""", 60);
    private readonly TextBox _muxerOptions = MultiLine("Optional muxer-specific options, e.g. -cluster_time_limit 1000", 56);
    private readonly TextBox _outputOptions = MultiLine(@"e.g. -shortest -t 00:10:00 -metadata comment=""created with FFGrabber""", 56);

    private readonly TextBox _preview = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, MinHeight = 52 };
    private readonly TextBox _log = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = new FontFamily("Consolas"),
        FontSize = 12,
        MinHeight = 190
    };
    private readonly StringBuilder _displayLog = new();
    private readonly ObservableCollection<string> _queueItems = new();
    private readonly Queue<EncodingSettings> _jobs = new();
    private readonly ListBox _queueView = new() { MinHeight = 90 };
    private readonly Button _startButton = new() { Content = "Start queue", Classes = { "primary" } };
    private readonly Button _cancelButton = new() { Content = "Cancel active job", IsEnabled = false };
    private readonly ProgressBar _conversionProgress = new() { Minimum = 0, Maximum = 100, Value = 0, Height = 18 };
    private readonly TextBlock _progressDescription = new() { Text = "Idle — no active conversion", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _systemUsage = new() { Text = "CPU / memory telemetry is shown while FFmpeg is running.", TextWrapping = TextWrapping.Wrap };
    private CancellationTokenSource? _cancellation;
    private EncodingSettings? _activeJobSettings;
    private Action<int>? _navigateToWorkspace;
    private readonly string[] _startupArguments;
    private int _activeFfmpegProcessId;
    private Task _usageMonitorTask = Task.CompletedTask;

    private static string AppVersion => typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.5.8";

    public MainWindow(string[]? startupArguments = null)
    {
        _startupArguments = startupArguments ?? Array.Empty<string>();
        InitializeComponent();
        try
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://FFGrabber/Assets/Windows11.ico")));
        }
        catch (Exception ex)
        {
            _sessionLog.Write("Could not load embedded FFGrabber icon: " + ex.Message);
        }
        Title = $"FFGrabber v{AppVersion} — Fluent Media Workspace";
        Width = 1260;
        Height = 1000;
        MinWidth = 900;
        MinHeight = 700;

        var root = new Grid { Background = Brush("#FF111319") };
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        root.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(220)));
        root.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        var header = new StackPanel { Spacing = 10, Margin = new Thickness(22, 14, 22, 10) };
        var convertContent = new StackPanel { Spacing = 14, Margin = new Thickness(22) };
        var captureContent = new StackPanel { Spacing = 14, Margin = new Thickness(22) };
        var advancedContent = new StackPanel { Spacing = 14, Margin = new Thickness(22) };
        var activityContent = new StackPanel { Spacing = 14, Margin = new Thickness(22) };
        var hero = new Border { Classes = { "heroPanel" }, Padding = new Thickness(20) };
        var heroContent = new StackPanel { Spacing = 10 };
        var brandRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };
        brandRow.Children.Add(new Border
        {
            Classes = { "brandMark" },
            Width = 48,
            Height = 48,
            CornerRadius = new CornerRadius(12),
            Background = Brush("#084BC0"),
            BorderBrush = Brush("#60E3FF"),
            BorderThickness = new Thickness(1),
            Child = BuildBrandGlyph()
        });
        brandRow.Children.Add(new TextBlock { Text = "FFGrabber", Classes = { "pageTitle" }, VerticalAlignment = VerticalAlignment.Center });
        brandRow.Children.Add(new Border
        {
            Classes = { "tagPill" },
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = "WINDOWS FLUENT  ·  IMPORT-ONLY", FontSize = 10, FontWeight = FontWeight.SemiBold, Foreground = Brush("#2E6FA4") }
        });
        brandRow.Children.Add(new Border
        {
            Classes = { "tagPill" },
            VerticalAlignment = VerticalAlignment.Center,
            Background = Brush("#E8F2FC"),
            BorderBrush = Brush("#0067C0"),
            BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = $"v{AppVersion}", FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = Brush("#0067C0") }
        });
        heroContent.Children.Add(brandRow);
        var capabilityBadges = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        capabilityBadges.Children.Add(new Border
        {
            Classes = { "tagPill" },
            Child = new TextBlock
            {
                Text = $"{EncodingGuideProfiles.All.Count} encoding profiles",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold
            }
        });
        capabilityBadges.Children.Add(new Border
        {
            Classes = { "tagPill" },
            Child = new TextBlock
            {
                Text = $"{CaptureGuideProfiles.All.Count} capture presets",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold
            }
        });
        capabilityBadges.Children.Add(new Border
        {
            Classes = { "tagPill" },
            Child = new TextBlock
            {
                Text = "System / Light / Dark",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold
            }
        });
        heroContent.Children.Add(capabilityBadges);
        heroContent.Children.Add(new TextBlock
        {
            Text = "A theme-aware local workstation for your own FFmpeg toolset. No FFmpeg executables are bundled or downloaded; formats, codecs, and detailed options are queried from the binaries you import.",
            Foreground = Brush("#B7BECC"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13
        });
        hero.Child = heroContent;
        header.Children.Add(hero);

        var topActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        topActions.Children.Add(GlyphButton("\uE713", "Options", async (_, _) => await OpenOptionsAsync()));
        topActions.Children.Add(GlyphButton("\uE946", "About / version", async (_, _) => await OpenAboutAsync()));
        _importFfmpegButton.Content = ButtonContent("\uE8E5", "Import FFmpeg…");
        topActions.Children.Add(_importFfmpegButton);
        _importFfmpegButton.Click += async (_, _) => await ImportFFmpegAsync();
        topActions.Children.Add(Button("MediaInfo…", async (_, _) => await OpenMediaInfoAsync(_input.Text ?? string.Empty)));
        topActions.Children.Add(GlyphButton("\uE71C", "FFmpeg options & capabilities", async (_, _) => await OpenCapabilitiesAsync()));
        topActions.Children.Add(GlyphButton("\uE713", "Encoding settings editor", async (_, _) => await OpenOptionComposerAsync()));
        topActions.Children.Add(GlyphButton("\uE74D", "Clean output .tmp files…", async (_, _) => await CleanTemporaryFilesAsync()));
        header.Children.Add(topActions);
        RefreshFFmpegStatus();
        header.Children.Add(new Border { Classes = { "statusPanel" }, Child = _ffmpegStatus });

        var sourcePanel = Panel("Source and destination");
        var sourceBody = PanelChildren(sourcePanel);
        var inputRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        inputRow.Children.Add(_input);
        inputRow.Children.Add(GlyphButton("\uE8E5", "Browse", async (_, _) => await BrowseInputAsync()));
        sourceBody.Children.Add(inputRow);

        var outputRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        outputRow.Children.Add(_output);
        outputRow.Children.Add(GlyphButton("\uE8B7", "Choose folder", async (_, _) => await BrowseOutputAsync()));
        sourceBody.Children.Add(outputRow);
        convertContent.Children.Add(sourcePanel);

        var settingsPanel = Panel("Encoding settings");
        var settings = PanelChildren(settingsPanel);

        var guideProfileRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        guideProfileRow.Children.Add(Label("Encoding guide profile", 150));
        guideProfileRow.Children.Add(_guideProfile);
        guideProfileRow.Children.Add(Button("Apply profile", (_, _) => ApplyGuideProfile(), primary: true));
        settings.Children.Add(guideProfileRow);
        settings.Children.Add(_guideProfileDescription);

        settings.Children.Add(Row("Operation", _mode));
        settings.Children.Add(Row("Video codec / encoder", _videoCodec));
        settings.Children.Add(Row("Encoder preset", _preset));
        settings.Children.Add(Row("Rate control", _rateControl));
        settings.Children.Add(Row("CPU thread budget", _cpuBudget));
        settings.Children.Add(new TextBlock
        {
            Text = "Balanced and Reduced cap software encoder/filter thread concurrency to lower peak CPU use. This is a best-effort thread budget, not a hard OS CPU quota; hardware encoders may ignore it.",
            Foreground = Brush("#A4A9B6"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        });

        var quality = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        quality.Children.Add(Label("CRF", 80));
        quality.Children.Add(_crf);
        quality.Children.Add(Label("Video bitrate", 110));
        quality.Children.Add(_videoBitrate);
        settings.Children.Add(quality);

        settings.Children.Add(Row("Output resolution", _size));
        var frame = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        frame.Children.Add(Label("Frame rate", 80));
        frame.Children.Add(_frameRate);
        frame.Children.Add(Label("Pixel format", 110));
        frame.Children.Add(_pixelFormat);
        settings.Children.Add(frame);
        settings.Children.Add(Row("Audio codec / encoder", _audioCodec));
        settings.Children.Add(Row("Audio bitrate", _audioBitrate));
        settings.Children.Add(Row("Input demuxer / format (-f)", _inputFormat));
        settings.Children.Add(Row("Output muxer / container (-f)", _outputFormat));

        var checks = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18 };
        checks.Children.Add(_metadata);
        checks.Children.Add(_fastStart);
        checks.Children.Add(_overwrite);
        settings.Children.Add(checks);

        var guideControls = new StackPanel { Spacing = 10 };
        guideControls.Children.Add(new TextBlock
        {
            Text = "Codec-specific controls extracted from the encoding guides. Blank fields leave the encoder defaults unchanged.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("#A4A9B6")
        });
        var videoLimits = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        videoLimits.Children.Add(Label("Min bitrate", 85)); videoLimits.Children.Add(_minVideoBitrate);
        videoLimits.Children.Add(Label("Max bitrate", 85)); videoLimits.Children.Add(_maxVideoBitrate);
        guideControls.Children.Add(videoLimits);
        var videoBufferGop = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        videoBufferGop.Children.Add(Label("VBV buffer", 85)); videoBufferGop.Children.Add(_bufferSize);
        videoBufferGop.Children.Add(Label("GOP / keyframe", 105)); videoBufferGop.Children.Add(_gopSize);
        guideControls.Children.Add(videoBufferGop);
        var videoTuneProfile = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        videoTuneProfile.Children.Add(Label("Video tune", 85)); videoTuneProfile.Children.Add(_videoTune);
        videoTuneProfile.Children.Add(Label("Video profile", 105)); videoTuneProfile.Children.Add(_videoProfile);
        guideControls.Children.Add(videoTuneProfile);
        var videoQuantizers = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        videoQuantizers.Children.Add(Label("Video qscale", 85)); videoQuantizers.Children.Add(_videoQscale);
        videoQuantizers.Children.Add(Label("Video QP", 105)); videoQuantizers.Children.Add(_videoQp);
        guideControls.Children.Add(videoQuantizers);

        var audioRate = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        audioRate.Children.Add(Label("Audio qscale", 85)); audioRate.Children.Add(_audioQscale);
        audioRate.Children.Add(Label("AAC VBR", 105)); audioRate.Children.Add(_audioVbr);
        guideControls.Children.Add(audioRate);
        var audioProfileRate = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        audioProfileRate.Children.Add(Label("Audio profile", 85)); audioProfileRate.Children.Add(_audioProfile);
        audioProfileRate.Children.Add(Label("Audio cutoff", 105)); audioProfileRate.Children.Add(_audioCutoff);
        guideControls.Children.Add(audioProfileRate);
        var audioLayout = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        audioLayout.Children.Add(Label("Sample rate", 85)); audioLayout.Children.Add(_audioSampleRate);
        audioLayout.Children.Add(Label("Channels", 105)); audioLayout.Children.Add(_audioChannels);
        guideControls.Children.Add(audioLayout);

        var colorRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        colorRow.Children.Add(Label("Color primaries", 100)); colorRow.Children.Add(_colorPrimaries);
        colorRow.Children.Add(Label("Transfer", 70)); colorRow.Children.Add(_colorTransfer);
        colorRow.Children.Add(Label("Colorspace", 85)); colorRow.Children.Add(_colorSpace);
        guideControls.Children.Add(colorRow);

        var passRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        passRow.Children.Add(_twoPass);
        passRow.Children.Add(Label("Pass-log prefix", 105));
        passRow.Children.Add(_passLogFile);
        guideControls.Children.Add(passRow);

        guideControls.Children.Add(new TextBlock
        {
            Text = "Two-pass mode is available for documented generic-pass encoders (libx264, MPEG-4, libvpx/VP9, libaom-av1, FFV1). FFGrabber runs pass 1 to the null muxer, then pass 2 to the selected output. Leave the pass-log prefix blank to use and clean a unique FFGrabber-managed path.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("#A4A9B6")
        });

        settings.Children.Add(new Expander
        {
            Header = "Guide-specific controls — VBV, keyframes, encoder tune/profile, audio quality and HDR metadata",
            IsExpanded = true,
            Content = guideControls
        });

        convertContent.Children.Add(settingsPanel);
        captureContent.Children.Add(BuildCaptureAndHardwarePanel());

        advancedContent.Children.Add(BuildAdvancedOptions());

        advancedContent.Children.Add(new TextBlock
        {
            Text = "Advanced values are passed to ffmpeg.exe as process arguments (not through a shell). They are placed in the input, encoder/filter, muxer and output positions shown below. Duplicate options are allowed so later values can override generated defaults where FFmpeg's option semantics permit.",
            Foreground = Brush("#A4A9B6"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        });

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(GlyphButton("\uE8A5", "Preview command", PreviewCommand));
        actions.Children.Add(GlyphButton("\uE710", "Add to queue", AddToQueue, primary: true));
        _startButton.Content = ButtonContent("\uE768", "Start queue");
        _startButton.Click += async (_, _) => await RunQueueAsync();
        actions.Children.Add(_startButton);
        _cancelButton.Content = ButtonContent("\uE711", "Cancel active job");
        _cancelButton.Click += (_, _) => CancelActiveJob();
        actions.Children.Add(_cancelButton);
        actions.Children.Add(GlyphButton("\uE946", "Inspect with ffprobe", async (_, _) => await ProbeAsync()));
        actions.Children.Add(GlyphButton("\uE768", "Play with ffplay", Play));
        actions.Children.Add(Button("MediaInfo…", async (_, _) => await OpenMediaInfoAsync(_input.Text ?? string.Empty)));
        convertContent.Children.Add(actions);

        convertContent.Children.Add(Heading("Live conversion progress and system usage"));
        convertContent.Children.Add(_conversionProgress);
        convertContent.Children.Add(_progressDescription);
        convertContent.Children.Add(_systemUsage);

        convertContent.Children.Add(Heading("Command preview"));
        convertContent.Children.Add(_preview);
        activityContent.Children.Add(Heading("Queue"));
        _queueView.ItemsSource = _queueItems;
        activityContent.Children.Add(_queueView);
        activityContent.Children.Add(Heading("Live log preview (complete log is retained separately)"));
        activityContent.Children.Add(_log);
        var convertPage = new ScrollViewer { Content = convertContent };
        var capturePage = new ScrollViewer { Content = captureContent };
        var advancedPage = new ScrollViewer { Content = advancedContent };
        var activityPage = new ScrollViewer { Content = activityContent };
        var mainContent = new ContentControl { Content = convertPage };
        var pages = new Control[] { convertPage, capturePage, advancedPage, activityPage };
        var navigationButtons = new List<Button>();

        void NavigateTo(int index)
        {
            mainContent.Content = pages[index];
            foreach (var item in navigationButtons)
                item.Classes.Remove("selected");
            if (index >= 0 && index < navigationButtons.Count)
                navigationButtons[index].Classes.Add("selected");
        }

        _navigateToWorkspace = NavigateTo;

        var navigation = new StackPanel { Spacing = 6, Margin = new Thickness(12, 18, 12, 18) };
        navigation.Children.Add(new TextBlock
        {
            Text = "WORKSPACE",
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#A4A9B6"),
            Margin = new Thickness(10, 4, 0, 6)
        });

        Button AddNavigationItem(string glyph, string label, int index)
        {
            var item = NavigationButton(glyph, label, (_, _) => NavigateTo(index));
            navigationButtons.Add(item);
            navigation.Children.Add(item);
            return item;
        }

        AddNavigationItem("\uE8A5", "Convert", 0).Classes.Add("selected");
        AddNavigationItem("\uE8B7", "Capture & hardware", 1);
        AddNavigationItem("\uE713", "Advanced options", 2);
        AddNavigationItem("\uE768", "Queue & activity", 3);
        navigation.Children.Add(NavigationButton("\uE8A5", "Source guide library", async (_, _) => await OpenGuideLibraryAsync()));

        navigation.Children.Add(new Border
        {
            Height = 1,
            Background = Brush("#A4A9B6"),
            Margin = new Thickness(8, 12, 8, 10)
        });
        navigation.Children.Add(new TextBlock
        {
            Text = "APPEARANCE",
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#A4A9B6"),
            Margin = new Thickness(10, 0, 0, 3)
        });
        var themePicker = new ComboBox
        {
            ItemsSource = new[] { "System", "Light", "Dark" },
            SelectedItem = ThemePreferences.GetSavedChoice(),
            MinWidth = 150,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(4, 0, 4, 0)
        };
        themePicker.SelectionChanged += (_, _) =>
        {
            var choice = themePicker.SelectedItem?.ToString() ?? "Light";
            ThemePreferences.ApplyChoice(choice);
        };
        navigation.Children.Add(themePicker);
        navigation.Children.Add(new TextBlock
        {
            Text = "FFGrabber uses your imported FFmpeg toolset. No FFmpeg executables are bundled or downloaded.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Foreground = Brush("#A4A9B6"),
            Margin = new Thickness(10, 12, 10, 0)
        });

        var sidebar = new Border
        {
            Classes = { "sidebarShell" },
            Child = new ScrollViewer { Content = navigation }
        };
        Grid.SetRow(header, 0);
        Grid.SetColumnSpan(header, 2);
        root.Children.Add(header);
        Grid.SetRow(sidebar, 1);
        Grid.SetColumn(sidebar, 0);
        root.Children.Add(sidebar);
        Grid.SetRow(mainContent, 1);
        Grid.SetColumn(mainContent, 1);
        root.Children.Add(mainContent);
        Content = root;

        _sessionLog.Write($"FFGrabber main window initialized; OS={RuntimeInformation.OSDescription}; architecture={RuntimeInformation.ProcessArchitecture}; appVersion={AppVersion}");
        _sessionLog.Write($"Tool paths: ffmpeg={FFmpegLocator.Find("ffmpeg") ?? "(not found)"}; ffprobe={FFmpegLocator.Find("ffprobe") ?? "(not found)"}; ffplay={FFmpegLocator.Find("ffplay") ?? "(not found)"}");
        AppendDisplayLog("Session log location: " + _sessionLog.CurrentLogPath);
        Opened += async (_, _) =>
        {
            await LoadAvailableCapabilitiesAsync();
            if (_startupArguments.Length > 0)
                await HandleStartupArgumentsAsync(_startupArguments);
            _ = CheckForUpdatesOnStartupAsync();
        };
        Closed += (_, _) =>
        {
            // Do not leave an FFmpeg encode running unattended after the window is closed.
            try { _cancellation?.Cancel(); }
            catch (ObjectDisposedException) { }
            _sessionLog.Dispose();
        };
    }

    private static ComboBox Combo(string[] values) => new() { ItemsSource = values, SelectedIndex = 0, MinWidth = 180 };

    private static void SetComboValue(ComboBox combo, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var choices = (combo.ItemsSource as IEnumerable<string> ?? Array.Empty<string>()).ToList();
        if (!choices.Contains(value, StringComparer.OrdinalIgnoreCase))
            choices.Add(value);

        combo.ItemsSource = choices;
        combo.SelectedItem = choices.First(choice =>
            string.Equals(choice, value, StringComparison.OrdinalIgnoreCase));
    }

    private static string CombineOptionLines(params string?[] values) =>
        string.Join(Environment.NewLine, values
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string BuildOption(string name, string? value) =>
        string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "auto", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : name + " " + value.Trim();

    private string BuildVideoGuideOptions() => CombineOptionLines(
        BuildOption("-minrate", _minVideoBitrate.Text),
        BuildOption("-maxrate", _maxVideoBitrate.Text),
        BuildOption("-bufsize", _bufferSize.Text),
        BuildOption("-g", _gopSize.Text),
        Selected(_videoTune) is "auto" or "" ? string.Empty : "-tune " + Selected(_videoTune),
        BuildOption("-profile:v", _videoProfile.Text),
        BuildOption("-qscale:v", _videoQscale.Text),
        BuildOption("-qp", _videoQp.Text));

    private string BuildAudioGuideOptions() => CombineOptionLines(
        BuildOption("-qscale:a", _audioQscale.Text),
        BuildOption("-vbr", _audioVbr.Text),
        Selected(_audioProfile) is "auto" or "" ? string.Empty : "-profile:a " + Selected(_audioProfile),
        BuildOption("-cutoff", _audioCutoff.Text),
        BuildOption("-ar", _audioSampleRate.Text),
        BuildOption("-ac", _audioChannels.Text));

    private string BuildColorMetadataOptions() => CombineOptionLines(
        BuildOption("-color_primaries", _colorPrimaries.Text),
        BuildOption("-color_trc", _colorTransfer.Text),
        BuildOption("-colorspace", _colorSpace.Text));
    private static IBrush Brush(string color) => ThemePreferences.Brush(color);
    private static TextBlock Heading(string text) => new() { Text = text, Foreground = Brush("#C2F970"), FontWeight = FontWeight.SemiBold, FontSize = 15 };
    private static TextBlock Label(string text, double width) => new() { Text = text, Width = width, VerticalAlignment = VerticalAlignment.Center, Foreground = Brush("#C9CED9") };

    private static TextBox MultiLine(string placeholder, double height) => new()
    {
        PlaceholderText = placeholder,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = new FontFamily("Consolas"),
        FontSize = 12,
        MinHeight = height,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
    };

    private static Border Panel(string heading)
    {
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(new TextBlock { Text = heading, Foreground = Brush("#F1F3F8"), FontWeight = FontWeight.SemiBold, FontSize = 15 });
        return new Border { Classes = { "panel" }, Child = stack };
    }

    private static StackPanel PanelChildren(Border panel) => (StackPanel)panel.Child!;

    private static StackPanel Row(string label, Control control)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(Label(label, 150));
        row.Children.Add(control);
        return row;
    }

    private static Control BuildBrandGlyph(double size = 32)
    {
        // Consistent Windows 11 four-pane mark, matching the supplied logo reference.
        var grid = new Grid { Width = size, Height = size };
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        for (var row = 0; row < 2; row++)
        {
            for (var column = 0; column < 2; column++)
            {
                var pane = new Border
                {
                    Background = Brush("#0078D4"),
                    CornerRadius = new CornerRadius(Math.Max(0.5, size / 32)),
                    Margin = new Thickness(0, 0, size * 0.055, size * 0.055)
                };
                Grid.SetRow(pane, row);
                Grid.SetColumn(pane, column);
                grid.Children.Add(pane);
            }
        }
        return grid;
    }

    private static Control ButtonContent(string glyph, string label)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(BuildBrandGlyph(16));
        content.Children.Add(new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        });
        return content;
    }

    private static Button NavigationButton(string glyph, string label, EventHandler<RoutedEventArgs> handler)
    {
        var button = new Button { Content = ButtonContent(glyph, label) };
        button.Classes.Add("navButton");
        button.Click += handler;
        return button;
    }

    private static Button GlyphButton(string glyph, string label, EventHandler<RoutedEventArgs> handler, bool primary = false)
    {
        var button = new Button { Content = ButtonContent(glyph, label) };
        if (primary) button.Classes.Add("primary");
        button.Click += handler;
        return button;
    }

    private static Button Button(string text, EventHandler<RoutedEventArgs> handler, bool primary = false)
    {
        var button = new Button { Content = text };
        if (primary) button.Classes.Add("primary");
        button.Click += handler;
        return button;
    }

    private Expander BuildCaptureAndHardwarePanel()
    {
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(new TextBlock
        {
            Text = "Capture and acceleration settings extracted from the DirectShow, desktop/webcam capture and hardware-acceleration references. The selected FFmpeg build and device must support the chosen settings; device names, size, frame rate and pixel formats vary by hardware.",
            TextWrapping = TextWrapping.Wrap, Foreground = Brush("#A4A9B6")
        });
        var profileRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        profileRow.Children.Add(Label("Capture guide profile", 150));
        profileRow.Children.Add(_capturePreset);
        profileRow.Children.Add(GlyphButton("\uE768", "Apply capture profile", (_, _) => ApplyCapturePreset(), primary: true));
        profileRow.Children.Add(GlyphButton("\uE8A5", "List DirectShow devices", async (_, _) => await ListDirectShowDevicesAsync()));
        stack.Children.Add(profileRow);

        var inputRateSize = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        inputRateSize.Children.Add(Label("Capture frame rate (-framerate)", 165));
        inputRateSize.Children.Add(_captureFrameRate);
        inputRateSize.Children.Add(Label("Capture size (-video_size)", 155));
        inputRateSize.Children.Add(_captureVideoSize);
        stack.Children.Add(inputRateSize);

        var inputFormatCodec = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        inputFormatCodec.Children.Add(Label("Input pixel format", 150));
        inputFormatCodec.Children.Add(_capturePixelFormat);
        inputFormatCodec.Children.Add(Label("Capture stream codec", 150));
        inputFormatCodec.Children.Add(_captureVideoCodec);
        stack.Children.Add(inputFormatCodec);

        var inputBufferRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        inputBufferRow.Children.Add(Label("Realtime buffer (-rtbufsize)", 165));
        inputBufferRow.Children.Add(_captureRtBufferSize);
        inputBufferRow.Children.Add(Label("Audio buffer (ms)", 115));
        inputBufferRow.Children.Add(_captureAudioBufferSize);
        stack.Children.Add(inputBufferRow);

        var cropRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        cropRow.Children.Add(Label("Desktop offset X", 120));
        cropRow.Children.Add(_captureOffsetX);
        cropRow.Children.Add(Label("Offset Y", 65));
        cropRow.Children.Add(_captureOffsetY);
        cropRow.Children.Add(Label("Device index", 90));
        cropRow.Children.Add(_captureVideoDeviceIndex);
        stack.Children.Add(cropRow);

        var audioRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        audioRow.Children.Add(Label("Capture channels (-channels)", 170));
        audioRow.Children.Add(_captureChannels);
        audioRow.Children.Add(Label("Capture rate (-sample_rate)", 170));
        audioRow.Children.Add(_captureSampleRate);
        stack.Children.Add(audioRow);

        var captureChecks = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18, VerticalAlignment = VerticalAlignment.Center };
        captureChecks.Children.Add(_captureShowRegion);
        captureChecks.Children.Add(_captureCopyTimestamps);
        captureChecks.Children.Add(_captureBenchmark);
        captureChecks.Children.Add(_disableAutoScale);
        stack.Children.Add(captureChecks);

        stack.Children.Add(Heading("Hardware acceleration / hardware frames"));
        var hwRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        hwRow.Children.Add(Label("Decoder hardware API (-hwaccel)", 180));
        hwRow.Children.Add(_hwAccel);
        hwRow.Children.Add(Label("Hardware frame format", 140));
        hwRow.Children.Add(_hwAccelOutputFormat);
        stack.Children.Add(hwRow);

        var hwDeviceRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        hwDeviceRow.Children.Add(Label("Decode device (-hwaccel_device)", 180));
        hwDeviceRow.Children.Add(_hwAccelDevice);
        hwDeviceRow.Children.Add(Label("Extra frames", 90));
        hwDeviceRow.Children.Add(_extraHwFrames);
        stack.Children.Add(hwDeviceRow);
        stack.Children.Add(Row("Initialize hardware device", _initHwDevice));
        stack.Children.Add(Row("Filter hardware device", _filterHwDevice));
        stack.Children.Add(new TextBlock
        {
            Text = "For DDAgrab/gfxcapture, use a source filter below instead of a file input. The historical Capture_Lightning.txt example uses a non-existent FFmpeg -delay option and an external buffering tool; that unsupported flag is intentionally reference-only.",
            TextWrapping = TextWrapping.Wrap, Foreground = Brush("#A4A9B6")
        });
        AddOptionEditor(stack, "Capture source filter (no primary -i; e.g. ddagrab=0 or gfxcapture=monitor_idx=1:max_framerate=60)", _captureFilterSource);
        return new Expander
        {
            Header = "Capture & hardware acceleration — DirectShow, desktop/webcam, buffering, CUDA/D3D11/QSV/Vulkan",
            IsExpanded = false, Content = stack
        };
    }

    private void ApplyCapturePreset()
    {
        var profile = CaptureGuideProfiles.Find(Selected(_capturePreset));
        if (profile is null)
        {
            _guideProfileDescription.Text = "Manual / custom capture mode. Select a source profile to populate the input, capture and encoder settings.";
            return;
        }
        // Applying a profile intentionally replaces its related input/capture/encoder baseline.
        _input.Text = profile.InputExpression;
        SetComboValue(_inputFormat, profile.InputFormat);
        SetComboValue(_videoCodec, profile.VideoCodec);
        SetComboValue(_rateControl, profile.RateControl);
        SetComboValue(_preset, profile.RateControl == "Encoder-specific options" ? "encoder default" : "medium");
        SetComboValue(_capturePixelFormat, profile.PixelFormat);
        SetComboValue(_captureVideoCodec, profile.StreamCodec);
        SetComboValue(_hwAccel, "None / disabled");
        SetComboValue(_hwAccelOutputFormat, "auto");
        _captureFrameRate.Text = profile.FrameRate;
        _captureVideoSize.Text = profile.VideoSize;
        _captureRtBufferSize.Text = profile.RtBufferSize;
        _captureAudioBufferSize.Text = profile.AudioBufferSize;
        _captureOffsetX.Text = profile.OffsetX;
        _captureOffsetY.Text = profile.OffsetY;
        _captureChannels.Text = profile.Channels;
        _captureSampleRate.Text = profile.SampleRate;
        _captureVideoDeviceIndex.Text = profile.VideoDeviceIndex;
        _captureShowRegion.IsChecked = profile.ShowRegion;
        _captureCopyTimestamps.IsChecked = profile.CopyTimestamps;
        _captureBenchmark.IsChecked = false;
        _disableAutoScale.IsChecked = false;
        _hwAccelDevice.Text = "";
        _extraHwFrames.Text = "";
        _initHwDevice.Text = profile.InitHardwareDevice;
        _filterHwDevice.Text = profile.FilterHardwareDevice;
        _captureFilterSource.Text = profile.CaptureFilterSource;
        _inputOptions.Text = "";
        _videoEncoderOptions.Text = profile.VideoEncoderOptions;
        _filterOptions.Text = "";
        _crf.Text = profile.Crf;
        _frameRate.Text = "source";
        _twoPass.IsChecked = false;
        _guideProfile.SelectedItem = "Manual / custom";
        _guideProfileDescription.Text = $"Capture source: {profile.SourceFile}{Environment.NewLine}{profile.Description}";
        if (!string.IsNullOrWhiteSpace(profile.CaptureFilterSource))
            _metadata.IsChecked = false;
        AppendLog($"Applied capture profile: {profile.Name} (source: {profile.SourceFile}). Replace example device names with actual names and review the command preview.");
        PreviewCommand(this, new RoutedEventArgs());
    }

    private string BuildCaptureInputOptions()
    {
        var args = new List<string>();
        AddOptionPair(args, "-framerate", _captureFrameRate.Text);
        AddOptionPair(args, "-video_size", _captureVideoSize.Text);
        var pixel = Selected(_capturePixelFormat);
        if (!string.IsNullOrWhiteSpace(pixel) && !pixel.Equals("auto", StringComparison.OrdinalIgnoreCase))
            AddOptionPair(args, "-pixel_format", pixel);
        var codec = Selected(_captureVideoCodec);
        if (!string.IsNullOrWhiteSpace(codec) && !codec.Equals("auto", StringComparison.OrdinalIgnoreCase))
            AddOptionPair(args, "-vcodec", codec);
        AddOptionPair(args, "-rtbufsize", _captureRtBufferSize.Text);
        AddOptionPair(args, "-audio_buffer_size", _captureAudioBufferSize.Text);
        AddOptionPair(args, "-offset_x", _captureOffsetX.Text);
        AddOptionPair(args, "-offset_y", _captureOffsetY.Text);
        AddOptionPair(args, "-video_device_index", _captureVideoDeviceIndex.Text);
        AddOptionPair(args, "-channels", _captureChannels.Text);
        AddOptionPair(args, "-sample_rate", _captureSampleRate.Text);
        if (_captureShowRegion.IsChecked == true)
            AddOptionPair(args, "-show_region", "1");
        var hw = Selected(_hwAccel);
        if (!string.IsNullOrWhiteSpace(hw) && !hw.Equals("None / disabled", StringComparison.OrdinalIgnoreCase))
            AddOptionPair(args, "-hwaccel", hw);
        var outputFormat = Selected(_hwAccelOutputFormat);
        if (!string.IsNullOrWhiteSpace(outputFormat) && !outputFormat.Equals("auto", StringComparison.OrdinalIgnoreCase))
            AddOptionPair(args, "-hwaccel_output_format", outputFormat);
        AddOptionPair(args, "-hwaccel_device", _hwAccelDevice.Text);
        AddOptionPair(args, "-extra_hw_frames", _extraHwFrames.Text);
        return string.Join(" ", args);
    }

    private string BuildHardwareGlobalOptions()
    {
        var args = new List<string>();
        AddOptionPair(args, "-init_hw_device", _initHwDevice.Text);
        AddOptionPair(args, "-filter_hw_device", _filterHwDevice.Text);
        if (_captureCopyTimestamps.IsChecked == true)
            args.Add("-copyts");
        if (_captureBenchmark.IsChecked == true)
            args.Add("-benchmark");
        return string.Join(" ", args);
    }

    private static void AddOptionPair(List<string> args, string option, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        args.Add(option);
        args.Add(value.Trim());
    }

    private Expander BuildAdvancedOptions()
    {
        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new TextBlock
        {
            Text = "Enter any documented FFmpeg command-line option, including codec-specific AVOptions. Use quotes around values containing spaces. For the complete option catalogue, use the FFmpeg options & capabilities window.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("#A4A9B6")
        });
        AddOptionEditor(stack, "Global / logging options (before inputs)", _globalOptions);
        AddOptionEditor(stack, "Input / demuxer options (before -i)", _inputOptions);
        AddOptionEditor(stack, "Additional inputs (one or more -i arguments, e.g. second video/audio/image sequence)", _additionalInputs);
        AddOptionEditor(stack, "Stream mapping / selection", _mappingOptions);
        AddOptionEditor(stack, "Video encoder-specific options", _videoEncoderOptions);
        AddOptionEditor(stack, "Audio encoder-specific options", _audioEncoderOptions);
        AddOptionEditor(stack, "Filters / filtergraph options", _filterOptions);
        AddOptionEditor(stack, "Muxer / container options", _muxerOptions);
        AddOptionEditor(stack, "Other output options", _outputOptions);
        return new Expander
        {
            Header = "Advanced FFmpeg options — encoder, demuxer, filters, muxer, stream mapping and more",
            IsExpanded = false,
            Content = stack
        };
    }

    private static void AddOptionEditor(StackPanel parent, string label, TextBox editor)
    {
        parent.Children.Add(new TextBlock { Text = label, Foreground = Brush("#C9CED9"), FontWeight = FontWeight.SemiBold });
        parent.Children.Add(editor);
    }

    private static string Selected(ComboBox combo) => combo.SelectedItem?.ToString() ?? string.Empty;

    private void ApplyGuideProfile()
    {
        var profile = EncodingGuideProfiles.Find(Selected(_guideProfile));
        if (profile is null)
        {
            _guideProfileDescription.Text = "Manual / custom mode. Choose a documented profile to populate the fields, or edit the controls directly.";
            return;
        }

        SetComboValue(_mode, profile.Mode);
        SetComboValue(_videoCodec, profile.VideoCodec);
        SetComboValue(_audioCodec, profile.AudioCodec);
        SetComboValue(_preset, profile.Preset);
        SetComboValue(_rateControl, profile.RateControl);
        SetComboValue(_size, profile.OutputSize);
        SetComboValue(_pixelFormat, profile.PixelFormat);
        SetComboValue(_inputFormat, profile.InputFormat);
        SetComboValue(_outputFormat, profile.OutputFormat);
        SetComboValue(_videoTune, string.IsNullOrWhiteSpace(profile.VideoTune) ? "auto" : profile.VideoTune);
        SetComboValue(_audioProfile, string.IsNullOrWhiteSpace(profile.AudioProfile) ? "auto" : profile.AudioProfile);

        _crf.Text = profile.Crf;
        _videoBitrate.Text = profile.VideoBitrate;
        _frameRate.Text = profile.FrameRate;
        _audioBitrate.Text = profile.AudioBitrate;
        _metadata.IsChecked = profile.PreserveMetadata;
        _fastStart.IsChecked = profile.FastStart;

        _globalOptions.Text = profile.GlobalOptions;
        _inputOptions.Text = profile.InputOptions;
        _additionalInputs.Text = profile.AdditionalInputs;
        _mappingOptions.Text = profile.MappingOptions;
        _videoEncoderOptions.Text = profile.VideoEncoderOptions;
        _audioEncoderOptions.Text = profile.AudioEncoderOptions;
        _filterOptions.Text = profile.FilterOptions;
        _muxerOptions.Text = profile.MuxerOptions;
        _outputOptions.Text = profile.OutputOptions;

        _minVideoBitrate.Text = profile.MinVideoBitrate;
        _maxVideoBitrate.Text = profile.MaxVideoBitrate;
        _bufferSize.Text = profile.BufferSize;
        _gopSize.Text = profile.GopSize;
        _videoProfile.Text = profile.VideoProfile;
        _videoQscale.Text = profile.VideoQscale;
        _videoQp.Text = profile.VideoQp;
        _audioQscale.Text = profile.AudioQscale;
        _audioVbr.Text = profile.AudioVbr;
        _audioCutoff.Text = profile.AudioCutoff;
        _audioSampleRate.Text = profile.AudioSampleRate;
        _audioChannels.Text = profile.AudioChannels;
        _twoPass.IsChecked = profile.TwoPass;
        _passLogFile.Text = profile.PassLogFile;
        _colorPrimaries.Text = profile.ColorPrimaries;
        _colorTransfer.Text = profile.ColorTransfer;
        _colorSpace.Text = profile.ColorSpace;

        _guideProfileDescription.Text = $"Source: {profile.SourceFile}{Environment.NewLine}{profile.Description}";
        AppendLog($"Applied encoding guide profile: {profile.Name} (source: {profile.SourceFile}). Review the command preview and the imported encoder's help before running.");
        PreviewCommand(this, new RoutedEventArgs());

        var executable = FFmpegLocator.Find("ffmpeg");
        if (executable is null)
        {
            AppendLog("Profile applied without validating codec availability because no FFmpeg toolset is imported.");
            return;
        }

        // Report rather than hide a codec mismatch: actual encoder availability depends on
        // the imported binary, and profile selection must not imply that it is guaranteed.
        if (profile.VideoCodec is not ("copy" or "")
            && !_availableVideoEncoders.Contains(profile.VideoCodec))
            AppendLog($"Profile note: {profile.VideoCodec} is not listed by the currently imported FFmpeg build. The profile remains editable, but a job may fail until a compatible encoder is imported.");
        if (profile.AudioCodec is not ("copy" or "")
            && !_availableAudioEncoders.Contains(profile.AudioCodec))
            AppendLog($"Profile note: {profile.AudioCodec} is not listed by the currently imported FFmpeg build. The profile remains editable, but a job may fail until a compatible encoder is imported.");
    }

    private async Task OpenOptionComposerAsync()
    {
        var window = new FFmpegOptionComposerWindow(
            FFmpegLocator.Find("ffmpeg"),
            Selected(_videoCodec),
            Selected(_audioCodec),
            Selected(_inputFormat),
            Selected(_outputFormat),
            AddAdvancedOption);
        await window.ShowDialog(this);
    }

    private void AddAdvancedOption(string scope, string optionText)
    {
        var target = scope switch
        {
            "Global / logging options" => _globalOptions,
            "Input / demuxer options" => _inputOptions,
            "Additional input source" => _additionalInputs,
            "Stream mapping / selection" => _mappingOptions,
            "Video encoder options" => _videoEncoderOptions,
            "Audio encoder options" => _audioEncoderOptions,
            "Filters / filtergraph" => _filterOptions,
            "Muxer / container options" => _muxerOptions,
            _ => _outputOptions
        };

        target.Text = CombineOptionLines(target.Text, optionText);
        SetComboValue(_guideProfile, "Manual / custom");
        _guideProfileDescription.Text = "Custom settings modified using the searchable encoding-settings editor.";
        AppendLog($"Added option from encoding settings editor to {scope}: {optionText}");
        PreviewCommand(this, new RoutedEventArgs());
    }

    private async Task CleanTemporaryFilesAsync()
    {
        if (_cancellation is not null)
        {
            AppendLog("Temporary-file cleanup is disabled while an FFmpeg job is running.");
            return;
        }

        var outputPath = _output.Text?.Trim();
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            AppendLog("Enter the output path first. Only local output-related .tmp files are eligible for cleanup.");
            return;
        }

        IReadOnlyList<string> arguments;
        try
        {
            var settings = GetSettings();
            if (string.IsNullOrWhiteSpace(settings.InputPath)
                || string.Equals(settings.InputPath, settings.OutputPath, StringComparison.OrdinalIgnoreCase))
            {
                // The cleanup uses only the output pattern and muxer/segment arguments. A
                // harmless synthetic input permits cleanup before a new encode is configured.
                settings = settings with { InputPath = "__ffgrabber_cleanup_probe__.invalid" };
            }

            arguments = FFmpegCommandBuilder.Build(settings);
        }
        catch (Exception ex)
        {
            AppendLog("Could not derive temporary-file patterns from the current settings: " + ex.Message);
            return;
        }

        var candidates = FFmpegTemporaryFileCleanupScope.FindExistingCandidates(outputPath, arguments);
        if (candidates.Count == 0)
        {
            AppendLog("No output-related .tmp files matching the current output/HLS/DASH template were found. Unrelated .tmp files were not scanned.");
            return;
        }

        if (!await ConfirmTemporaryFileCleanupAsync(candidates))
        {
            AppendLog("Temporary-file cleanup cancelled; no files were deleted.");
            return;
        }

        var result = await Task.Run(() =>
            FFmpegTemporaryFileCleanupScope.DeleteConfirmedCandidates(outputPath, arguments, candidates));

        foreach (var path in result.DeletedFiles)
            AppendLog("Removed confirmed output-related temporary file: " + path);
        foreach (var failure in result.FailedFiles)
            AppendLog("Could not remove temporary file: " + failure);

        AppendLog($"Temporary-file cleanup complete: deleted {result.DeletedCount}; failed {result.FailedCount}.");
    }

    private async Task<bool> ConfirmTemporaryFileCleanupAsync(IReadOnlyList<string> candidates)
    {
        var dialog = new Window
        {
            Title = "Confirm output temporary-file cleanup",
            Width = 760,
            Height = 560,
            MinWidth = 560,
            MinHeight = 400,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brush("#FF111319")
        };

        var root = new StackPanel { Spacing = 12, Margin = new Thickness(18) };
        root.Children.Add(new TextBlock
        {
            Text = $"Found {candidates.Count:N0} output-related .tmp file(s). Delete only these listed files?",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        root.Children.Add(new TextBlock
        {
            Text = "This operation is not reversible. The list is restricted to output/muxer-specific naming patterns. Existing files are never deleted automatically; this explicit action requires your confirmation.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("#A4A9B6")
        });
        root.Children.Add(new ListBox { ItemsSource = candidates, MinHeight = 220 });
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var confirmed = false;
        var delete = new Button { Content = $"Delete {candidates.Count:N0} file(s)", Classes = { "primary" } };
        delete.Click += (_, _) => { confirmed = true; dialog.Close(); };
        var cancel = new Button { Content = "Cancel" };
        cancel.Click += (_, _) => dialog.Close();
        actions.Children.Add(delete);
        actions.Children.Add(cancel);
        root.Children.Add(actions);
        dialog.Content = new Border { Classes = { "glassShell" }, Child = root };

        await dialog.ShowDialog(this);
        return confirmed;
    }

    private EncodingSettings GetSettings() => new(
        _input.Text?.Trim() ?? string.Empty,
        _output.Text?.Trim() ?? string.Empty,
        Selected(_mode),
        Selected(_videoCodec),
        Selected(_preset),
        int.TryParse(_crf.Text, out var crf) ? Math.Clamp(crf, 0, 63) : 20,
        Selected(_audioCodec),
        _audioBitrate.Text?.Trim() ?? string.Empty,
        _overwrite.IsChecked == true,
        Selected(_rateControl),
        _videoBitrate.Text?.Trim() ?? "8M",
        Selected(_size),
        string.IsNullOrWhiteSpace(_frameRate.Text) ? "source" : _frameRate.Text.Trim(),
        Selected(_pixelFormat),
        _metadata.IsChecked == true,
        _fastStart.IsChecked == true,
        CombineOptionLines(_globalOptions.Text, BuildHardwareGlobalOptions()),
        CombineOptionLines(_inputOptions.Text, BuildCaptureInputOptions()),
        _mappingOptions.Text ?? string.Empty,
        CombineOptionLines(_videoEncoderOptions.Text, BuildVideoGuideOptions()),
        CombineOptionLines(_audioEncoderOptions.Text, BuildAudioGuideOptions()),
        _filterOptions.Text ?? string.Empty,
        _muxerOptions.Text ?? string.Empty,
        CombineOptionLines(_outputOptions.Text, BuildColorMetadataOptions(), _disableAutoScale.IsChecked == true ? "-noautoscale" : ""),
        Selected(_inputFormat),
        Selected(_outputFormat),
        _additionalInputs.Text ?? string.Empty,
        _twoPass.IsChecked == true,
        _passLogFile.Text?.Trim() ?? string.Empty,
        _captureFilterSource.Text?.Trim() ?? string.Empty,
        _cpuBudget.SelectedItem?.ToString() ?? "Balanced (75%)");

    private async Task LoadAvailableCapabilitiesAsync()
    {
        var executable = FFmpegLocator.Find("ffmpeg");
        if (executable is null)
        {
            AppendLog("No FFmpeg imported yet. Click \"Import FFmpeg…\" and select the folder containing ffmpeg.exe, ffprobe.exe and ffplay.exe.");
            return;
        }

        try
        {
            AppendLog("Discovering encoder, demuxer and muxer options from: " + executable);
            var capabilities = await FFmpegCapabilityService.LoadAsync(executable);
            _availableVideoEncoders = new HashSet<string>(capabilities.VideoEncoders, StringComparer.OrdinalIgnoreCase);
            _availableAudioEncoders = new HashSet<string>(capabilities.AudioEncoders, StringComparer.OrdinalIgnoreCase);
            SetChoices(_videoCodec, new[] { "copy" }.Concat(capabilities.VideoEncoders), "libx264");
            SetChoices(_audioCodec, new[] { "copy" }.Concat(capabilities.AudioEncoders), "aac");
            SetChoices(_inputFormat, new[] { "auto", "dshow", "gdigrab", "lavfi", "v4l2", "alsa", "avfoundation", "pulse", "x11grab" }.Concat(capabilities.Demuxers), "auto");
            SetChoices(_outputFormat, new[] { "auto" }.Concat(capabilities.Muxers), "auto");
            AppendLog($"Discovered {capabilities.VideoEncoders.Count} video encoders, {capabilities.AudioEncoders.Count} audio encoders, {capabilities.Demuxers.Count} demuxers and {capabilities.Muxers.Count} muxers.");
        }
        catch (Exception ex)
        {
            AppendLog("Dynamic FFmpeg capability discovery failed; advanced text options remain available: " + ex);
        }
    }

    private static void SetChoices(ComboBox combo, IEnumerable<string> choices, string preferred)
    {
        var previous = combo.SelectedItem?.ToString();
        var values = choices
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (values.Count == 0)
            return;

        combo.ItemsSource = values;
        combo.SelectedItem = values.FirstOrDefault(value => string.Equals(value, previous, StringComparison.OrdinalIgnoreCase))
            ?? values.FirstOrDefault(value => string.Equals(value, preferred, StringComparison.OrdinalIgnoreCase))
            ?? values[0];
    }

    private void PreviewCommand(object? sender, RoutedEventArgs e)
    {
        try
        {
            var settings = GetSettings();
            var executable = FFmpegLocator.Find("ffmpeg") ?? "ffmpeg.exe";
            if (settings.TwoPass)
            {
                var plan = FFmpegCommandBuilder.BuildTwoPass(settings);
                var firstCommand = FFmpegCommandBuilder.ToDisplayCommand(executable, plan.FirstPassArguments);
                var secondCommand = FFmpegCommandBuilder.ToDisplayCommand(executable, plan.SecondPassArguments);
                _preview.Text = "PASS 1 — analysis to null output:" + Environment.NewLine
                    + firstCommand + Environment.NewLine + Environment.NewLine
                    + "PASS 2 — final output:" + Environment.NewLine + secondCommand;
                AppendLog("Two-pass command preview (pass 1): " + firstCommand);
                AppendLog("Two-pass command preview (pass 2): " + secondCommand);
            }
            else
            {
                var command = FFmpegCommandBuilder.ToDisplayCommand(executable, FFmpegCommandBuilder.Build(settings));
                _preview.Text = command;
                AppendLog("Command preview: " + command);
            }
        }
        catch (Exception ex)
        {
            AppendLog("Preview error: " + ex.Message);
        }
    }

    private void AddToQueue(object? sender, RoutedEventArgs e)
    {
        try
        {
            var settings = GetSettings();
            if (settings.TwoPass)
                _ = FFmpegCommandBuilder.BuildTwoPass(settings);
            else
                _ = FFmpegCommandBuilder.Build(settings);
            _jobs.Enqueue(settings);
            var passLabel = settings.TwoPass ? " [two-pass]" : string.Empty;
            var label = $"{_queueItems.Count + 1}. {Path.GetFileName(settings.InputPath)} → {Path.GetFileName(settings.OutputPath)} [{settings.Mode}]{passLabel}";
            _queueItems.Add(label);
            AppendLog("Queued: " + label);
            PreviewCommand(sender, e);
        }
        catch (Exception ex)
        {
            AppendLog("Cannot queue job: " + ex.Message);
        }
    }

    private void RefreshFFmpegStatus()
    {
        var importedDirectory = FFmpegLocator.ImportedDirectory;
        if (importedDirectory is not null)
        {
            _ffmpegStatus.Text = $"Active FFmpeg source: Imported user tool set{Environment.NewLine}{importedDirectory}{Environment.NewLine}FFmpeg, ffprobe and ffplay will all run from this folder.";
            return;
        }

        _ffmpegStatus.Text = "No FFmpeg imported. FFGrabber does not include FFmpeg: click \"Import FFmpeg…\" and select your folder containing ffmpeg.exe, ffprobe.exe and ffplay.exe.";
    }

    private async Task ImportFFmpegAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select the FFmpeg folder containing ffmpeg.exe, ffprobe.exe, and ffplay.exe",
            AllowMultiple = false
        });
        if (folders.Count == 0)
            return;

        var selectedDirectory = folders[0].TryGetLocalPath() ?? folders[0].Path.LocalPath;
        if (string.IsNullOrWhiteSpace(selectedDirectory))
        {
            AppendLog("FFmpeg import cancelled: the selected folder is not a local filesystem path.");
            return;
        }

        _importFfmpegButton.IsEnabled = false;
        _ffmpegStatus.Text = "Validating ffmpeg.exe, ffprobe.exe and ffplay.exe…";
        AppendLog("Validating user-provided FFmpeg folder: " + selectedDirectory);
        try
        {
            var result = await FFmpegImportService.ImportAndValidateAsync(selectedDirectory);
            RefreshFFmpegStatus();
            AppendLog("Imported user FFmpeg tool set: " + result.Directory);
            AppendLog("ffmpeg: " + result.FFmpegVersion);
            AppendLog("ffprobe: " + result.FFprobeVersion);
            AppendLog("ffplay: " + result.FFplayVersion);
            AppendLog("Future encoding jobs, probing, playback and capability reports will use the imported executables.");
            await LoadAvailableCapabilitiesAsync();
        }
        catch (Exception ex)
        {
            _ffmpegStatus.Text = "FFmpeg import failed. The previous tool selection remains active.";
            AppendLog("FFmpeg import failed: " + ex);
        }
        finally
        {
            _importFfmpegButton.IsEnabled = true;
        }
    }

    private async Task BrowseInputAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select source media",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Media files")
                {
                    Patterns = ["*.mp4", "*.mkv", "*.mov", "*.webm", "*.avi", "*.mp3", "*.wav", "*.flac", "*.m4a", "*.aac", "*.ts", "*.mts", "*.m2ts", "*.vob", "*.wmv", "*.asf", "*.ogg", "*.opus", "*.hevc", "*.h264", "*.y4m", "*.nut"],
                },
                FilePickerFileTypes.All
            ]
        });
        if (files.Count == 0) return;
        _input.Text = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        if (string.IsNullOrWhiteSpace(_output.Text))
            _output.Text = Path.Combine(
                Path.GetDirectoryName(_input.Text) ?? string.Empty,
                Path.GetFileNameWithoutExtension(_input.Text) + "_encoded.mkv");
        AppendLog("Selected input: " + _input.Text);
    }

    private async Task BrowseOutputAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose output folder",
            AllowMultiple = false
        });
        if (folders.Count == 0) return;
        var folder = folders[0].TryGetLocalPath() ?? folders[0].Path.LocalPath;
        var filename = string.IsNullOrWhiteSpace(_input.Text)
            ? "output.mkv"
            : Path.GetFileNameWithoutExtension(_input.Text) + "_encoded.mkv";
        _output.Text = Path.Combine(folder, filename);
        AppendLog("Output path set to: " + _output.Text);
    }

    private async Task RunQueueAsync()
    {
        if (_jobs.Count == 0)
        {
            AppendLog("Queue is empty.");
            return;
        }

        var executable = FFmpegLocator.Find("ffmpeg");
        if (executable is null)
        {
            AppendLog("No FFmpeg imported. Click \"Import FFmpeg…\" and select the folder containing ffmpeg.exe, ffprobe.exe and ffplay.exe.");
            return;
        }

        _cancellation = new CancellationTokenSource();
        _startButton.IsEnabled = false;
        _cancelButton.IsEnabled = true;
        AppendLog($"Starting queue with { _jobs.Count } job(s). Executable: {executable}");

        try
        {
            while (_jobs.Count > 0)
            {
                var job = _jobs.Dequeue();
                // Keep the on-screen queue in step with the job queue; otherwise started jobs
                // stay listed as pending and new entries are numbered after finished ones.
                if (_queueItems.Count > 0)
                    _queueItems.RemoveAt(0);
                _activeJobSettings = job;
                AppendLog("Starting output: " + job.OutputPath);
                try
                {
                    var twoPassPlan = job.TwoPass ? FFmpegCommandBuilder.BuildTwoPass(job) : null;
                    var args = twoPassPlan?.FirstPassArguments ?? FFmpegCommandBuilder.Build(job);
                    AppendLog("Running: " + FFmpegCommandBuilder.ToDisplayCommand(executable, args));

                    // Track the final output's temporary files across the whole job. Pass 1
                    // writes to the null muxer, so cleanup is scoped to the requested output.
                    var cleanupArguments = twoPassPlan?.SecondPassArguments ?? args;
                    var durationSeconds = await TryGetInputDurationSecondsAsync(job.InputPath, _cancellation.Token);
                    ResetConversionProgress(job, durationSeconds);
                    var temporaryFileScope = FFmpegTemporaryFileCleanupScope.Begin(job.OutputPath, cleanupArguments);
                    try
                    {
                        if (twoPassPlan is null)
                        {
                            var result = await RunMonitoredFfmpegAsync(
                                executable, args, job, durationSeconds, "Encoding", _cancellation.Token);
                            AppendLog(result.ExitCode == 0
                                ? "Completed: " + job.OutputPath
                                : $"Failed (exit {result.ExitCode}): {job.OutputPath}");
                        }
                        else
                        {
                            AppendLog("Two-pass job: starting pass 1/2 (analysis output is discarded).");
                            var firstPass = await RunMonitoredFfmpegAsync(
                                executable, twoPassPlan.FirstPassArguments, job, durationSeconds, "Pass 1 of 2", _cancellation.Token);

                            if (firstPass.ExitCode != 0)
                            {
                                AppendLog($"Two-pass pass 1 failed with exit code {firstPass.ExitCode}; pass 2 was not started.");
                            }
                            else
                            {
                                AppendLog("Two-pass pass 1 succeeded; starting pass 2/2 (final output).");
                                var secondPass = await RunMonitoredFfmpegAsync(
                                    executable, twoPassPlan.SecondPassArguments, job, durationSeconds, "Pass 2 of 2", _cancellation.Token);
                                AppendLog(secondPass.ExitCode == 0
                                    ? "Two-pass encoding completed: " + job.OutputPath
                                    : $"Two-pass pass 2 failed (exit {secondPass.ExitCode}): {job.OutputPath}");
                            }
                        }
                    }
                    finally
                    {
                        // Wait for FFmpeg to finish or be killed before checking for orphaned
                        // files. Cleanup is restricted to patterns in the output's own scope.
                        if (temporaryFileScope is not null)
                        {
                            var cleanup = _cancellation?.IsCancellationRequested == true
                                ? temporaryFileScope.CleanupAfterCancellation()
                                : temporaryFileScope.CleanupCreatedFiles();
                            foreach (var orphan in cleanup.DeletedFiles)
                                AppendLog("Removed temporary file created by this FFmpeg job: " + orphan);
                            foreach (var failure in cleanup.FailedFiles)
                                AppendLog("Could not remove FFmpeg temporary file: " + failure);
                            if (cleanup.DeletedCount > 0)
                                AppendLog($"Automatic temporary-file cleanup removed {cleanup.DeletedCount} file(s).");
                        }

                        if (twoPassPlan is { IsAutoManagedPassLog: true })
                            CleanupManagedPassLogs(twoPassPlan.PassLogPrefix);
                    }
                }
                catch (OperationCanceledException)
                {
                    AppendLog("Job canceled by user; FFmpeg was terminated and job-owned temporary files are being cleaned.");
                    Dispatcher.UIThread.Post(() =>
                    {
                        _progressDescription.Text = "Canceled — process terminated; job-owned temporary files cleaned where possible.";
                        _conversionProgress.IsIndeterminate = false;
                        _conversionProgress.Value = 0;
                        _systemUsage.Text = "FFmpeg is no longer running.";
                    });
                    break;
                }
                catch (Exception ex)
                {
                    AppendLog("Job error: " + ex);
                }
                _activeJobSettings = null;
            }
        }
        finally
        {
            _activeJobSettings = null;
            _cancellation.Dispose();
            _cancellation = null;
            _startButton.IsEnabled = true;
            _cancelButton.IsEnabled = false;
            _cancelButton.Content = ButtonContent("\\uE711", "Cancel active job");
            Dispatcher.UIThread.Post(() =>
            {
                _conversionProgress.IsIndeterminate = false;
                _systemUsage.Text = "FFmpeg is no longer running.";
            });
            AppendLog("Queue processing ended.");
        }
    }

    private void CleanupManagedPassLogs(string passLogPrefix)
    {
        var directory = Path.GetDirectoryName(passLogPrefix);
        var prefix = Path.GetFileName(passLogPrefix);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(prefix) || !Directory.Exists(directory))
            return;

        try
        {
            foreach (var file in Directory.GetFiles(directory, prefix + "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(file);
                    AppendLog("Removed FFmpeg two-pass log: " + file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    AppendLog("Could not remove FFmpeg two-pass log: " + file + " — " + ex.Message);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppendLog("Could not inspect managed two-pass logs: " + ex.Message);
        }
    }

    private async Task ProbeAsync()
    {
        if (string.IsNullOrWhiteSpace(_input.Text))
        {
            AppendLog("Enter a source path, URL, or device expression first.");
            return;
        }

        var executable = FFmpegLocator.Find("ffprobe");
        if (executable is null)
        {
            AppendLog("ffprobe.exe was not found.");
            return;
        }

        var args = new[] { "-hide_banner", "-show_format", "-show_streams", "-of", "json", _input.Text! };
        AppendLog("Running: " + FFmpegCommandBuilder.ToDisplayCommand(executable, args));
        try
        {
            var result = await FFmpegProcessRunner.RunAsync(
                executable,
                args,
                onStandardOutput: line => LogProcessLine("FFPROBE STDOUT", line),
                onStandardError: line => LogProcessLine("FFPROBE STDERR", line));
            AppendLog($"ffprobe exit code: {result.ExitCode}");
        }
        catch (Exception ex)
        {
            AppendLog("ffprobe failed: " + ex);
        }
    }

    private void Play(object? sender, RoutedEventArgs e) => _ = PlayAsync();

    private async Task PlayAsync()
    {
        if (string.IsNullOrWhiteSpace(_input.Text))
        {
            AppendLog("Enter a source path, URL, or device expression first.");
            return;
        }

        var executable = FFmpegLocator.Find("ffplay");
        if (executable is null)
        {
            AppendLog("ffplay.exe was not found.");
            return;
        }

        try
        {
            // FFplay's default display aspect ratio calculation is preserved by forcing only
            // one dimension. The other dimension is calculated by ffplay from the media DAR.
            var maxWidth = 1280;
            var maxHeight = 720;
            var displayAspect = await TryGetDisplayAspectRatioAsync(_input.Text!);
            var maxAspect = maxWidth / (double)maxHeight;
            var args = new List<string> { "-hide_banner", "-window_title", "FFGrabber — " + Path.GetFileName(_input.Text!) };
            if (displayAspect is > 0)
            {
                if (displayAspect.Value >= maxAspect)
                    args.AddRange(["-x", maxWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
                else
                    args.AddRange(["-y", maxHeight.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            }
            else
            {
                // Unknown/streaming input: set width only and let FFplay keep its default DAR.
                args.AddRange(["-x", maxWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            }
            args.Add(_input.Text!);
            AppendLog("Launching size-capped FFplay with its default display aspect ratio: " + FFmpegCommandBuilder.ToDisplayCommand(executable, args));
            // Pass the media path as a separate process argument; never use a shell.
            var info = new ProcessStartInfo(executable) { UseShellExecute = false };
            foreach (var arg in args) info.ArgumentList.Add(arg);
            Process.Start(info);
        }
        catch (Exception ex)
        {
            AppendLog("Could not launch ffplay: " + ex);
        }
    }

    private void CancelActiveJob()
    {
        var cancellation = _cancellation;
        if (cancellation is null || cancellation.IsCancellationRequested)
            return;

        // CancellationToken registration in FFmpegProcessRunner kills the entire process tree.
        // It runs synchronously here; file cleanup follows only after process exit is observed.
        _cancelButton.IsEnabled = false;
        _cancelButton.Content = ButtonContent("\\uE711", "Stopping…");
        _progressDescription.Text = "Cancellation requested — terminating FFmpeg process tree…";
        AppendLog("Cancellation requested. Terminating active FFmpeg process tree now.");
        try { cancellation.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    private async Task<ProcessRunResult> RunMonitoredFfmpegAsync(
        string executable,
        IReadOnlyList<string> commandArguments,
        EncodingSettings job,
        double? durationSeconds,
        string stage,
        CancellationToken cancellationToken)
    {
        var arguments = commandArguments.ToList();
        // FFmpeg's progress protocol is routed to stderr so an output targeting stdout/pipe:1
        // remains intact. -nostats removes only the periodic terminal status line.
        var insertAt = arguments.Count > 0 && arguments[0] == "-hide_banner" ? 1 : 0;
        arguments.InsertRange(insertAt, ["-progress", "pipe:2", "-nostats"]);

        Task usageMonitor = Task.CompletedTask;
        try
        {
            AppendLog("Running with progress telemetry: " + FFmpegCommandBuilder.ToDisplayCommand(executable, arguments));
            var result = await FFmpegProcessRunner.RunAsync(
                executable,
                arguments,
                onStandardOutput: line =>
                {
                    // Keep any genuine output/log text, but don't clutter the log with progress key/value frames.
                    if (!IsProgressProtocolLine(line))
                        LogProcessLine("FFMPEG STDOUT", line);
                },
                onStandardError: line =>
                {
                    if (!HandleProgressProtocolLine(line, durationSeconds, job, stage))
                        LogProcessLine("FFMPEG STDERR", line);
                },
                cancellationToken: cancellationToken,
                onProcessStarted: processId =>
                {
                    Interlocked.Exchange(ref _activeFfmpegProcessId, processId);
                    usageMonitor = SystemUsageMonitor.RunAsync(
                        processId,
                        snapshot => Dispatcher.UIThread.Post(() =>
                        {
                            if (Interlocked.CompareExchange(ref _activeFfmpegProcessId, 0, 0) != processId)
                                return;
                            _systemUsage.Text =
                                $"FFmpeg PID {snapshot.ProcessId}  |  Process CPU {snapshot.ProcessCpuPercent:F1}% of total machine capacity  |  System CPU {snapshot.SystemCpuPercent:F1}%"
                                + Environment.NewLine
                                + $"Working set {snapshot.ProcessWorkingSetMiB:N0} MiB  |  Private memory {snapshot.ProcessPrivateMemoryMiB:N0} MiB  |  Threads {snapshot.ProcessThreadCount}"
                                + Environment.NewLine
                                + (snapshot.TotalMemoryGiB > 0
                                    ? $"System RAM {snapshot.UsedMemoryGiB:F1}/{snapshot.TotalMemoryGiB:F1} GiB ({snapshot.UsedMemoryPercent:F1}% used)"
                                    : "System RAM telemetry is unavailable.");
                        }),
                        () => Interlocked.CompareExchange(ref _activeFfmpegProcessId, 0, 0) == processId);
                });
            if (result.ExitCode == 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (durationSeconds is > 0)
                    {
                        _conversionProgress.IsIndeterminate = false;
                        _conversionProgress.Value = 100;
                    }
                    _progressDescription.Text = $"{stage} completed — {Path.GetFileName(job.OutputPath)}";
                });
            }
            return result;
        }
        finally
        {
            Interlocked.Exchange(ref _activeFfmpegProcessId, 0);
            try { await usageMonitor.ConfigureAwait(false); }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                AppendLog("System usage sampling ended: " + ex.Message);
            }
        }
    }

    private static bool IsProgressProtocolLine(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("frame=", StringComparison.Ordinal)
            || trimmed.StartsWith("fps=", StringComparison.Ordinal)
            || trimmed.StartsWith("stream_", StringComparison.Ordinal)
            || trimmed.StartsWith("bitrate=", StringComparison.Ordinal)
            || trimmed.StartsWith("total_size=", StringComparison.Ordinal)
            || trimmed.StartsWith("out_time_us=", StringComparison.Ordinal)
            || trimmed.StartsWith("out_time_ms=", StringComparison.Ordinal)
            || trimmed.StartsWith("out_time=", StringComparison.Ordinal)
            || trimmed.StartsWith("dup_frames=", StringComparison.Ordinal)
            || trimmed.StartsWith("drop_frames=", StringComparison.Ordinal)
            || trimmed.StartsWith("speed=", StringComparison.Ordinal)
            || trimmed.StartsWith("progress=", StringComparison.Ordinal);
    }

    private bool HandleProgressProtocolLine(string line, double? durationSeconds, EncodingSettings job, string stage)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith("out_time_us=", StringComparison.Ordinal)
            && long.TryParse(trimmed.AsSpan("out_time_us=".Length), out var microseconds)
            && microseconds >= 0)
        {
            var elapsedSeconds = microseconds / 1_000_000d;
            Dispatcher.UIThread.Post(() =>
            {
                if (durationSeconds is > 0)
                {
                    var percent = Math.Clamp(elapsedSeconds / durationSeconds.Value * 100d, 0d, 100d);
                    _conversionProgress.IsIndeterminate = false;
                    _conversionProgress.Value = percent;
                    _progressDescription.Text =
                        $"{stage}  |  {percent:F1}%  |  {FormatProgressTime(elapsedSeconds)} / {FormatProgressTime(durationSeconds.Value)}"
                        + Environment.NewLine + Path.GetFileName(job.InputPath) + " → " + Path.GetFileName(job.OutputPath);
                }
                else
                {
                    _conversionProgress.IsIndeterminate = true;
                    _progressDescription.Text = $"{stage}  |  Processed {FormatProgressTime(elapsedSeconds)} (duration unavailable)"
                        + Environment.NewLine + Path.GetFileName(job.InputPath) + " → " + Path.GetFileName(job.OutputPath);
                }
            });
            return true;
        }

        if (trimmed.Equals("progress=end", StringComparison.Ordinal))
        {
            Dispatcher.UIThread.Post(() =>
            {
                _conversionProgress.IsIndeterminate = false;
                _conversionProgress.Value = 100;
                _progressDescription.Text = $"{stage} — finalizing output…";
            });
            return true;
        }

        return IsProgressProtocolLine(trimmed);
    }

    private static string FormatProgressTime(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            return "—";
        var time = TimeSpan.FromSeconds(Math.Min(seconds, TimeSpan.MaxValue.TotalSeconds));
        return time.TotalHours >= 1 ? time.ToString(@"hh\:mm\:ss") : time.ToString(@"mm\:ss");
    }

    private void ResetConversionProgress(EncodingSettings job, double? durationSeconds)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _conversionProgress.IsIndeterminate = durationSeconds is not > 0;
            _conversionProgress.Value = 0;
            _progressDescription.Text =
                $"Starting  |  {Path.GetFileName(job.InputPath)} → {Path.GetFileName(job.OutputPath)}";
            _systemUsage.Text = "Waiting for FFmpeg process metrics…";
        });
    }

    private async Task<double?> TryGetInputDurationSecondsAsync(string inputPath, CancellationToken cancellationToken)
    {
        var ffprobe = FFmpegLocator.Find("ffprobe");
        if (ffprobe is null || string.IsNullOrWhiteSpace(inputPath))
            return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(6));
        try
        {
            var result = await FFmpegProcessRunner.RunAsync(
                ffprobe,
                ["-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", inputPath],
                cancellationToken: timeout.Token);
            if (result.ExitCode == 0)
            {
                foreach (var value in result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                {
                    if (double.TryParse(value.Trim(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var seconds)
                        && double.IsFinite(seconds) && seconds > 0)
                        return seconds;
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            AppendLog("Duration probe timed out after six seconds; progress will show elapsed media time without a percentage.");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            AppendLog("Could not determine media duration; progress percentage will be unavailable: " + ex.Message);
        }
        return null;
    }

    private async Task<double?> TryGetDisplayAspectRatioAsync(string mediaPath)
    {
        var ffprobe = FFmpegLocator.Find("ffprobe");
        if (ffprobe is null)
            return null;

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            var result = await FFmpegProcessRunner.RunAsync(
                ffprobe,
                ["-v", "error", "-select_streams", "v:0", "-show_entries",
                 "stream=width,height,sample_aspect_ratio,display_aspect_ratio:stream_side_data=rotation",
                 "-of", "json", mediaPath],
                cancellationToken: timeout.Token);
            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
                return null;

            using var document = JsonDocument.Parse(result.StandardOutput);
            if (!document.RootElement.TryGetProperty("streams", out var streams)
                || streams.ValueKind != JsonValueKind.Array || streams.GetArrayLength() == 0)
                return null;

            var stream = streams[0];
            var ratio = ReadRatio(stream, "display_aspect_ratio");
            if (ratio is null
                && stream.TryGetProperty("width", out var widthNode)
                && stream.TryGetProperty("height", out var heightNode)
                && widthNode.TryGetInt32(out var width) && heightNode.TryGetInt32(out var height)
                && width > 0 && height > 0)
            {
                ratio = width / (double)height;
                var sample = ReadRatio(stream, "sample_aspect_ratio");
                if (sample is > 0) ratio *= sample.Value;
            }

            // FFprobe reports rotation in stream_side_data on many formats. A quarter turn
            // swaps the display axes; the computed ffplay window remains within its size cap.
            if (ratio is > 0 && stream.TryGetProperty("side_data_list", out var sideData)
                && sideData.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in sideData.EnumerateArray())
                {
                    if (item.TryGetProperty("rotation", out var rotationNode)
                        && rotationNode.TryGetDouble(out var degrees)
                        && Math.Abs(Math.Abs(degrees) % 180d - 90d) < 1d)
                    {
                        ratio = 1d / ratio.Value;
                        break;
                    }
                }
            }
            return ratio is > 0 && double.IsFinite(ratio.Value) ? ratio : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException
            or System.ComponentModel.Win32Exception or OperationCanceledException)
        {
            AppendLog("ffprobe could not calculate display aspect ratio; ffplay will use default DAR with a width limit. " + ex.Message);
            return null;
        }
    }

    private static double? ReadRatio(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out var valueNode) || valueNode.ValueKind != JsonValueKind.String)
            return null;
        var value = valueNode.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value == "N/A")
            return null;
        var parts = value.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length == 2
            && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var numerator)
            && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var denominator)
            && numerator > 0 && denominator > 0)
            return numerator / denominator;
        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ratio)
            && ratio > 0 && double.IsFinite(ratio))
            return ratio;
        return null;
    }

    private async Task OpenMediaInfoAsync(string mediaPath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            AppendLog("Select a media file first, then open MediaInfo.");
            return;
        }

        var window = new MediaInfoWindow(mediaPath.Trim());
        await window.ShowDialog(this);
    }

    private async Task HandleStartupArgumentsAsync(string[] arguments)
    {
        if (arguments.Length < 2 || string.IsNullOrWhiteSpace(arguments[0]) || string.IsNullOrWhiteSpace(arguments[1]))
            return;

        var action = arguments[0].Trim().ToLowerInvariant();
        var inputPath = arguments[1].Trim().Trim('"');
        if (!File.Exists(inputPath))
        {
            AppendLog("Explorer action ignored because the source file no longer exists: " + inputPath);
            return;
        }

        _input.Text = inputPath;
        var folder = Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory;
        var stem = Path.GetFileNameWithoutExtension(inputPath);
        switch (action)
        {
            case "--context-play":
                await PlayAsync();
                return;
            case "--context-mediainfo":
                await OpenMediaInfoAsync(inputPath);
                return;
            case "--context-convert":
                SetComboValue(_mode, "Convert / encode");
                _output.Text = Path.Combine(folder, stem + "_converted.mkv");
                break;
            case "--context-remux":
                SetComboValue(_mode, "Remux / stream copy");
                _output.Text = Path.Combine(folder, stem + "_remuxed.mkv");
                break;
            case "--context-demux":
                // Video-only stream-copy output; the selected container remains user-editable.
                SetComboValue(_mode, "Video only");
                _output.Text = Path.Combine(folder, stem + "_video.mkv");
                break;
            case "--context-extract-audio":
                SetComboValue(_mode, "Extract audio");
                SetComboValue(_audioCodec, "copy");
                _output.Text = Path.Combine(folder, stem + "_audio.mka");
                break;
            default:
                return;
        }

        AddToQueue(this, new RoutedEventArgs());
        await RunQueueAsync();
    }

    private async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directory = Path.Combine(localData, "FFGrabber");
            var lastCheckPath = Path.Combine(directory, "last-update-check-utc.txt");
            if (File.Exists(lastCheckPath)
                && DateTimeOffset.TryParse(await File.ReadAllTextAsync(lastCheckPath), out var lastCheck)
                && DateTimeOffset.UtcNow - lastCheck.ToUniversalTime() < TimeSpan.FromHours(24))
                return;

            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(lastCheckPath, DateTimeOffset.UtcNow.ToString("O"));
            var mediaInfoVersion = await MediaInfoService.GetVersionAsync();
            var result = await UpdateCheckService.CheckAsync(AppVersion, mediaInfoVersion);
            foreach (var note in result.Notes)
                AppendLog("Automatic update check: " + note);
            if (result.Available.Count > 0)
                await ShowAvailableUpdatesAsync(result.Available);
        }
        catch (Exception ex)
        {
            AppendLog("Automatic update check failed without interrupting FFGrabber: " + ex.Message);
        }
    }

    private async Task ShowAvailableUpdatesAsync(IReadOnlyList<AvailableUpdate> updates)
    {
        var dialog = new Window
        {
            Title = "FFGrabber — Updates available",
            Width = 620,
            Height = 360,
            MinWidth = 520,
            MinHeight = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brush("#FF111319")
        };
        var root = new StackPanel { Spacing = 12, Margin = new Thickness(20) };
        root.Children.Add(new TextBlock
        {
            Text = "New releases are available",
            FontSize = 22,
            FontWeight = FontWeight.SemiBold
        });
        root.Children.Add(new TextBlock
        {
            Text = "FFGrabber checks official release metadata automatically. It does not silently download or replace application binaries or your imported FFmpeg/MediaInfo tools.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("#A4A9B6")
        });
        foreach (var update in updates)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"{update.Product}: {update.CurrentVersion} → {update.LatestVersion}",
                FontWeight = FontWeight.SemiBold
            });
            root.Children.Add(Button("Open official " + update.Product + " release ↗", (_, _) => OpenUrl(update.ReleaseUrl)));
        }
        var close = Button("Close", (_, _) => dialog.Close());
        root.Children.Add(close);
        dialog.Content = new Border { Classes = { "glassShell" }, Child = root };
        await dialog.ShowDialog(this);
    }

    private void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { AppendLog("Could not open the release URL: " + ex.Message); }
    }

    private async Task OpenGuideLibraryAsync()
    {
        var window = new GuideLibraryWindow((profileName, isCapture) =>
        {
            if (isCapture)
            {
                SetComboValue(_capturePreset, profileName);
                ApplyCapturePreset();
                _navigateToWorkspace?.Invoke(1);
            }
            else
            {
                SetComboValue(_guideProfile, profileName);
                ApplyGuideProfile();
                _navigateToWorkspace?.Invoke(0);
            }

            AppendLog($"Applied related guide-library profile: {profileName}");
        });
        await window.ShowDialog(this);
    }

    private async Task OpenOptionsAsync()
    {
        var options = new OptionsWindow(
            FFmpegLocator.Find("ffmpeg"),
            _sessionLog,
            GetSettings,
            BuildQueueSnapshot,
            AppendLog);
        await options.ShowDialog(this);
    }

    private async Task OpenCapabilitiesAsync()
    {
        var window = new FFmpegCapabilitiesWindow(FFmpegLocator.Find("ffmpeg"));
        await window.ShowDialog(this);
    }

    private async Task OpenAboutAsync()
    {
        var assembly = typeof(MainWindow).Assembly;
        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(new TextBlock { Text = "FFGrabber", FontSize = 26, FontWeight = FontWeight.Bold, Foreground = Brush("#E5F8FF") });
        root.Children.Add(new TextBlock { Text = $"Version {AppVersion}", FontSize = 18, FontWeight = FontWeight.SemiBold, Foreground = Brush("#56DDF9") });
        root.Children.Add(new TextBlock
        {
            Text = $"Assembly version: {assembly.GetName().Version}\nRuntime: {RuntimeInformation.FrameworkDescription}\nOperating system: {RuntimeInformation.OSDescription}\nProcess architecture: {RuntimeInformation.ProcessArchitecture}\n\nFFGrabber is an import-only frontend. FFmpeg, FFprobe and FFplay are supplied and updated by the user; this application does not bundle or fetch those executables.",
            TextWrapping = TextWrapping.Wrap, Foreground = Brush("#C6D0E0")
        });
        root.Children.Add(new TextBlock { Text = "Source and releases: github.com/PCUser2021/FFGrabber", Foreground = Brush("#8BDFFF"), TextWrapping = TextWrapping.Wrap });
        var close = new Button { Content = "Close", HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 100 };
        var dialog = new Window { Title = $"About FFGrabber v{AppVersion}", Width = 520, Height = 360, MinWidth = 520, MinHeight = 360, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        close.Click += (_, _) => dialog.Close();
        root.Children.Add(close);
        dialog.Content = new Border { Classes = { "glassShell" }, Child = root };
        try { dialog.Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://FFGrabber/Assets/Windows11.ico"))); } catch { }
        await dialog.ShowDialog(this);
    }

    private async Task ListDirectShowDevicesAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AppendLog("DirectShow device discovery is available only on Windows.");
            return;
        }
        var executable = FFmpegLocator.Find("ffmpeg");
        if (executable is null)
        {
            AppendLog("Import a Windows FFmpeg build first, then list DirectShow devices.");
            return;
        }
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var arg in new[] { "-hide_banner", "-list_devices", "true", "-f", "dshow", "-i", "dummy" })
            startInfo.ArgumentList.Add(arg);
        try
        {
            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
                throw new InvalidOperationException("FFmpeg did not start.");
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            AppendLog("DirectShow device enumeration finished (exit code " + process.ExitCode + ").");
            foreach (var line in (stdout + Environment.NewLine + stderr).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                AppendLog(line);
            AppendLog("Replace example camera/microphone names with the exact names shown above.");
        }
        catch (Exception ex)
        {
            AppendLog("DirectShow device discovery failed: " + ex);
        }
    }

    private string BuildQueueSnapshot() =>
        JsonSerializer.Serialize(new
        {
            DisplayQueue = _queueItems.ToArray(),
            ActiveJobSettings = _activeJobSettings,
            PendingJobSettings = _jobs.ToArray()
        }, new JsonSerializerOptions { WriteIndented = true });

    private void AppendLog(string message)
    {
        _sessionLog.Write(message);
        if (Dispatcher.UIThread.CheckAccess())
            AppendDisplayLog(message);
        else
            Dispatcher.UIThread.Post(() => AppendDisplayLog(message));
    }

    private void LogProcessLine(string channel, string line)
    {
        _sessionLog.Write($"{channel}: {line}");
        Dispatcher.UIThread.Post(() => AppendDisplayLog($"{channel}: {line}"));
    }

    private void AppendDisplayLog(string message)
    {
        _displayLog.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
        _displayLog.Append("  ");
        _displayLog.AppendLine(message);
        const int displayLimit = 60_000;
        if (_displayLog.Length > displayLimit)
            _displayLog.Remove(0, _displayLog.Length - displayLimit);
        _log.Text = _displayLog.ToString();
    }
}
