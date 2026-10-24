using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace FFGrabber;

/// <summary>
/// Shows the complete contents of every capture/hardware guide tracked in the repository.
/// The existing curated encoder-option index is also searchable as a virtual document.
/// </summary>
public sealed class GuideLibraryWindow : Window
{
    private sealed record GuideDocument(string Title, string Source, string Content);

    private readonly TextBox _search = new()
    {
        PlaceholderText = "Search file names and full guide text",
        MinWidth = 280
    };
    private readonly ListBox _documents = new()
    {
        MinWidth = 270,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };
    private readonly TextBlock _selectedTitle = new()
    {
        FontSize = 21,
        FontWeight = FontWeight.SemiBold,
        TextWrapping = TextWrapping.Wrap
    };
    private readonly TextBlock _selectedSource = new()
    {
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
        Foreground = ThemePreferences.Brush("#A4A9B6")
    };
    private readonly TextBox _viewer = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = new FontFamily("Consolas"),
        FontSize = 12,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch
    };
    private readonly TextBlock _status = new()
    {
        FontSize = 12,
        Foreground = ThemePreferences.Brush("#A4A9B6")
    };
    private readonly List<GuideDocument> _allDocuments;

    public GuideLibraryWindow()
    {
        Title = "FFGrabber — Source guide library";
        Width = 1120;
        Height = 790;
        MinWidth = 780;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _allDocuments = LoadDocuments();
        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(290)));
        root.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

        var heading = new StackPanel { Spacing = 5, Margin = new Thickness(0, 0, 0, 14) };
        heading.Children.Add(new TextBlock
        {
            Text = "Source guide library",
            FontSize = 26,
            FontWeight = FontWeight.SemiBold
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Search and read complete capture and hardware-acceleration references embedded with FFGrabber.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemePreferences.Brush("#A4A9B6")
        });
        Grid.SetColumnSpan(heading, 2);
        root.Children.Add(heading);

        var listPanel = new Grid { Margin = new Thickness(0, 0, 14, 0) };
        listPanel.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        listPanel.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        listPanel.RowDefinitions[0].Height = GridLength.Auto;
        Grid.SetRow(_search, 0);
        listPanel.Children.Add(_search);
        var documentListSurface = new Border
        {
            Classes = { "panel" },
            Child = _documents,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(documentListSurface, 1);
        listPanel.Children.Add(documentListSurface);
        Grid.SetRow(listPanel, 1);
        Grid.SetColumn(listPanel, 0);
        root.Children.Add(listPanel);

        var details = new Grid();
        details.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        details.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        details.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        _selectedTitle.Margin = new Thickness(0, 0, 0, 3);
        Grid.SetRow(_selectedTitle, 0);
        details.Children.Add(_selectedTitle);
        _selectedSource.Margin = new Thickness(0, 0, 0, 4);
        Grid.SetRow(_selectedSource, 1);
        details.Children.Add(_selectedSource);
        var viewerSurface = new Border
        {
            Classes = { "panel" },
            Child = _viewer,
            Margin = new Thickness(0, 8, 0, 0)
        };
        Grid.SetRow(viewerSurface, 2);
        details.Children.Add(viewerSurface);
        Grid.SetRow(details, 1);
        Grid.SetColumn(details, 1);
        root.Children.Add(details);

        _status.Margin = new Thickness(0, 12, 0, 0);
        Grid.SetRow(_status, 2);
        Grid.SetColumnSpan(_status, 2);
        root.Children.Add(_status);
        Content = root;

        _search.TextChanged += (_, _) => RefreshList();
        _documents.SelectionChanged += (_, _) => ShowSelectedDocument();
        RefreshList();
    }

    private static List<GuideDocument> LoadDocuments()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames();
        var sourceFiles = new[]
        {
            "Capture_ALSA.txt",
            "Capture_Desktop.txt",
            "Capture_Lightning.txt",
            "Capture_Webcam.txt",
            "DirectShow.txt",
            "HWAccelIntro.txt"
        };
        var documents = new List<GuideDocument>();
        foreach (var fileName in sourceFiles)
        {
            var resource = resources.FirstOrDefault(name =>
                name.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase));
            if (resource is null)
                continue;
            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null)
                continue;
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            documents.Add(new GuideDocument(
                fileName.Replace(".txt", "", StringComparison.OrdinalIgnoreCase).Replace('_', ' '),
                fileName,
                content));
        }

        var indexedOptions = EncodingGuideOptionIndex.All
            .OrderBy(option => option.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
            .Select(option => option.Name + " [" + option.Group + "]" + Environment.NewLine
                + option.Description + Environment.NewLine
                + "Source references: " + option.SourceFiles);
        var indexText = "This searchable index preserves the FFmpeg options and descriptions already curated from historical encoding guides. The original 14 encoding-guide .txt files referenced by these records are not present in the current repository tree; this index is metadata, not a substitute for those missing source documents."
            + Environment.NewLine + Environment.NewLine
            + string.Join(Environment.NewLine + Environment.NewLine, indexedOptions);
        documents.Add(new GuideDocument(
            "Encoding options index",
            "EncodingGuideOptionIndex.cs (curated index; original source .txt files not present)",
            indexText));

        return documents.OrderBy(document => document.Title, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void RefreshList()
    {
        var query = _search.Text?.Trim();
        var matches = _allDocuments.Where(document =>
            string.IsNullOrWhiteSpace(query)
            || document.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || document.Source.Contains(query, StringComparison.OrdinalIgnoreCase)
            || document.Content.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        _documents.ItemsSource = matches.Select(document => document.Title).ToArray();
        _status.Text = matches.Count.ToString() + " reference(s) match. The viewer shows the complete selected source text.";
        if (matches.Count == 0)
        {
            _selectedTitle.Text = "No matching source";
            _selectedSource.Text = "Change or clear the search text.";
            _viewer.Text = string.Empty;
            return;
        }

        _documents.SelectedIndex = 0;
        ShowSelectedDocument();
    }

    private void ShowSelectedDocument()
    {
        var title = _documents.SelectedItem?.ToString();
        var document = _allDocuments.FirstOrDefault(item =>
            string.Equals(item.Title, title, StringComparison.Ordinal));
        if (document is null)
            return;

        _selectedTitle.Text = document.Title;
        _selectedSource.Text = document.Source;
        _viewer.Text = document.Content;
    }
}
