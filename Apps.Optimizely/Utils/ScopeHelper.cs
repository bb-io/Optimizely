namespace Apps.Optimizely.Utils;

public static class ScopeHelper
{
    private static readonly char[] Separators = [' ', ',', ';', '\t', '\r', '\n'];

    public static string Normalize(string scopes) =>
        string.Join(' ', scopes
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal));
}
