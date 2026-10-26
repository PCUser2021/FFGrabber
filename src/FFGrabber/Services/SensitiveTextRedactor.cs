using System.Text.RegularExpressions;

namespace FFGrabber.Services;

/// <summary>
/// Removes credentials from text that is written to session logs and support bundles.
/// FFmpeg command lines routinely contain stream keys, URL passwords, API tokens and
/// Authorization headers; these must not be persisted to disk or shared in a support ZIP.
/// Redaction is best-effort and deliberately conservative: it never changes ordinary paths.
/// </summary>
public static class SensitiveTextRedactor
{
    public const string Mask = "***";

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    // scheme://user:password@host  ->  scheme://user:***@host   (also user-only tokens)
    private static readonly Regex UrlUserInfo = new(
        @"(?<scheme>\b[a-z][a-z0-9+.\-]{1,15}://)(?<user>[^\s/:@""'?#]+)(?::(?<password>[^\s/@""'?#]*))?@",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        MatchTimeout);

    // ?token=abc&key=... / &password=...  (query strings and FFmpeg protocol options)
    private static readonly Regex SecretQueryValue = new(
        @"(?<name>[?&;](?:token|access_token|auth|authorization|api[_-]?key|apikey|key|secret|passphrase|password|passwd|pwd|signature|sig|streamkey|stream_key)=)(?<value>[^&\s""']+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        MatchTimeout);

    // Authorization: Bearer xyz / Cookie: a=b / X-Api-Key: ...  (-headers option values)
    private static readonly Regex SecretHeader = new(
        @"(?<name>\b(?:authorization|proxy-authorization|cookie|set-cookie|x-api-key|x-auth-token)\s*:\s*)(?<value>[^\r\n""']+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        MatchTimeout);

    // -srt_passphrase / -rtmp_playpath style options followed by a value token.
    private static readonly Regex SecretOptionValue = new(
        @"(?<name>(?<![\w-])-(?:srt_passphrase|passphrase|cert_file_password|http_proxy_password|user_password|password|stream_key)\s+)(?<value>""[^""]*""|'[^']*'|\S+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        MatchTimeout);

    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        try
        {
            var result = UrlUserInfo.Replace(text, match =>
                match.Groups["password"].Success
                    ? $"{match.Groups["scheme"].Value}{match.Groups["user"].Value}:{Mask}@"
                    : $"{match.Groups["scheme"].Value}{Mask}@");
            result = SecretQueryValue.Replace(result, "${name}" + Mask);
            result = SecretHeader.Replace(result, "${name}" + Mask);
            result = SecretOptionValue.Replace(result, "${name}" + Mask);
            return result;
        }
        catch (RegexMatchTimeoutException)
        {
            // Fail closed: never persist text we could not safely scan.
            return "[line withheld: redaction timed out]";
        }
    }
}
