using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using FFGrabber.Services;

namespace FFGrabber;

/// <summary>
/// Searchable visual composer for flags reported by the user's imported FFmpeg build.
/// Options are appended to the selected argument stage in the main window; the normal
/// command preview remains the final source of truth for command ordering.
/// </summary>
public sealed class FFmpegOptionComposerWindow : Window
{
    private sealed record OptionEntry(string Name, string HelpLine, bool IsRunnable = true, string? RecommendedScope = null);

    private static readonly Regex OptionNamePattern = new(
        @"^\s{0,8}(?<name>-{1,2}[A-Za-z0-9][A-Za-z0-9_.:+-]*)(?<tail>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string? _ffmpegPath;
    private readonly string _videoEncoder;
    private readonly string _audioEncoder;
    private readonly string _inputDemuxer;
    private readonly string _outputMuxer;
    private readonly Action<string, string> _appendOption;

    private readonly ComboBox _scope = new()
    {
        ItemsSource = new[]
        {
            "Global / logging options",
            "Input / demuxer options",
            "Additional input source",
            "Stream mapping / selection",
            "Video encoder options",
            "Audio encoder options",
            "Filters / filtergraph",
            "Muxer / container options",
            "Other output options"
        },
        SelectedIndex = 0,
        MinWidth = 260
    };

    private readonly ComboBox _catalogSource = new()
    {
        ItemsSource = new[]
        {
            "Full tool option catalogue (-h full)",
            "Current video encoder",
            "Current audio encoder",
            "Current input demuxer",
            "Current output muxer",
            "Custom component topic",
            "Options from all 14 encoding guide text files"
        },
        SelectedIndex = 0,
        MinWidth = 300
    };

    private readonly TextBox _componentTopic = new()
    {
        PlaceholderText = "Component topic: encoder=libx265, filter=scale, muxer=hls, demuxer=mov, bsf=h264_mp4toannexb",
        MinWidth = 420
    };

    private readonly TextBox _search = new()
    {
        PlaceholderText = "Search option name or help text…",
        MinWidth = 340
    };

    private readonly ListBox _catalog = new() { MinHeight = 300 };
    private readonly TextBox _optionName = new()
    {
        Text = "-",
        PlaceholderText = "FFmpeg option name, e.g. -movflags",
        MinWidth = 240
    };
    private readonly TextBox _optionArguments = new()
    {
        PlaceholderText = "Optional value / argument tokens; quote values with spaces",
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        MinHeight = 64,
        MinWidth = 400
    };
    private readonly TextBlock _description = new()
    {
        TextWrapping = TextWrapping.Wrap,
        Foreground = ThemePreferences.Brush("#A4A9B6")
    };
    private readonly TextBlock _status = new()
    {
        TextWrapping = TextWrapping.Wrap,
        Foreground = ThemePreferences.Brush("#A4A9B6")
    };

    private bool _selectedOptionIsRunnable = true;
    private IReadOnlyList<OptionEntry> _allOptions = Array.Empty<OptionEntry>();
    private IReadOnlyList<OptionEntry> _visibleOptions = Array.Empty<OptionEntry>();

    public FFmpegOptionComposerWindow(
        string? ffmpegPath,
        string videoEncoder,
        string audioEncoder,
        string inputDemuxer,
        string outputMuxer,
        Action<string, string> appendOption)
    {
        _ffmpegPath = ffmpegPath;
        _videoEncoder = videoEncoder;
        _audioEncoder = audioEncoder;
        _inputDemuxer = inputDemuxer;
        _outputMuxer = outputMuxer;
        _appendOption = appendOption ?? throw new ArgumentNullException(nameof(appendOption));

        Title = "FFGrabber — Complete FFmpeg Settings Editor";
        Width = 1120;
        Height = 820;
        MinWidth = 820;
        MinHeight = 640;
        Background = ThemePreferences.Brush("#FF111319");

        var root = new StackPanel { Spacing = 12, Margin = new Thickness(18) };
        root.Children.Add(new TextBlock
        {
            Text = "Searchable FFmpeg settings editor",
            FontSize = 24,
            FontWeight = FontWeight.SemiBold
        });
        root.Children.Add(new TextBlock
        {
            Text = "Load the full option catalogue or private options for a component from the FFmpeg executable you imported. Select an option, enter its value, choose the argument stage, then add it directly to the matching advanced field in the main GUI. You can also type an option not listed here.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemePreferences.Brush("#A4A9B6")
        });

        var sourceRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        sourceRow.Children.Add(_catalogSource);
        sourceRow.Children.Add(Button("Load catalogue", async (_, _) => await LoadSelectedCatalogueAsync()));
        sourceRow.Children.Add(Button("Load full tool options", async (_, _) =>
            await LoadCatalogueAsync(["-hide_banner", "-h", "full"], "Full tool option catalogue (-h full)")));
        root.Children.Add(sourceRow);

        var componentRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        componentRow.Children.Add(_componentTopic);
        componentRow.Children.Add(Button("Load custom component", async (_, _) => await LoadCustomComponentAsync()));
        root.Children.Add(componentRow);

        var searchRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        searchRow.Children.Add(_search);
        searchRow.Children.Add(Button("Clear search", (_, _) => _search.Text = string.Empty));
        root.Children.Add(searchRow);

        _catalog.SelectionChanged += (_, _) => SelectCatalogEntry();
        _search.TextChanged += (_, _) => ApplySearch();
        _componentTopic.KeyDown += async (_, e) =>
        {
            if (e.Key != Avalonia.Input.Key.Enter) return;
            e.Handled = true;
            await LoadCustomComponentAsync();
        };

        root.Children.Add(_catalog);

        var settingPanel = new Border
        {
            Classes = { "panel" },
            Child = new StackPanel { Spacing = 10 }
        };
        var settingBody = (StackPanel)settingPanel.Child!;
        settingBody.Children.Add(new TextBlock
        {
            Text = "Add option to the main GUI",
            FontSize = 16,
            FontWeight = FontWeight.SemiBold
        });
        settingBody.Children.Add(_description);
        settingBody.Children.Add(new TextBlock { Text = "Option name" });
        settingBody.Children.Add(_optionName);
        settingBody.Children.Add(new TextBlock { Text = "Value / argument tokens (optional)" });
        settingBody.Children.Add(_optionArguments);
        settingBody.Children.Add(new TextBlock { Text = "Apply at this argument stage" });
        settingBody.Children.Add(_scope);

        var actionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actionRow.Children.Add(Button("Add setting to main GUI", (_, _) => AddSelectedOption(), primary: true));
        actionRow.Children.Add(Button("Clear value", (_, _) => _optionArguments.Text = string.Empty));
        actionRow.Children.Add(Button("Official FFmpeg reference ↗", (_, _) => OpenUrl("https://ffmpeg.org/ffmpeg-all.html")));
        settingBody.Children.Add(actionRow);
        root.Children.Add(settingPanel);
        root.Children.Add(_status);
        root.Children.Add(Button("Close", (_, _) => Close()));

        Content = new Border
        {
            Classes = { "glassShell" },
            Child = new ScrollViewer { Content = root, Background = ThemePreferences.Brush("#FF111319") }
        };

        Opened += async (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_ffmpegPath) && File.Exists(_ffmpegPath))
                await LoadCatalogueAsync(["-hide_banner", "-h", "full"], "Full tool option catalogue (-h full)");
            else
                _status.Text = "No FFmpeg executable imported. You can still type any option manually; importing FFmpeg enables the live build-specific catalogue.";
        };
    }

    private async Task LoadSelectedCatalogueAsync()
    {
        var source = _catalogSource.SelectedItem?.ToString() ?? string.Empty;
        switch (source)
        {
            case "Full tool option catalogue (-h full)":
                await LoadCatalogueAsync(["-hide_banner", "-h", "full"], source);
                break;
            case "Current video encoder":
                await LoadComponentAsync("encoder=" + _videoEncoder);
                break;
            case "Current audio encoder":
                await LoadComponentAsync("encoder=" + _audioEncoder);
                break;
            case "Current input demuxer":
                await LoadComponentAsync("demuxer=" + _inputDemuxer);
                break;
            case "Current output muxer":
                await LoadComponentAsync("muxer=" + _outputMuxer);
                break;
            case "Custom component topic":
                await LoadCustomComponentAsync();
                break;
            case "Options from all 14 encoding guide text files":
                LoadGuideOptionIndex();
                break;
            default:
                _status.Text = "Choose a catalogue source.";
                break;
        }
    }

    private async Task LoadCustomComponentAsync()
    {
        var topic = _componentTopic.Text?.Trim();
        if (string.IsNullOrWhiteSpace(topic))
        {
            _status.Text = "Enter a component topic, for example filter=scale or encoder=libx265.";
            return;
        }

        IReadOnlyList<string> tokens;
        try
        {
            tokens = FFmpegArgumentParser.Parse(topic);
        }
        catch (ArgumentException ex)
        {
            _status.Text = ex.Message;
            return;
        }

        if (tokens.Count != 1 || tokens[0].StartsWith("-", StringComparison.Ordinal))
        {
            _status.Text = "Enter one component topic such as encoder=libx265, filter=scale, muxer=hls, demuxer=mov, decoder=h264, bsf=h264_mp4toannexb, or protocol=https.";
            return;
        }

        await LoadComponentAsync(tokens[0]);
    }

    private async Task LoadComponentAsync(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic) || topic.EndsWith("=", StringComparison.Ordinal))
        {
            _status.Text = "This component selection has no usable topic. Choose a component present in the imported build.";
            return;
        }

        await LoadCatalogueAsync(["-hide_banner", "-h", topic], "Component options: " + topic);
    }

    private async Task LoadCatalogueAsync(IReadOnlyList<string> arguments, string label)
    {
        if (string.IsNullOrWhiteSpace(_ffmpegPath) || !File.Exists(_ffmpegPath))
        {
            _status.Text = "No valid imported FFmpeg executable. Enter an option manually or import FFmpeg first.";
            return;
        }

        _status.Text = "Loading " + label + "…";
        _catalog.IsEnabled = false;
        _catalogSource.IsEnabled = false;

        try
        {
            var output = new StringBuilder();
            var result = await FFmpegProcessRunner.RunAsync(
                _ffmpegPath,
                arguments,
                onStandardOutput: line => output.AppendLine(line),
                onStandardError: line => output.AppendLine(line));

            var parsed = ParseOptions(output.ToString());
            _allOptions = parsed;
            ApplySearch();
            _status.Text = $"Loaded {label}: {parsed.Count:N0} distinct options detected. Exit code: {result.ExitCode}. The catalogue is specific to this imported FFmpeg build.";
            if (parsed.Count == 0)
                _status.Text += " This topic may not be supported by the selected build; check the detailed help output or type the option manually.";
        }
        catch (Exception ex)
        {
            _status.Text = "Could not load the FFmpeg options: " + ex.Message;
        }
        finally
        {
            _catalog.IsEnabled = true;
            _catalogSource.IsEnabled = true;
        }
    }

    private void LoadGuideOptionIndex()
    {
        _allOptions = EncodingGuideOptionIndex.All
            .Select(option =>
            {
                var nonRunnableGroups = new[]
                {
                    "Build capability note",
                    "Tool inspection only",
                    "External tool syntax only",
                    "Probe / diagnostic"
                };
                var runnable = !nonRunnableGroups.Contains(option.Group, StringComparer.OrdinalIgnoreCase);
                return new OptionEntry(
                    option.Name,
                    $"{option.Name}  [{option.Group}]  {option.Description}  | Source: {option.SourceFiles}",
                    runnable,
                    RecommendScope(option.Name, option.Group, option.Description));
            })
            .ToArray();
        _search.Text = string.Empty;
        ApplySearch();
        _status.Text = $"Loaded {_allOptions.Count:N0} unique option entries extracted from {EncodingGuideOptionIndex.SourceFiles.Count:N0} Dropbox .txt guides. Build-time flags are explanatory only.";
    }

    private static string? RecommendScope(string optionName, string group, string description)
    {
        if (string.Equals(group, "Build capability note", StringComparison.OrdinalIgnoreCase)
            || string.Equals(group, "Tool inspection only", StringComparison.OrdinalIgnoreCase)
            || string.Equals(group, "External tool syntax only", StringComparison.OrdinalIgnoreCase)
            || string.Equals(group, "Probe / diagnostic", StringComparison.OrdinalIgnoreCase))
            return null;

        if (optionName.Equals("-i", StringComparison.OrdinalIgnoreCase))
            return "Additional input source";

        if (group.Contains("Stream selection", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Metadata / stream", StringComparison.OrdinalIgnoreCase)
            || optionName is "-map" or "-map_metadata" or "-map_chapters" or "-an")
            return "Stream mapping / selection";

        if (group.Contains("Audio", StringComparison.OrdinalIgnoreCase)
            || optionName is "-ar" or "-ac" or "-b:a" or "-q:a" or "-qscale:a"
                or "-profile:a" or "-cutoff" or "-vbr" or "-aq")
            return "Audio encoder options";

        if (group.Contains("Input", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Capture", StringComparison.OrdinalIgnoreCase)
            || optionName is "-ss" or "-t" or "-to" or "-re" or "-framerate"
                or "-video_size" or "-list_formats" or "-list-formats-ext"
                or "-hwaccel" or "-probesize")
            return "Input / demuxer options";

        if (group.Contains("Filter", StringComparison.OrdinalIgnoreCase)
            || optionName is "-vf" or "-filter_complex")
            return "Filters / filtergraph";

        if (group.Contains("Container", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Streaming", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Compatibility", StringComparison.OrdinalIgnoreCase)
            || group.Contains("HDR", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Multiplexing", StringComparison.OrdinalIgnoreCase)
            || optionName is "-f" or "-movflags" or "-metadata" or "-metadata:s:v:0" or "-tag:v" or "-vtag" or "-vendor")
            return "Muxer / container options";

        if (group.Contains("Global", StringComparison.OrdinalIgnoreCase)
            || optionName is "-y" or "-loglevel" or "-report")
            return "Global / logging options";

        if (group.Contains("Video", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Rate control", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Quantizer", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Keyframe", StringComparison.OrdinalIgnoreCase)
            || group.Contains("GOP", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Parallelism", StringComparison.OrdinalIgnoreCase)
            || group.Contains("VFX", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Hardware encoding", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Speed", StringComparison.OrdinalIgnoreCase)
            || group.Contains("Profile", StringComparison.OrdinalIgnoreCase)
            || group.Contains("FFV1", StringComparison.OrdinalIgnoreCase))
            return "Video encoder options";

        _ = description;
        return null;
    }

    private static IReadOnlyList<OptionEntry> ParseOptions(string help)
    {
        var entries = new Dictionary<string, OptionEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in help.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var match = OptionNamePattern.Match(line);
            if (!match.Success)
                continue;

            var name = match.Groups["name"].Value;
            var tail = match.Groups["tail"].Value.Trim();
            // Avoid treating unindented examples/headers as command-line option rows.
            if (!name.StartsWith("-", StringComparison.Ordinal) || name.Length < 2)
                continue;

            if (!entries.ContainsKey(name))
                entries.Add(name, new OptionEntry(name, line.Trim()));
            else if (tail.Length > 0 && entries[name].HelpLine.Length < line.Trim().Length)
                entries[name] = new OptionEntry(name, line.Trim());
        }

        return entries.Values
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void ApplySearch()
    {
        var query = _search.Text?.Trim();
        _visibleOptions = string.IsNullOrWhiteSpace(query)
            ? _allOptions
            : _allOptions.Where(entry =>
                entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || entry.HelpLine.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();

        _catalog.ItemsSource = _visibleOptions
            .Select(entry => entry.HelpLine)
            .ToArray();

        _status.Text = string.IsNullOrWhiteSpace(query)
            ? $"Showing {_visibleOptions.Count:N0} options."
            : $"Showing {_visibleOptions.Count:N0} of {_allOptions.Count:N0} options matching “{query}”.";
    }

    private void SelectCatalogEntry()
    {
        var index = _catalog.SelectedIndex;
        if (index < 0 || index >= _visibleOptions.Count)
            return;

        var entry = _visibleOptions[index];
        _optionName.Text = entry.Name;
        _description.Text = entry.HelpLine;
        _selectedOptionIsRunnable = entry.IsRunnable;
        if (entry.RecommendedScope is not null)
        {
            var scopes = _scope.ItemsSource as IEnumerable<string> ?? Array.Empty<string>();
            if (scopes.Contains(entry.RecommendedScope, StringComparer.OrdinalIgnoreCase))
                _scope.SelectedItem = entry.RecommendedScope;
        }
    }

    private void AddSelectedOption()
    {
        var optionName = _optionName.Text?.Trim() ?? string.Empty;
        var optionArguments = _optionArguments.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(optionName) || !optionName.StartsWith("-", StringComparison.Ordinal) || optionName.Length < 2)
        {
            _status.Text = "Enter a valid FFmpeg option beginning with a hyphen, for example -movflags.";
            return;
        }

        if (!_selectedOptionIsRunnable)
        {
            _status.Text = "This guide entry is a build-time configure flag, not a runtime ffmpeg option. It cannot be added to an encode command.";
            return;
        }

        var addition = optionName + (string.IsNullOrWhiteSpace(optionArguments) ? string.Empty : " " + optionArguments);
        IReadOnlyList<string> parsed;
        try
        {
            parsed = FFmpegArgumentParser.Parse(addition);
        }
        catch (ArgumentException ex)
        {
            _status.Text = "Fix the option arguments before adding them: " + ex.Message;
            return;
        }

        if (parsed.Count == 0 || !string.Equals(parsed[0], optionName, StringComparison.Ordinal))
        {
            _status.Text = "The option could not be parsed. Check quotation marks and argument syntax.";
            return;
        }

        var scope = _scope.SelectedItem?.ToString() ?? "Other output options";
        _appendOption(scope, addition);
        _status.Text = $"Added {addition} to {scope}. Preview the command in the main window to verify option order and semantics.";
    }

    private static Button Button(string text, EventHandler<RoutedEventArgs> onClick, bool primary = false)
    {
        var button = new Button { Content = text };
        if (primary)
            button.Classes.Add("primary");
        button.Click += onClick;
        return button;
    }

    private void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _status.Text = "Could not open the documentation: " + ex.Message;
        }
    }
}
