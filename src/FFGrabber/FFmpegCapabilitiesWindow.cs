using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System.Text;
using Avalonia.Media;
using FFGrabber.Services;

namespace FFGrabber;

public sealed class FFmpegCapabilitiesWindow : Window
{
    private static readonly (string Label, string[] Arguments)[] Catalogs =
    [
        ("All generic options — -h full", ["-h", "full"]),
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
        Background = new SolidColorBrush(Color.Parse("#111319"));

        var root = new StackPanel { Spacing = 10, Margin = new Thickness(18) };
        root.Children.Add(new TextBlock { Text = "FFmpeg Options & Capabilities", FontSize = 22, FontWeight = FontWeight.SemiBold });
        root.Children.Add(new TextBlock
        {
            Text = "This browser queries your installed FFmpeg binary. Lists and detailed help reflect the codecs, formats, filters and options actually compiled into that build.",
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

        root.Children.Add(_status);
        root.Children.Add(_output);
        root.Children.Add(Button("Close", (_, _) => Close()));
        Content = new ScrollViewer { Content = root };
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
