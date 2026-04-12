namespace Apps.Optimizely.Utils;

public static class UrlHelper
{
    public static string Normalize(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Base URL is required", nameof(url));
        }

        var trimmed = url.Trim().TrimEnd('/');
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = $"https://{trimmed}";
        }

        return trimmed;
    }
}
