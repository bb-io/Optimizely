using System.Text.RegularExpressions;

namespace Apps.Optimizely.Utils;

public static class FileNameSanitizer
{
    private static readonly Regex InvalidCharsRegex = new($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);

    public static string Sanitize(string? rawName, string fallback)
    {
        var candidate = string.IsNullOrWhiteSpace(rawName) ? fallback : rawName.Trim();
        candidate = InvalidCharsRegex.Replace(candidate, "_");
        candidate = candidate.Replace(' ', '-');

        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
    }
}
