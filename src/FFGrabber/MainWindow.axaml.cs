using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Media;
using FFGrabber.Models;
using FFGrabber.Services;

namespace FFGrabber;

public partial class MainWindow : Window
{
    private readonly TextBox _input = new() { PlaceholderText = "Select source media", MinWidth = 420 };
    private readonly TextBox _output = new() { Watermark = "Output path including filename and extension", MinWidth = 420 };
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
    private readonly TextBox _preview = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, MinHeight = 52 };
    private readonly TextBox _log = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 160 };
    private readonly ObservableCollection<string> _queueItems = new();
    private readonly Queue<EncodingSettings> _jobs = new();
    private readonly ListBox _queueView = new() { MinHeight = 90 };
    private readonly Button _startButton = new() { Content = "Start queue", Classes = { "primary" } };
    private readonly Button _cancelButton = new() { Content = "Cancel active job", IsEnabled = false };
    private CancellationTokenSource? _cancellation;

    public MainWindow()
    {
        InitializeComponent();
        var root = new StackPanel { Spacing = 14, Margin = new Thickness(24) };
        root.Children.Add(new TextBlock { Text = "FFGrabber", Classes = { "pageTitle" } });
        root.Children.Add(new TextBlock
        {
            Text = "A native desktop frontend for FFmpeg, ffprobe and ffplay. Review each command before running it.",
            Foreground = Brush("#A4A9B6"), TextWrapping = TextWrapping.Wrap
        });

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
        settings.Children.Add(Row("Video codec", _videoCodec));
        settings.Children.Add(Row("Encoder preset", _preset));
        settings.Children.Add(Row("Rate control", _rateControl));
        var quality = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        quality.Children.Add(Label("CRF", 80)); quality.Children.Add(_crf);
        quality.Children.Add(Label("Video bitrate", 110)); quality.Children.Add(_videoBitrate);
        settings.Children.Add(quality);
        settings.Children.Add(Row("Output resolution", _size));
        var frame = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        frame.Children.Add(Label("Frame rate", 80)); frame.Children.Add(_frameRate);
        frame.Children.Add(Label("Pixel format", 110)); frame.Children.Add(_pixelFormat);
        settings.Children.Add(frame);
        settings.Children.Add(Row("Audio codec", _audioCodec));
        settings.Children.Add(Row("Audio bitrate", _audioBitrate));
        var checks = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18 };
        checks.Children.Add(_metadata); checks.Children.Add(_fastStart); checks.Children.Add(_overwrite);
        settings.Children.Add(checks);
        root.Children.Add(settingsPanel);

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
        root.Children.Add(Heading("Process log"));
        root.Children.Add(_log);
        Content = new ScrollViewer { Content = root };
    }

    private static ComboBox Combo(string[] values) => new() { ItemsSource = values, SelectedIndex = 0, MinWidth = 180 };
    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
    private static TextBlock Heading(string text) => new() { Text = text, Foreground = Brush("#C2F970"), FontWeight = FontWeight.SemiBold, FontSize = 15 };
    private static TextBlock Label(string text, double width) => new() { Text = text, Width = width, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Foreground = Brush("#C9CED9") };

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
        row.Children.Add(Label(label, 130));
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

    private static string Selected(ComboBox combo) => combo.SelectedItem?.ToString() ?? "";

    private EncodingSettings GetSettings() => new(
        _input.Text?.Trim() ?? "", _output.Text?.Trim() ?? "", Selected(_mode),
        Selected(_videoCodec), Selected(_preset),
        int.TryParse(_crf.Text, out var c) ? Math.Clamp(c, 0, 63) : 20,
        Selected(_audioCodec), _audioBitrate.Text?.Trim() ?? "",
        _overwrite.IsChecked == true, Selected(_rateControl), _videoBitrate.Text?.Trim() ?? "8M",
        Selected(_size), string.IsNullOrWhiteSpace(_frameRate.Text) ? "source" : _frameRate.Text.Trim(),
        Selected(_pixelFormat), _metadata.IsChecked == true, _fastStart.IsChecked == true);

    private void PreviewCommand(object? sender, RoutedEventArgs e)
    {
        try
        {
            var settings = GetSettings();
            _preview.Text = FFmpegCommandBuilder.ToDisplayCommand(
                FFmpegLocator.Find("ffmpeg") ?? "ffmpeg.exe", FFmpegCommandBuilder.Build(settings));
        }
        catch (Exception ex) { AppendLog("Preview error: " + ex.Message); }
    }

    private void AddToQueue(object? sender, RoutedEventArgs e)
    {
        try
        {
            var job = GetSettings();
            _ = FFmpegCommandBuilder.Build(job);
            _jobs.Enqueue(job);
            _queueItems.Add($"{_jobs.Count}. {Path.GetFileName(job.InputPath)} → {Path.GetFileName(job.OutputPath)} [{job.Mode}]");
            AppendLog("Queued: " + job.OutputPath);
            PreviewCommand(sender, e);
        }
        catch (Exception ex) { AppendLog("Cannot queue job: " + ex.Message); }
    }

    private async Task BrowseInputAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select source media", AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Media files") { Patterns = ["*.mp4", "*.mkv", "*.mov", "*.webm", "*.avi", "*.mp3", "*.wav", "*.flac", "*.m4a", "*.aac", "*.ts"] }, FilePickerFileTypes.All]
        });
        if (files.Count == 0) return;
        _input.Text = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        if (string.IsNullOrWhiteSpace(_output.Text))
            _output.Text = Path.Combine(Path.GetDirectoryName(_input.Text) ?? "", Path.GetFileNameWithoutExtension(_input.Text) + "_encoded.mkv");
    }

    private async Task BrowseOutputAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Choose output folder", AllowMultiple = false });
        if (folders.Count == 0) return;
        var folder = folders[0].TryGetLocalPath() ?? folders[0].Path.LocalPath;
        var filename = string.IsNullOrWhiteSpace(_input.Text) ? "output.mkv" : Path.GetFileNameWithoutExtension(_input.Text) + "_encoded.mkv";
        _output.Text = Path.Combine(folder, filename);
    }

    private async Task RunQueueAsync()
    {
        if (_jobs.Count == 0) { AppendLog("Queue is empty."); return; }
        var executable = FFmpegLocator.Find("ffmpeg");
        if (executable is null) { AppendLog("ffmpeg.exe was not found. Bundle FFmpeg or add its folder to PATH."); return; }
        _cancellation = new CancellationTokenSource();
        _startButton.IsEnabled = false; _cancelButton.IsEnabled = true;
        try
        {
            while (_jobs.Count > 0)
            {
                var job = _jobs.Dequeue();
                AppendLog("Starting: " + job.OutputPath);
                try
                {
                    var result = await FFmpegProcessRunner.RunAsync(executable, FFmpegCommandBuilder.Build(job),
                        onStandardError: line => Dispatcher.UIThread.Post(() => AppendLog(line)),
                        cancellationToken: _cancellation.Token);
                    AppendLog(result.ExitCode == 0 ? "Completed: " + job.OutputPath : $"Failed (exit {result.ExitCode}): {job.OutputPath}");
                }
                catch (OperationCanceledException) { AppendLog("Job canceled."); break; }
                catch (Exception ex) { AppendLog("Job error: " + ex.Message); }
            }
        }
        finally { _cancellation.Dispose(); _cancellation = null; _startButton.IsEnabled = true; _cancelButton.IsEnabled = false; }
    }

    private async Task ProbeAsync()
    {
        if (!File.Exists(_input.Text)) { AppendLog("Choose a valid input file first."); return; }
        var executable = FFmpegLocator.Find("ffprobe");
        if (executable is null) { AppendLog("ffprobe.exe was not found."); return; }
        AppendLog("ffprobe JSON metadata:");
        var result = await FFmpegProcessRunner.RunAsync(executable,
            ["-hide_banner", "-show_format", "-show_streams", "-of", "json", _input.Text!],
            onStandardOutput: line => Dispatcher.UIThread.Post(() => AppendLog(line)),
            onStandardError: line => Dispatcher.UIThread.Post(() => AppendLog(line)));
        AppendLog($"ffprobe exit code: {result.ExitCode}");
    }

    private void Play(object? sender, RoutedEventArgs e)
    {
        if (!File.Exists(_input.Text)) { AppendLog("Choose a valid input file first."); return; }
        var executable = FFmpegLocator.Find("ffplay");
        if (executable is null) { AppendLog("ffplay.exe was not found."); return; }
        try
        {
            var info = new ProcessStartInfo(executable) { UseShellExecute = false };
            info.ArgumentList.Add("-hide_banner"); info.ArgumentList.Add(_input.Text!);
            Process.Start(info);
        }
        catch (Exception ex) { AppendLog("Could not launch ffplay: " + ex.Message); }
    }

    private void AppendLog(string line)
    {
        _log.Text = string.IsNullOrEmpty(_log.Text) ? line : _log.Text + Environment.NewLine + line;
        if (_log.Text.Length > 30000) _log.Text = _log.Text[^30000..];
    }
}
