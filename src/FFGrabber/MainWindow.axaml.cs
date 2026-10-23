using System.Text;
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
    private readonly TextBox _input = new() { PlaceholderText = "Local file, URL, device expression, pipe, or FFmpeg input", MinWidth = 420 };
    private readonly TextBox _output = new() { PlaceholderText = "Output file path, URL, or FFmpeg target", MinWidth = 420 };
    private readonly ComboBox _mode = Combo(["Convert / encode", "Remux / stream copy", "Extract audio", "Video only"]);
    private readonly ComboBox _videoCodec = Combo(["libx264", "libx265", "libsvtav1", "copy"]);
    private readonly ComboBox _preset = Combo(["medium", "slow", "fast", "veryfast"]);
    private readonly ComboBox _rateControl = Combo(["CRF quality", "Target bitrate"]);
    private readonly TextBox _crf = new() { Text = "20", Width = 100 };
    private readonly TextBox _videoBitrate = new() { Text = "8M", Width = 100 };
    private readonly ComboBox _size = Combo(["source", "3840x2160", "2560x1440", "1920x1080", "1280x720", "854x480"]);
    private readonly TextBox _frameRate = new() { Text = "source", Width = 100 };
    private readonly ComboBox _pixelFormat = Combo(["auto", "yuv420p", "yuv420p10le", "yuv422p10le", "yuv444p10le"]);
    private readonly ComboBox _audioCodec = Combo(["aac", "copy", "libopus", "libmp3lame", "flac", "pcm_s16le"]);
    private readonly TextBox _audioBitrate = new() { Text = "192k", Width = 100 };
    private readonly CheckBox _metadata = new() { Content = "Preserve metadata and chapters", IsChecked = true };
    private readonly CheckBox _fastStart = new() { Content = "MP4/MOV fast start", IsChecked = true };
    private readonly CheckBox _overwrite = new() { Content = "Overwrite output", IsChecked = false };

    private readonly TextBox _globalOptions = MultiLine("-loglevel verbose -threads 0", 56);
    private readonly TextBox _inputOptions = MultiLine("-ss 00:00:05 -probesize 50M -analyzeduration 100M", 56);
    private readonly TextBox _mappingOptions = MultiLine("-map 0:v:0 -map 0:a? -map_metadata 0 -map_chapters 0", 56);
    private readonly TextBox _videoEncoderOptions = MultiLine(@"e.g. -profile:v high -level:v 5.1 -x264-params ""keyint=240:min-keyint=24""", 56);
    private readonly TextBox _audioEncoderOptions = MultiLine("-compression_level 8 -application audio", 56);
    private readonly TextBox _filterOptions = MultiLine(@"e.g. -vf ""scale=1920:-2"" OR -filter_complex ""[0:v]scale=1920:-2[v]""", 60);
    private readonly TextBox _muxerOptions = MultiLine("-f matroska -cluster_time_limit 1000", 56);
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

    public MainWindow()
    {
        InitializeComponent();
        Title = "FFGrabber — FFmpeg workstation";
        Width = 1260;
        Height = 1000;
        MinWidth = 900;
        MinHeight = 700;

        var root = new StackPanel { Spacing = 14, Margin = new Thickness(24) };
        root.Children.Add(new TextBlock { Text = "FFGrabber", Classes = { "pageTitle" } });
        root.Children.Add(new TextBlock
        {
            Text = "A Windows media workstation built around the real FFmpeg executables. Use the advanced fields for any documented FFmpeg option; the help browser queries the exact binary included with this installer.",
            Foreground = Brush("#A4A9B6"),
            TextWrapping = TextWrapping.Wrap
        });

        var topActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        topActions.Children.Add(Button("Options…", async (_, _) => await OpenOptionsAsync()));
        topActions.Children.Add(Button("FFmpeg options & capabilities…", async (_, _) => await OpenCapabilitiesAsync()));
        root.Children.Add(topActions);

        var sourcePanel = Panel("Source and destination");
        var sourceBody = PanelChildren(sourcePanel);
        var inputRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        inputRow.Children.Add(_input);
        inputRow.Children.Add(Button("Browse…", async (_, _) => await BrowseInputAsync()));
        sourceBody.Children.Add(inputRow);

        var outputRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        outputRow.Children.Add(_output);
        outputRow.Children.Add(Button("Choose folder…", async (_, _) => await BrowseOutputAsync()));
        sourceBody.Children.Add(outputRow);
        root.Children.Add(sourcePanel);

        var settingsPanel = Panel("Encoding settings");
        var settings = PanelChildren(settingsPanel);
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

        var checks = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18 };
        checks.Children.Add(_metadata);
        checks.Children.Add(_fastStart);
        checks.Children.Add(_overwrite);
        settings.Children.Add(checks);
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
        actions.Children.Add(Button("Preview command", PreviewCommand));
        actions.Children.Add(Button("Add to queue", AddToQueue, primary: true));
        _startButton.Click += async (_, _) => await RunQueueAsync();
        actions.Children.Add(_startButton);
        _cancelButton.Click += (_, _) => _cancellation?.Cancel();
        actions.Children.Add(_cancelButton);
        actions.Children.Add(Button("Inspect with ffprobe", async (_, _) => await ProbeAsync()));
        actions.Children.Add(Button("Play with ffplay", Play));
        root.Children.Add(actions);

        root.Children.Add(Heading("Command preview"));
        root.Children.Add(_preview);
        root.Children.Add(Heading("Queue"));
        _queueView.ItemsSource = _queueItems;
        root.Children.Add(_queueView);
        root.Children.Add(Heading("Live log preview (complete log is retained separately)"));
        root.Children.Add(_log);
        Content = new ScrollViewer { Content = root };

        _sessionLog.Write($"FFGrabber main window initialized; OS={RuntimeInformation.OSDescription}; architecture={RuntimeInformation.ProcessArchitecture}; appVersion={typeof(MainWindow).Assembly.GetName().Version}");
        _sessionLog.Write($"Tool paths: ffmpeg={FFmpegLocator.Find("ffmpeg") ?? "(not found)"}; ffprobe={FFmpegLocator.Find("ffprobe") ?? "(not found)"}; ffplay={FFmpegLocator.Find("ffplay") ?? "(not found)"}");
        AppendDisplayLog("Session log location: " + _sessionLog.CurrentLogPath);
        Closed += (_, _) => _sessionLog.Dispose();
    }

    private static ComboBox Combo(string[] values) => new() { ItemsSource = values, SelectedIndex = 0, MinWidth = 180 };
    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
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
        _videoEncoderOptions.Text ?? string.Empty,
        _audioEncoderOptions.Text ?? string.Empty,
        _filterOptions.Text ?? string.Empty,
        _muxerOptions.Text ?? string.Empty,
        _outputOptions.Text ?? string.Empty);

    private void PreviewCommand(object? sender, RoutedEventArgs e)
    {
        try
        {
            var settings = GetSettings();
            var executable = FFmpegLocator.Find("ffmpeg") ?? "ffmpeg.exe";
            var command = FFmpegCommandBuilder.ToDisplayCommand(executable, FFmpegCommandBuilder.Build(settings));
            _preview.Text = command;
            AppendLog("Command preview: " + command);
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
            _ = FFmpegCommandBuilder.Build(settings);
            _jobs.Enqueue(settings);
            var label = $"{_queueItems.Count + 1}. {Path.GetFileName(settings.InputPath)} → {Path.GetFileName(settings.OutputPath)} [{settings.Mode}]";
            _queueItems.Add(label);
            AppendLog("Queued: " + label);
            PreviewCommand(sender, e);
        }
        catch (Exception ex)
        {
            AppendLog("Cannot queue job: " + ex.Message);
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
            AppendLog("ffmpeg.exe was not found. Bundle the matching FFmpeg build or add its folder to PATH.");
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
                AppendLog("Starting output: " + job.OutputPath);
                try
                {
                    var args = FFmpegCommandBuilder.Build(job);
                    AppendLog("Running: " + FFmpegCommandBuilder.ToDisplayCommand(executable, args));
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
                catch (OperationCanceledException)
                {
                    AppendLog("Job canceled by user.");
                    break;
                }
                catch (Exception ex)
                {
                    AppendLog("Job error: " + ex);
                }
            }
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            _startButton.IsEnabled = true;
            _cancelButton.IsEnabled = false;
            AppendLog("Queue processing ended.");
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
            () => string.Join(Environment.NewLine, _queueItems),
            AppendLog);
        await options.ShowDialog(this);
    }

    private async Task OpenCapabilitiesAsync()
    {
        var window = new FFmpegCapabilitiesWindow(FFmpegLocator.Find("ffmpeg"));
        await window.ShowDialog(this);
    }

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
