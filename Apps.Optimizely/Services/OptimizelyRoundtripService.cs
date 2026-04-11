using Apps.Optimizely.Models.Dtos;
using Apps.Optimizely.Models.Roundtrip;
using Apps.Optimizely.Utils;
using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Services;

public class OptimizelyRoundtripService
{
    private static readonly string[] DefaultPaths = ["name", "metaTitle.value"];

    public IReadOnlyCollection<string> GetRequestedPaths(IEnumerable<string>? additionalPaths)
    {
        return DefaultPaths
            .Concat(additionalPaths ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public RoundtripState CreateState(JObject content, string locale, IEnumerable<string>? additionalPaths)
    {
        var fields = new List<RoundtripField>();
        foreach (var path in GetRequestedPaths(additionalPaths))
        {
            if (!JsonPathHelper.TryGetValue(content, path, out var value) ||
                value is null ||
                value.Type is JTokenType.Null or JTokenType.Object or JTokenType.Array)
            {
                continue;
            }

            fields.Add(new RoundtripField
            {
                Path = path,
                Value = value.Type == JTokenType.String ? value.Value<string>() ?? string.Empty : value.ToString(),
                ValueType = value.Type.ToString().ToLowerInvariant()
            });
        }

        return new RoundtripState
        {
            ContentId = content.SelectToken("contentLink.id")?.ToString() ?? string.Empty,
            Locale = locale,
            ContentName = content["name"]?.ToString(),
            OriginalJson = content,
            Fields = fields
        };
    }

    public JObject BuildPatch(RoundtripContentDocument document, OptimizelyLanguageDto language)
    {
        var patch = new JObject();
        foreach (var field in document.Fields)
        {
            JsonPathHelper.SetValue(patch, field.Path, ConvertFieldValue(field));
        }

        patch["language"] = JObject.FromObject(new
        {
            link = language.EffectiveLink,
            displayName = language.DisplayName,
            name = language.Name
        });

        return patch;
    }

    public string ResolveLocale(JObject content, string? requestedLocale)
    {
        if (!string.IsNullOrWhiteSpace(requestedLocale))
        {
            return requestedLocale;
        }

        return content.SelectToken("language.name")?.ToString()
               ?? content.SelectToken("masterLanguage.name")?.ToString()
               ?? throw new InvalidOperationException("Could not determine the content language.");
    }

    private static JToken ConvertFieldValue(RoundtripField field)
    {
        return field.ValueType switch
        {
            "integer" => int.TryParse(field.Value, out var intValue) ? JToken.FromObject(intValue) : JValue.CreateString(field.Value),
            "float" or "decimal" => decimal.TryParse(field.Value, out var decimalValue) ? JToken.FromObject(decimalValue) : JValue.CreateString(field.Value),
            "boolean" => bool.TryParse(field.Value, out var boolValue) ? JToken.FromObject(boolValue) : JValue.CreateString(field.Value),
            _ => JValue.CreateString(field.Value)
        };
    }
}
