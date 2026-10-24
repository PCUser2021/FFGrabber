using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace FFGrabber;

/// <summary>
/// Persists the user's appearance preference and supplies theme-aware brushes for
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
            // If the preference cannot be read, use the system theme without blocking startup.
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
                "#A4A9B6" => "#58677A",
                "#B7BECC" => "#526174",
                "#C9CED9" => "#29364A",
                "#F1F3F8" => "#1C2738",
                "#E9EDF5" => "#29364A",
                "#C2F970" => "#4F7618",
                "#FF111319" => "#F3F6FA",
                _ => key
            };
        }
        else if (key == "#FF111319")
        {
            key = "#10141C";
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
