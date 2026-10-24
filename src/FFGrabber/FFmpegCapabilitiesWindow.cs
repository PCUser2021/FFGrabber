using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using System.Diagnostics;
using Avalonia.Layout;
using System.Text;
using Avalonia.Media;
using FFGrabber.Services;

namespace FFGrabber;

public sealed class FFmpegCapabilitiesWindow : Window
{
    private static readonly (string Label, string[] Arguments)[] Catalogs =
    [
        ("Basic tool help — -h", ["-h"]),
        ("Advanced tool options — -h long", ["-h", "long"]),
        ("Complete option catalogue — -h full", ["-h", "full"]),

        ("Encoders — -encoders", ["-encoders"]),
        ("Decoders — -decoders", ["-decoders"]),
        ("All codecs — -codecs", ["-codecs"]),
        ("Muxers — -muxers", ["-muxers"]),
        ("Demuxers — -demuxers", ["-demuxers"]),
        ("Formats — -formats", ["-formats"]),
        ("Filters — -filters", ["-filters"]),
        ("Bitstream filters — -bsfs", ["-bsfs"]),
        ("Protocols — -protocols", ["-protocols"]),
        ("Devices — -devices", ["-devices"]),
        ("Hardware accelerators — -hwaccels", ["-hwaccels"]),
        ("Pixel formats — -pix_fmts", ["-pix_fmts"]),
        ("Sample formats — -sample_fmts", ["-sample_fmts"]),
        ("Channel layouts — -layouts", ["-layouts"]),
        ("Named colors — -colors", ["-colors"]),
        ("Stream dispositions — -dispositions", ["-dispositions"]),
        ("FFmpeg version and library versions", ["-version"]),
        ("FFmpeg build configuration", ["-buildconf"])
    ];

    private readonly string? _ffmpegPath;
    private readonly ComboBox _catalog = new() { ItemsSource = Catalogs.Select(c => c.Label).ToArray(), SelectedIndex = 0, MinWidth = 360 };
    private readonly TextBox _helpQuery = new() { PlaceholderText = "Detailed topic, e.g. encoder=libx265, muxer=matroska, demuxer=mov, filter=scale", MinWidth = 450 };
    private readonly TextBox _filter = new() { PlaceholderText = "Filter displayed help text", MinWidth = 300 };
    private readonly TextBox _output = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.NoWrap,
        FontFamily = new FontFamily("Consolas"),
        FontSize = 12,
        MinHeight = 460
    };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightGray };
    private string _fullOutput = string.Empty;

    public FFmpegCapabilitiesWindow(string? ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
        Title = "FFGrabber — FFmpeg options and capabilities";
        Width = 1060;
        Height = 760;
        MinWidth = 760;
        MinHeight = 520;
        Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Mica, WindowTransparencyLevel.Transparent];
        TransparencyBackgroundFallback = Brush("#FF111319");

        var root = new StackPanel { Spacing = 10, Margin = new Thickness(18) };
        root.Children.Add(new TextBlock { Text = "FFmpeg Options & Capabilities", FontSize = 22, FontWeight = FontWeight.SemiBold });
        root.Children.Add(new TextBlock
        {
            Text = "Search and export the options exposed by your imported FFmpeg executable. “-h full” returns the complete option catalogue, including component-specific options; separate lists reflect what is compiled into this exact build.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#A4A9B6"))
        });

        var catalogRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        catalogRow.Children.Add(_catalog);
        catalogRow.Children.Add(Button("Load catalog", async (_, _) => await LoadSelectedAsync()));
        root.Children.Add(catalogRow);

        var detailRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        detailRow.Children.Add(_helpQuery);
        detailRow.Children.Add(Button("Show detailed help", async (_, _) => await LoadDetailAsync()));
        root.Children.Add(detailRow);

        var filterRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        filterRow.Children.Add(_filter);
        filterRow.Children.Add(Button("Filter text", (_, _) => ApplyFilter()));
        filterRow.Children.Add(Button("Clear filter", (_, _) => { _filter.Text = ""; ApplyFilter(); }));
        root.Children.Add(filterRow);

        var exportRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Left };
        exportRow.Children.Add(Button("Copy displayed text", async (_, _) => await CopyDisplayedTextAsync()));
        exportRow.Children.Add(Button("Save complete help…", async (_, _) => await SaveFullHelpAsync()));
        exportRow.Children.Add(Button("Official documentation ↗", (_, _) => OpenOfficialDocumentation()));
        root.Children.Add(exportRow);

        _helpQuery.KeyDown += async (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            await LoadDetailAsync();
        };
        _filter.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            ApplyFilter();
        };
        _filter.TextChanged += (_, _) => ApplyFilter();

        root.Children.Add(_status);
        root.Children.Add(_output);
        root.Children.Add(Button("Close", (_, _) => Close()));
        Content = new Border { Classes = { "glassShell" }, Child = new ScrollViewer { Content = root, Background = Brushes.Transparent } };
        Opened += async (_, _) => await LoadSelectedAsync();
    }

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

    private static Button Button(string text, EventHandler<RoutedEventArgs> onClick)
    {
        var button = new Button { Content = text };
        button.Click += onClick;
        return button;
    }

    private async Task LoadSelectedAsync()
    {
        var item = Catalogs.FirstOrDefault(c => c.Label == _catalog.SelectedItem?.ToString());
        if (item.Arguments is null)
            return;
        await RunHelpAsync(item.Arguments, item.Label);
    }

    private async Task LoadDetailAsync()
    {
        var query = _helpQuery.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            _status.Text = "Enter an FFmpeg help topic, such as encoder=libx265 or filter=scale.";
            return;
        }

        IReadOnlyList<string> tokens;
        try
        {
            tokens = FFmpegArgumentParser.Parse(query);
        }
        catch (ArgumentException ex)
        {
            _status.Text = ex.Message;
            return;
        }

        if (tokens.Count == 0 || tokens.Any(token => token is "-i" or "-o" or "-y" or "-n"))
        {
            _status.Text = "Enter a help topic, not input/output or overwrite flags.";
            return;
        }

        await RunHelpAsync(["-h", .. tokens], $"Detailed help: {query}");
    }

    private async Task RunHelpAsync(IReadOnlyList<string> arguments, string label)
    {
        if (string.IsNullOrWhiteSpace(_ffmpegPath) || !File.Exists(_ffmpegPath))
        {
            _status.Text = "No FFmpeg imported. Close this window and use \"Import FFmpeg…\" on the main window.";
            _output.Text = string.Empty;
            return;
        }

        _status.Text = $"Loading {label}…";
        _output.Text = string.Empty;
        try
        {
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var result = await FFmpegProcessRunner.RunAsync(
                _ffmpegPath,
                arguments,
                onStandardOutput: line => stdout.AppendLine(line),
                onStandardError: line => stderr.AppendLine(line));
            var pieces = new List<string>();
            if (stdout.Length > 0) pieces.Add("----- stdout -----" + Environment.NewLine + stdout);
            if (stderr.Length > 0) pieces.Add("----- stderr -----" + Environment.NewLine + stderr);
            _fullOutput = $"$ {FFmpegCommandBuilder.ToDisplayCommand(_ffmpegPath, arguments)}{Environment.NewLine}Exit code: {result.ExitCode}{Environment.NewLine}{string.Join(Environment.NewLine, pieces)}";
            _filter.Text = string.Empty;
            _output.Text = _fullOutput;
            _status.Text = $"Loaded {label}. Exit code: {result.ExitCode}. This build's own help is the authority for supported options.";
        }
        catch (Exception ex)
        {
            _fullOutput = ex.ToString();
            _output.Text = _fullOutput;
            _status.Text = "Could not query FFmpeg; see the text below for the error.";
        }
    }

    private async Task CopyDisplayedTextAsync()
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null)
            {
                _status.Text = "Clipboard access is unavailable on this platform.";
                return;
            }
            await clipboard.SetTextAsync(_output.Text ?? string.Empty);
            _status.Text = "Displayed help text copied to the clipboard.";
        }
        catch (Exception ex)
        {
            _status.Text = "Could not copy help text: " + ex.Message;
        }
    }

    private async Task SaveFullHelpAsync()
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save complete FFmpeg help output",
                SuggestedFileName = "FFGrabber-FFmpeg-help.txt",
                DefaultExtension = "txt",
                ShowOverwritePrompt = true,
                FileTypeChoices = [new FilePickerFileType("Text file") { Patterns = ["*.txt"] }]
            });
            if (file is null)
            {
                _status.Text = "Help export cancelled.";
                return;
            }

            var path = file.TryGetLocalPath() ?? file.Path.LocalPath;
            if (string.IsNullOrWhiteSpace(path))
                throw new IOException("The selected destination is not a local file path.");
            if (!string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
                path += ".txt";

            await File.WriteAllTextAsync(path, _fullOutput, new UTF8Encoding(false));
            _status.Text = "Saved complete help output to: " + path;
        }
        catch (Exception ex)
        {
            _status.Text = "Could not save help output: " + ex.Message;
        }
    }

    private void OpenOfficialDocumentation()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://ffmpeg.org/ffmpeg.html") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _status.Text = "Could not open the official documentation: " + ex.Message;
        }
    }

    private void ApplyFilter()
    {
        var query = _filter.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            _output.Text = _fullOutput;
            return;
        }

        var filtered = _fullOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Where(line => line.Contains(query, StringComparison.OrdinalIgnoreCase));
        _output.Text = string.Join(Environment.NewLine, filtered);
        _status.Text = $"Showing lines containing '{query}'. Clear the filter to restore the complete output.";
    }
}
