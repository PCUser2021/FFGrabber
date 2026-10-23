using System.Text;

namespace FFGrabber.Services;

/// <summary>
/// Splits an FFmpeg argument line into process arguments without invoking a shell.
/// Double- and single-quoted values are supported for convenient editing in the GUI.
/// Backslashes are preserved except when escaping a quote or another backslash.
/// </summary>
public static class FFmpegArgumentParser
{
    public static IReadOnlyList<string> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var result = new List<string>();
        var current = new StringBuilder();
        char quote = '\0';
        bool tokenStarted = false;

        for (var i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\\' && i + 1 < text.Length &&
                (text[i + 1] == '"' || text[i + 1] == '\'' || text[i + 1] == '\\'))
            {
                current.Append(text[++i]);
                tokenStarted = true;
                continue;
            }

            if (quote != '\0')
            {
                if (c == quote)
                    quote = '\0';
                else
                    current.Append(c);
                tokenStarted = true;
                continue;
            }

            if (c is '"' or '\'')
            {
                quote = c;
                tokenStarted = true;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (tokenStarted)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    tokenStarted = false;
                }
                continue;
            }

            current.Append(c);
            tokenStarted = true;
        }

        if (quote != '\0')
            throw new ArgumentException("An advanced FFmpeg option contains an unmatched quote.");

        if (tokenStarted)
            result.Add(current.ToString());

        return result;
    }
}
