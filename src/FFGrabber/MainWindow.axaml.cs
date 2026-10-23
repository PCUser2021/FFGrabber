using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FFGrabber.Models;
using FFGrabber.Services;

namespace FFGrabber;

public partial class MainWindow : Window
{
    private readonly TextBox _input = new() { Watermark = "Choose a source media file", MinWidth = 460 };
    private readonly TextBox _output = new() { Watermark = "Output path (include filename and extension)", MinWidth = 460 };
    private readonly ComboBox _mode = Combo(["Convert / encode", "Remux / stream copy", "Extract audio", "Video only"]);
    private readonly ComboBox _videoCodec = Combo(["libx264", "libx265", "libsvtav1", "copy"]);
    private readonly ComboBox _preset = Combo(["medium", "slow", "fast", "veryfast", "slowest"]);
    private readonly ComboBox _rateControl = Combo(["CRF quality", "Target bitrate"]);
    private readonly TextBox _crf = new() { Text = "20", Width = 100 };
    private readonly TextBox _videoBitrate = new() { Text = "8M", Width = 120 };
    private readonly ComboBox _size = Combo(["source", "3840x2160", "2560x1440", "1920x1080", "1280x720", "854x480"]);
    private readonly TextBox _frameRate = new() { Text = "source", Width = 130 };
    private readonly ComboBox _pixelFormat = Combo(["auto", "yuv420p", "yuv420p10le", "yuv422p10le", "yuv444p10le"]);
    private readonly ComboBox _audioCodec = Combo(["aac", "copy", "libopus", "libmp3lame", "flac", "pcm_s16le"]);
    private readonly TextBox _audioBitrate = new() { Text = "192k", Width = 120 };
    private readonly CheckBox _metadata = new() { Content = "Preserve metadata and chapters", IsChecked = true };
    private readonly CheckBox _fastStart = new() { Content = "MP4/MOV fast start", IsChecked = true };
    private readonly CheckBox _overwrite = new() { Content = "Overwrite output", IsChecked = false };
    private readonly TextBox _preview = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, MinHeight = 56 };
    private readonly TextBox _log = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 180 };
    private readonly ObservableCollection<string> _queueItems = [];
    private readonly ListBox _queueView = new() { ItemsSource = null, MinHeight = 100 };
    private readonly Queue<EncodingSettings> _jobs = new();
    private readonly Button _startButton = new() { Content = "Start queue", Classes = { "primary" } };
    private readonly Button _cancelButton = new() { Content = "Cancel active job", IsEnabled = false };
    private CancellationTokenSource? _cancellation;

    public MainWindow()
    {
        InitializeComponent();
        Title = "FFGrabber — FFmpeg workstation";
        var root = new StackPanel { Spacing = 14, Margin = new Thickness(24) };
        root.Children.Add(new TextBlock { Text = "FFGrabber", Classes = { "pageTitle" } });
        root.Children.Add(new TextBlock
        {
            Text = "A native desktop frontend for FFmpeg, ffprobe and ffplay. Inspect your command before running it.",
            Foreground = Brush("#A4A9B6"), TextWrapping = TextWrapping.Wrap
        });

        var filesPanel = Panel("Source and destination");
        var inputRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        inputRow.Children.Add(_input);
        inputRow.Children.Add(Button("Browse…", async (_, _) => await BrowseInputAsync()));
        filesPanel.Children.Add(inputRow);
        var outputRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 10, 0, 0) };
        outputRow.Children.Add(_output);
        outputRow.Children.Add(Button("Choose folder…", async (_, _) => await BrowseOutputAsync()));
        filesPanel.Children.Add(outputRow);
        root.Children.Add(filesPanel);

        var settings = Panel("Encoding settings");
        settings.Children.Add(Row("Operation", _mode));
        settings.Children.Add(Row("Video codec", _videoCodec));
        settings.Children.Add(Row("Encoder preset", _preset));
        settings.Children.Add(Row("Rate control", _rateControl));
        var qualityRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        qualityRow.Children.Add(new TextBlock { Text = "CRF", Width = 90, VerticalAlignment = VerticalAlignment.Center });
        qualityRow.Children.Add(_crf);
        qualityRow.Children.Add(new TextBlock { Text = "Video bitrate", Margin = new Thickness(20, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        qualityRow.Children.Add(_videoBitrate);
        settings.Children.Add(qualityRow);
        settings.Children.Add(Row("Output resolution", _size));
        var frameRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        frameRow.Children.Add(new TextBlock { Text = "Frame rate", Width = 90, VerticalAlignment = VerticalAlignment.Center });
        frameRow.Children.Add(_frameRate);
        frameRow.Children.Add(new TextBlock { Text = "Pixel format", Margin = new Thickness(20, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        frameRow.Children.Add(_pixelFormat);
        settings.Children.Add(frameRow);
        settings.Children.Add(Row("Audio codec", _audioCodec));
        settings.Children.Add(Row("Audio bitrate", _audioBitrate));
        var checkRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20 };
        checkRow.Children.Add(_metadata); checkRow.Children.Add(_fastStart); checkRow.Children.Add(_overwrite);
        settings.Children.Add(checkRow);
        root.Children.Add(settings);

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

        root.Children.Add(new TextBlock { Text = "Command preview", Foreground = Brush("#C2F970"), FontWeight = FontWeight.SemiBold });
        root.Children.Add(_preview);
        root.Children.Add(new TextBlock { Text = "Queue", Foreground = Brush("#C2F970"), FontWeight = FontWeight.SemiBold });
        _queueView.ItemsSource = _queueItems;
        root.Children.Add(_queueView);
        root.Children.Add(new TextBlock { Text = "Process log", Foreground = Brush("#C2F970"), FontWeight = FontWeight.SemiBold });
        root.Children.Add(_log);
        Content = new ScrollViewer { Content = root };
    }

    private static ComboBox Combo(string[] options) => new()
    {
        ItemsSource = options, SelectedIndex = 0, MinWidth = 180
    };

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

    private static Border Panel(string heading)
    {
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(new TextBlock { Text = heading, Foreground = Brush("#F1F3F8"), FontWeight = FontWeight.SemiBold, FontSize = 15 });
        return new Border { Classes = { "panel" }, Child = stack, Tag = stack };
    }

    private static StackPanel PanelChildren(Border panel) => (StackPanel)panel.Child!;

    private static Border Panel(string heading, out StackPanel content)
    {
        content = new StackPanel { Spacing = 10 };
        content.Children.Add(new TextBlock { Text = heading, Foreground = Brush("#F1F3F8"), FontWeight = FontWeight.SemiBold, FontSize = 15 });
        return new Border { Classes = { "panel" }, Child = content };
    }

    private static StackPanel Row(string label, Control control)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock { Text = label, Width = 130, VerticalAlignment = VerticalAlignment.Center, Foreground = Brush("#C9CED9") });
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

    private EncodingSettings BuildSettings() => new(
        _input.Text?.Trim() ?? "", _output.Text?.Trim() ?? "",
        Selected(_mode), Selected(_videoCodec), Selected(_preset),
        int.TryParse(_crf.Text, out var crf) ? Math.Clamp(crf, 0, 63) : 20,
        Selected(_audioCodec), _audioBitrate.Text?.Trim() ?? "",
        _overwrite.IsChecked == true, Selected(_rateControl),
        _videoBitrate.Text?.Trim() ?? "8M", Selected(_size),
        string.IsNullOrWhiteSpace(_frameRate.Text) ? "source" : _frameRate.Text.Trim(),
        Selected(_pixelFormat), _metadata.IsChecked == true, _fastStart.IsChecked == true);

    private static string Selected(ComboBox combo) => combo.SelectedItem?.ToString() ?? "";

    private void PreviewCommand(object? sender, RoutedEventArgs e)
    {
        try
        {
            var settings = BuildSettings();
            var executable = FFmpegLocator.Find("ffmpeg") ?? "ffmpeg.exe";
            _preview.Text = FFmpegCommandBuilder.ToDisplayCommand(executable, FFmpegCommandBuilder.Build(settings));
        }
        catch (Exception ex) { AppendLog("Preview error: " + ex.Message); }
    }

    private void AddToQueue(object? sender, RoutedEventArgs e)
    {
        try
        {
            var job = BuildSettings();
            _ = FFmpegCommandBuilder.Build(job);
            _jobs.Enqueue(job);
            _queueItems.Add($"{_jobs.Count}. {Path.GetFileName(job.InputPath)}  →  {Path.GetFileName(job.OutputPath)}  [{job.Mode}]");
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
            FileTypeFilter = [new FilePickerFileType("Media files") { Patterns = ["*.mp4", "*.mkv", "*.mov", "*.webm", "*.avi", "*.m4v", "*.mp3", "*.wav", "*.flac", "*.m4a", "*.aac", "*.ts", "*.mts"] },
                FilePickerFileTypes.All]
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
        var name = string.IsNullOrWhiteSpace(_input.Text) ? "output.mkv" : Path.GetFileNameWithoutExtension(_input.Text) + "_encoded.mkv";
        _output.Text = Path.Combine(folder, name);
    }

    private async Task RunQueueAsync()
    {
        if (_jobs.Count == 0) { AppendLog("Queue is empty."); return; }
        var executable = FFmpegLocator.Find("ffmpeg");
        if (executable is null) { AppendLog("ffmpeg.exe was not found. Bundle FFmpeg, add it to PATH, or install a matching build."); return; }

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
                    var args = FFmpegCommandBuilder.Build(job);
                    var result = await FFmpegProcessRunner.RunAsync(executable, args,
                        onStandardError: line => Dispatcher.UIThread.Post(() => AppendLog(line)),
                        cancellationToken: _cancellation.Token);
                    AppendLog(result.ExitCode == 0 ? "Completed: " + job.OutputPath : $"Failed (exit {result.ExitCode}): {job.OutputPath}");
                }
                catch (OperationCanceledException) { AppendLog("Job canceled."); break; }
                catch (Exception ex) { AppendLog("Job error: " + ex.Message); }
            }
        }
        finally
        {
            _cancellation.Dispose(); _cancellation = null;
            _startButton.IsEnabled = true; _cancelButton.IsEnabled = false;
        }
    }

    private async Task ProbeAsync()
    {
        if (!File.Exists(_input.Text)) { AppendLog("Choose a valid input file first."); return; }
        var executable = FFmpegLocator.Find("ffprobe");
        if (executable is null) { AppendLog("ffprobe.exe was not found."); return; }
        AppendLog("ffprobe metadata:");
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
