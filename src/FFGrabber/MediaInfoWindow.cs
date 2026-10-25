using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using FFGrabber.Services;

namespace FFGrabber;

public sealed class MediaInfoWindow : Window
{
    private readonly string _mediaPath;
    private readonly TextBox _report = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = new FontFamily("Consolas"),
        FontSize = 12,
        MinHeight = 320,
    };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _inspect = new() { Content = "Inspect with MediaInfo", Classes = { "primary" } };
    private readonly Button _import = new() { Content = "Select MediaInfo CLI…" };

    public MediaInfoWindow(string mediaPath)
    {
        _mediaPath = mediaPath;
        Title = "FFGrabber — MediaInfo";
        Width = 920;
        Height = 720;
        MinWidth = 620;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var root = new StackPanel { Spacing = 10, Margin = new Thickness(18) };
        root.Children.Add(new TextBlock { Text = "MediaInfo technical report", FontSize = 22, FontWeight = FontWeight.SemiBold });
        root.Children.Add(new TextBlock { Text = mediaPath, TextWrapping = TextWrapping.Wrap, Foreground = ThemePreferences.Brush("#A4A9B6") });

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(_inspect);
        actions.Children.Add(_import);
        actions.Children.Add(Button("MediaInfo releases ↗", (_, _) => OpenUrl("https://github.com/MediaArea/MediaInfo/releases")));
        actions.Children.Add(Button("Copy report", async (_, _) => await CopyReportAsync()));
        root.Children.Add(actions);
        root.Children.Add(_status);
        root.Children.Add(_report);
        _inspect.Click += async (_, _) => await InspectAsync();
        _import.Click += async (_, _) => await ImportAsync();
        Content = new Border { Classes = { "glassShell" }, Child = root };
        Opened += async (_, _) => await InspectAsync();
    }

    private async Task ImportAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select the official MediaInfo CLI executable",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("MediaInfo executable") { Patterns = ["MediaInfo.exe", "mediainfo.exe"] }, FilePickerFileTypes.All]
        });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path)) return;

        _import.IsEnabled = false;
        try
        {
            var version = await MediaInfoService.ImportAndValidateAsync(path);
            _status.Text = $"MediaInfo configured: {version}{Environment.NewLine}{path}";
            await InspectAsync();
        }
        catch (Exception ex)
        {
            _status.Text = "MediaInfo import failed: " + ex.Message;
        }
        finally { _import.IsEnabled = true; }
    }

    private async Task CopyReportAsync()
    {
        if (string.IsNullOrEmpty(_report.Text))
        {
            _status.Text = "There is no report to copy yet.";
            return;
        }
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            _status.Text = "Clipboard service is unavailable in this desktop session.";
            return;
        }
        try
        {
            await clipboard.SetTextAsync(_report.Text);
            _status.Text = "MediaInfo JSON report copied to the clipboard.";
        }
        catch (Exception ex)
        {
            _status.Text = "Could not copy report: " + ex.Message;
        }
    }

    private async Task InspectAsync()
    {
        _inspect.IsEnabled = false;
        _status.Text = "Inspecting the media file…";
        try
        {
            var result = await MediaInfoService.InspectAsync(_mediaPath);
            _report.Text = result.JsonReport;
            _status.Text = $"MediaInfo: {result.Version}{Environment.NewLine}Executable: {result.ExecutablePath}";
        }
        catch (Exception ex)
        {
            _report.Text = string.Empty;
            _status.Text = ex.Message + Environment.NewLine
                + "Download the official MediaInfo CLI distribution from https://github.com/MediaArea/MediaInfo/releases and select MediaInfo.exe.";
        }
        finally { _inspect.IsEnabled = true; }
    }

    private static Button Button(string title, EventHandler<Avalonia.Interactivity.RoutedEventArgs> handler)
    {
        var button = new Button { Content = title };
        button.Click += handler;
        return button;
    }

    private void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { _status.Text = "Could not open URL: " + ex.Message; }
    }
}
