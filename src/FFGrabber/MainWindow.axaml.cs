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

    private readonly TextBox _globalOptions = MultiLine("-loglevel verbose -threads 0", 56);
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
    private CancellationTokenSource? _cancellation;
    private EncodingSettings? _activeJobSettings;

    public MainWindow()
    {
        InitializeComponent();
        Title = "FFGrabber — FFmpeg workstation";
        Width = 1260;
        Height = 1000;
        MinWidth = 900;
        MinHeight = 700;

        var root = new StackPanel { Spacing = 14, Margin = new Thickness(24) };
        var hero = new Border { Classes = { "heroPanel" }, Padding = new Thickness(20) };
        var heroContent = new StackPanel { Spacing = 10 };
        var brandRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };
        brandRow.Children.Add(new Border
        {
            Classes = { "brandMark" },
            Child = new TextBlock
            {
                Text = "FG",
                FontSize = 19,
                FontWeight = FontWeight.Bold,
                Foreground = Brush("#171D10"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });
        brandRow.Children.Add(new TextBlock { Text = "FFGrabber", Classes = { "pageTitle" }, VerticalAlignment = VerticalAlignment.Center });
        brandRow.Children.Add(new Border
        {
            Classes = { "tagPill" },
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = "PURE GUI  ·  IMPORT-ONLY", FontSize = 10, FontWeight = FontWeight.Bold, Foreground = Brush("#C2F970") }
        });
        heroContent.Children.Add(brandRow);
        heroContent.Children.Add(new TextBlock
        {
            Text = "A theme-aware local workstation for your own FFmpeg toolset. No FFmpeg executables are bundled or downloaded; formats, codecs, and detailed options are queried from the binaries you import.",
            Foreground = Brush("#B7BECC"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13
        });
        hero.Child = heroContent;
        root.Children.Add(hero);

        var topActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        topActions.Children.Add(GlyphButton("\uE713", "Options", async (_, _) => await OpenOptionsAsync()));
        _importFfmpegButton.Content = ButtonContent("\uE8E5", "Import FFmpeg…");
        topActions.Children.Add(_importFfmpegButton);
        _importFfmpegButton.Click += async (_, _) => await ImportFFmpegAsync();
        topActions.Children.Add(GlyphButton("\uE71C", "FFmpeg options & capabilities", async (_, _) => await OpenCapabilitiesAsync()));
        topActions.Children.Add(GlyphButton("\uE713", "Encoding settings editor", async (_, _) => await OpenOptionComposerAsync()));
        topActions.Children.Add(GlyphButton("\uE74D", "Clean output .tmp files…", async (_, _) => await CleanTemporaryFilesAsync()));
        root.Children.Add(topActions);
        RefreshFFmpegStatus();
        root.Children.Add(new Border { Classes = { "statusPanel" }, Child = _ffmpegStatus });

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
        root.Children.Add(sourcePanel);

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

        root.Children.Add(settingsPanel);

        root.Children.Add(BuildAdvancedOptions());

        root.Children.Add(new TextBlock
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
        _cancelButton.Click += (_, _) => _cancellation?.Cancel();
        actions.Children.Add(_cancelButton);
        actions.Children.Add(GlyphButton("\uE946", "Inspect with ffprobe", async (_, _) => await ProbeAsync()));
        actions.Children.Add(GlyphButton("\uE768", "Play with ffplay", Play));
        root.Children.Add(actions);

        root.Children.Add(Heading("Command preview"));
        root.Children.Add(_preview);
        root.Children.Add(Heading("Queue"));
        _queueView.ItemsSource = _queueItems;
        root.Children.Add(_queueView);
        root.Children.Add(Heading("Live log preview (complete log is retained separately)"));
        root.Children.Add(_log);
        Content = new ScrollViewer { Content = root, Background = Brush("#FF111319") };

        _sessionLog.Write($"FFGrabber main window initialized; OS={RuntimeInformation.OSDescription}; architecture={RuntimeInformation.ProcessArchitecture}; appVersion={typeof(MainWindow).Assembly.GetName().Version}");
        _sessionLog.Write($"Tool paths: ffmpeg={FFmpegLocator.Find("ffmpeg") ?? "(not found)"}; ffprobe={FFmpegLocator.Find("ffprobe") ?? "(not found)"}; ffplay={FFmpegLocator.Find("ffplay") ?? "(not found)"}");
        AppendDisplayLog("Session log location: " + _sessionLog.CurrentLogPath);
        Opened += async (_, _) => await LoadAvailableCapabilitiesAsync();
        Closed += (_, _) => _sessionLog.Dispose();
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

    private static Control ButtonContent(string glyph, string label)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 15,
            VerticalAlignment = VerticalAlignment.Center
        });
        content.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        return content;
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
        _globalOptions.Text ?? string.Empty,
        _inputOptions.Text ?? string.Empty,
        _mappingOptions.Text ?? string.Empty,
        CombineOptionLines(_videoEncoderOptions.Text, BuildVideoGuideOptions()),
        CombineOptionLines(_audioEncoderOptions.Text, BuildAudioGuideOptions()),
        _filterOptions.Text ?? string.Empty,
        _muxerOptions.Text ?? string.Empty,
        CombineOptionLines(_outputOptions.Text, BuildColorMetadataOptions()),
        Selected(_inputFormat),
        Selected(_outputFormat),
        _additionalInputs.Text ?? string.Empty,
        _twoPass.IsChecked == true,
        _passLogFile.Text?.Trim() ?? string.Empty);

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
            SetChoices(_inputFormat, new[] { "auto" }.Concat(capabilities.Demuxers), "auto");
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
                    var temporaryFileScope = FFmpegTemporaryFileCleanupScope.Begin(job.OutputPath, cleanupArguments);
                    try
                    {
                        if (twoPassPlan is null)
                        {
                            var result = await FFmpegProcessRunner.RunAsync(
                                executable,
                                args,
                                onStandardOutput: line => LogProcessLine("FFMPEG STDOUT", line),
                                onStandardError: line => LogProcessLine("FFMPEG STDERR", line),
                                cancellationToken: _cancellation.Token);
                            AppendLog(result.ExitCode == 0
                                ? "Completed: " + job.OutputPath
                                : $"Failed (exit {result.ExitCode}): {job.OutputPath}");
                        }
                        else
                        {
                            AppendLog("Two-pass job: starting pass 1/2 (analysis output is discarded).");
                            var firstPass = await FFmpegProcessRunner.RunAsync(
                                executable,
                                twoPassPlan.FirstPassArguments,
                                onStandardOutput: line => LogProcessLine("FFMPEG PASS 1 STDOUT", line),
                                onStandardError: line => LogProcessLine("FFMPEG PASS 1 STDERR", line),
                                cancellationToken: _cancellation.Token);

                            if (firstPass.ExitCode != 0)
                            {
                                AppendLog($"Two-pass pass 1 failed with exit code {firstPass.ExitCode}; pass 2 was not started.");
                            }
                            else
                            {
                                AppendLog("Two-pass pass 1 succeeded; starting pass 2/2 (final output).");
                                var secondPass = await FFmpegProcessRunner.RunAsync(
                                    executable,
                                    twoPassPlan.SecondPassArguments,
                                    onStandardOutput: line => LogProcessLine("FFMPEG PASS 2 STDOUT", line),
                                    onStandardError: line => LogProcessLine("FFMPEG PASS 2 STDERR", line),
                                    cancellationToken: _cancellation.Token);
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
                            var cleanup = temporaryFileScope.CleanupCreatedFiles();
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
                    AppendLog("Job canceled by user.");
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

    private void Play(object? sender, RoutedEventArgs e)
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
            var info = new ProcessStartInfo(executable) { UseShellExecute = false };
            info.ArgumentList.Add("-hide_banner");
            info.ArgumentList.Add(_input.Text!);
            AppendLog("Launching: " + FFmpegCommandBuilder.ToDisplayCommand(executable, ["-hide_banner", _input.Text!]));
            Process.Start(info);
        }
        catch (Exception ex)
        {
            AppendLog("Could not launch ffplay: " + ex);
        }
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
