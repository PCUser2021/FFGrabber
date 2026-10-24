using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace FFGrabber;

/// <summary>
/// Persists the user's appearance preference and supplies system-aware, theme-aware brushes for
/// dynamically constructed controls. FFGrabber deliberately uses opaque surfaces;
/// the theme never depends on desktop pixels or backdrop transparency.
/// </summary>
public static class ThemePreferences
{
    private static readonly string PreferencePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FFGrabber",
        "ui-theme.txt");

    private static readonly object Sync = new();
    private static readonly Dictionary<string, SolidColorBrush> BrushCache = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;

    public static void Initialize(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (!_initialized)
        {
            application.ActualThemeVariantChanged += (_, _) => RefreshBrushes();
            _initialized = true;
        }

        application.RequestedThemeVariant = ToVariant(GetSavedChoice());
        RefreshBrushes();
    }

    public static string GetSavedChoice()
    {
        try
        {
            if (File.Exists(PreferencePath))
                return NormalizeChoice(File.ReadAllText(PreferencePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // If the preference cannot be read, follow the system theme without blocking startup.
        }

        return "System";
    }

    public static void ApplyChoice(string? choice)
    {
        var normalized = NormalizeChoice(choice);
        if (Application.Current is { } application)
            application.RequestedThemeVariant = ToVariant(normalized);

        try
        {
            var directory = Path.GetDirectoryName(PreferencePath)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = PreferencePath + ".tmp";
            File.WriteAllText(temporaryPath, normalized);
            File.Move(temporaryPath, PreferencePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The selected theme still applies for this session if persistence is unavailable.
        }

        RefreshBrushes();
    }

    public static IBrush Brush(string color)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(color);
        lock (Sync)
        {
            if (!BrushCache.TryGetValue(color, out var brush))
            {
                brush = new SolidColorBrush(ResolveColor(color));
                BrushCache.Add(color, brush);
            }

            return brush;
        }
    }

    private static string NormalizeChoice(string? choice) =>
        choice?.Trim().ToLowerInvariant() switch
        {
            "system" => "System",
            "light" => "Light",
            "dark" => "Dark",
            _ => "System"
        };

    private static ThemeVariant ToVariant(string choice) => choice switch
    {
        "Light" => ThemeVariant.Light,
        "Dark" => ThemeVariant.Dark,
        _ => ThemeVariant.Default
    };

    private static bool IsLight =>
        Application.Current?.ActualThemeVariant == ThemeVariant.Light;

    private static Color ResolveColor(string hex)
    {
        var key = hex.Trim().ToUpperInvariant();

        // These source colors are used by dynamically created FFGrabber controls.
        // The cache updates in place on theme changes, so existing controls update too.
        if (IsLight)
        {
            key = key switch
            {
                "#A4A9B6" => "#5D5D5D",
                "#B7BECC" => "#5D5D5D",
                "#C9CED9" => "#303030",
                "#F1F3F8" => "#1A1A1A",
                "#E9EDF5" => "#303030",
                "#C2F970" => "#0067C0",
                "#FF111319" => "#F3F5F8",
                "#084BC0" => "#0067C0",
                "#0067C0" => "#0067C0",
                "#E8F2FC" => "#E8F2FC",
                "#60E3FF" => "#0067C0",
                "#0A3D96" => "#005A9E",
                "#175DCE" => "#0067C0",
                "#4FE9FF" => "#60CDFF",
                "#134A99" => "#E8F2FC",
                "#50D8F7" => "#0067C0",
                "#E7FCFF" => "#005A9E",
                _ => key
            };
        }
        else
        {
            key = key switch
            {
                "#A4A9B6" => "#C8C8C8",
                "#B7BECC" => "#C8C8C8",
                "#C9CED9" => "#D7D7D7",
                "#F1F3F8" => "#F5F5F5",
                "#E9EDF5" => "#D7D7D7",
                "#C2F970" => "#60CDFF",
                "#FF111319" => "#202020",
                "#084BC0" => "#005A9E",
                "#0067C0" => "#60CDFF",
                "#E8F2FC" => "#283B46",
                "#60E3FF" => "#60CDFF",
                "#0A3D96" => "#004578",
                "#175DCE" => "#005A9E",
                "#4FE9FF" => "#60CDFF",
                "#134A99" => "#283B46",
                "#50D8F7" => "#60CDFF",
                "#E7FCFF" => "#F5F5F5",
                _ => key
            };
        }

        return Color.Parse(key);
    }

    private static void RefreshBrushes()
    {
        lock (Sync)
        {
            foreach (var entry in BrushCache)
                entry.Value.Color = ResolveColor(entry.Key);
        }
    }
}
