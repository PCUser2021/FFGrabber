using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System.Diagnostics;
using FFGrabber.Models;
using FFGrabber.Services;

namespace FFGrabber;

public sealed class OptionsWindow : Window
{
    private readonly string? _ffmpegPath;
    private readonly SessionLog _sessionLog;
    private readonly Func<EncodingSettings> _getSettings;
    private readonly Func<string> _getQueueSnapshot;
    private readonly Action<string> _log;
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.Parse("#A4A9B6")) };
    private readonly Button _saveLogs = new() { Content = "Save Logs…", Classes = { "primary" } };
    private readonly ComboBox _themeMode = new()
    {
        ItemsSource = new[] { "System", "Light", "Dark" },
        SelectedItem = ThemePreferences.GetSavedChoice(),
        MinWidth = 150
    };

    public OptionsWindow(
        string? ffmpegPath,
        SessionLog sessionLog,
        Func<EncodingSettings> getSettings,
        Func<string> getQueueSnapshot,
        Action<string> log)
    {
        _ffmpegPath = ffmpegPath;
        _sessionLog = sessionLog;
        _getSettings = getSettings;
        _getQueueSnapshot = getQueueSnapshot;
        _log = log;

        Title = "FFGrabber — Options";
        Width = 640;
        Height = 520;
        MinWidth = 540;
        MinHeight = 420;
        Background = ThemePreferences.Brush("#FF111319");

        var root = new StackPanel { Spacing = 14, Margin = new Thickness(24) };
        root.Children.Add(new TextBlock { Text = "Options", FontSize = 24, FontWeight = FontWeight.SemiBold });
        root.Children.Add(new TextBlock
        {
            Text = "Export full diagnostic logs or browse every option supported by your imported FFmpeg build. The option catalogue is read from your local executable, not a stale built-in list.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemePreferences.Brush("#A4A9B6")
        });

        root.Children.Add(new TextBlock { Text = "Appearance", FontSize = 16, FontWeight = FontWeight.SemiBold });
        var appearanceRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
        appearanceRow.Children.Add(new TextBlock { Text = "Application theme", VerticalAlignment = VerticalAlignment.Center, MinWidth = 145 });
        appearanceRow.Children.Add(_themeMode);
        root.Children.Add(appearanceRow);
        root.Children.Add(new TextBlock
        {
            Text = "System follows Windows. Light and Dark are applied immediately to all FFGrabber windows and saved for the next launch.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemePreferences.Brush("#A4A9B6")
        });
        _themeMode.SelectionChanged += (_, _) =>
        {
            var choice = _themeMode.SelectedItem?.ToString() ?? "System";
            ThemePreferences.ApplyChoice(choice);
            _status.Text = $"Appearance changed to {choice}.";
        };

        _saveLogs.Click += async (_, _) => await SaveLogsAsync();
        root.Children.Add(_saveLogs);
        root.Children.Add(Button("Browse all FFmpeg options and capabilities…", async (_, _) =>
        {
            var window = new FFmpegCapabilitiesWindow(_ffmpegPath);
            await window.ShowDialog(this);
        }));
        root.Children.Add(Button("Open official FFmpeg command-line documentation ↗", (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("https://ffmpeg.org/ffmpeg.html") { UseShellExecute = true }); }
            catch (Exception ex) { _status.Text = "Could not open the official documentation: " + ex.Message; }
        }));
        root.Children.Add(Button("Open complete FFmpeg component reference ↗", (_, _) => OpenUrl("https://ffmpeg.org/ffmpeg-all.html")));
        root.Children.Add(Button("Open FFmpeg Trac wiki and guides ↗", (_, _) => OpenUrl("https://trac.ffmpeg.org/wiki")));
        root.Children.Add(_status);
        root.Children.Add(Button("Close", (_, _) => Close()));
        Content = new Border { Classes = { "glassShell" }, Child = root };
    }

    private void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { _status.Text = "Could not open the documentation: " + ex.Message; }
    }

    private static Button Button(string text, EventHandler<RoutedEventArgs> onClick)
    {
        var button = new Button { Content = text };
        button.Click += onClick;
        return button;
    }

    private async Task SaveLogsAsync()
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save FFGrabber full log archive",
                SuggestedFileName = $"FFGrabber-Logs-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
                DefaultExtension = "zip",
                ShowOverwritePrompt = true,
                FileTypeChoices = [new FilePickerFileType("ZIP archive") { Patterns = ["*.zip"] }]
            });

            if (file is null)
            {
                _status.Text = "Log export cancelled.";
                return;
            }

            var path = file.TryGetLocalPath() ?? file.Path.LocalPath;
            if (string.IsNullOrWhiteSpace(path))
                throw new IOException("The selected destination is not a local file path.");
            if (!string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase))
                path += ".zip";

            _saveLogs.IsEnabled = false;
            _status.Text = "Collecting complete logs and FFmpeg diagnostics…";
            _log("Full ZIP log export started: " + path);
            await FFmpegLogArchiveService.SaveZipAsync(
                path, _sessionLog, _getSettings(), _getQueueSnapshot(),
                progress => Avalonia.Threading.Dispatcher.UIThread.Post(() => _status.Text = progress));

            _log("Full ZIP log export finished: " + path);
            _status.Text = $"Saved full ZIP log archive:{Environment.NewLine}{path}";
        }
        catch (Exception ex)
        {
            _log("Full ZIP log export failed: " + ex);
            _status.Text = "Log export failed: " + ex.Message;
        }
        finally
        {
            _saveLogs.IsEnabled = true;
        }
    }
}
