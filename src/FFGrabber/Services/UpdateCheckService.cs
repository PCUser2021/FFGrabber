using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FFGrabber.Services;

public sealed record AvailableUpdate(string Product, string CurrentVersion, string LatestVersion, string ReleaseUrl);
public sealed record UpdateCheckResult(IReadOnlyList<AvailableUpdate> Available, IReadOnlyList<string> Notes);

/// <summary>Queries official GitHub Releases APIs only. This service informs the user but never installs binaries.</summary>
public static class UpdateCheckService
{
    private const int MaxResponseBytes = 1_048_576;
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5),
            MaxResponseContentBufferSize = MaxResponseBytes
        };
        var version = typeof(UpdateCheckService).Assembly.GetName().Version;
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
            "FFGrabber", version is null ? "0" : $"{version.Major}.{version.Minor}.{version.Build}"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    public static async Task<UpdateCheckResult> CheckAsync(
        string currentAppVersion,
        string? currentMediaInfoVersion,
        CancellationToken cancellationToken = default)
    {
        var updates = new List<AvailableUpdate>();
        var notes = new List<string>();
        await CheckRepositoryAsync(
            "FFGrabber",
            "https://api.github.com/repos/PCUser2021/FFGrabber/releases/latest",
            currentAppVersion,
            updates, notes, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(currentMediaInfoVersion))
        {
            await CheckRepositoryAsync(
                "MediaInfo CLI",
                "https://api.github.com/repos/MediaArea/MediaInfo/releases/latest",
                currentMediaInfoVersion,
                updates, notes, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            notes.Add("MediaInfo CLI is not configured; install/import it from the official MediaArea distribution to include it in update checks.");
        }

        return new UpdateCheckResult(updates, notes);
    }

    private static async Task CheckRepositoryAsync(
        string product,
        string apiUrl,
        string currentVersion,
        ICollection<AvailableUpdate> updates,
        ICollection<string> notes,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Client.GetAsync(apiUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = json.RootElement;
            var latestTag = root.TryGetProperty("tag_name", out var tag) ? tag.GetString() : null;
            var releaseUrl = root.TryGetProperty("html_url", out var url) ? url.GetString() : null;
            if (string.IsNullOrWhiteSpace(latestTag) || string.IsNullOrWhiteSpace(releaseUrl))
            {
                notes.Add($"{product}: GitHub returned incomplete release metadata.");
                return;
            }

            // The URL is later handed to the OS shell; never trust it beyond an official GitHub release page.
            if (!IsTrustedReleaseUrl(releaseUrl))
            {
                notes.Add($"{product}: ignored a release link that is not an official https://github.com URL.");
                return;
            }

            var latest = latestTag.TrimStart('v', 'V');
            var current = currentVersion.TrimStart('v', 'V');
            if (CompareNumericVersions(latest, current) > 0)
                updates.Add(new AvailableUpdate(product, current, latest, releaseUrl));
            else
                notes.Add($"{product}: up to date (current {current}; latest {latest}).");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            notes.Add($"{product}: update check was unavailable ({ex.Message}).");
        }
    }

    internal static bool IsTrustedReleaseUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)
        && uri.IsDefaultPort
        && string.IsNullOrEmpty(uri.UserInfo)
        && (uri.AbsolutePath.StartsWith("/PCUser2021/FFGrabber/", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.StartsWith("/MediaArea/MediaInfo/", StringComparison.OrdinalIgnoreCase));

    private static int CompareNumericVersions(string left, string right)
    {
        static int[] Parse(string version) =>
            Regex.Matches(version, @"\d+").Select(match => int.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture)).Take(4).ToArray();

        var a = Parse(left);
        var b = Parse(right);
        for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            var av = i < a.Length ? a[i] : 0;
            var bv = i < b.Length ? b[i] : 0;
            if (av != bv) return av.CompareTo(bv);
        }
        return 0;
    }
}
