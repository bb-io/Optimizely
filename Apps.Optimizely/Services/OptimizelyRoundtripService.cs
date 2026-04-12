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
        return CreateState(content, locale, additionalPaths, Enumerable.Empty<RoundtripReferenceField>(), Enumerable.Empty<JObject>());
    }

    public RoundtripState CreateState(JObject content, string locale, IEnumerable<string>? additionalPaths, IEnumerable<JObject> references)
    {
        return CreateState(content, locale, additionalPaths, Enumerable.Empty<RoundtripReferenceField>(), references);
    }

    public RoundtripState CreateState(JObject content, string locale, IEnumerable<string>? additionalPaths, IEnumerable<RoundtripReferenceField> referenceFields, IEnumerable<JObject> references)
    {
        var fields = new List<RoundtripField>();
        foreach (var path in GetRequestedPaths(additionalPaths))
        {
            if (!JsonPathHelper.TryGetValue(content, path, out var value) ||
                value is null ||
                !IsSupportedFieldValue(value))
            {
                continue;
            }

            fields.Add(new RoundtripField
            {
                Path = path,
                Value = SerializeFieldValue(value),
                ValueType = GetFieldValueType(value)
            });
        }

        return new RoundtripState
        {
            ContentId = content.SelectToken("contentLink.id")?.ToString() ?? string.Empty,
            Locale = locale,
            ContentName = content["name"]?.ToString(),
            OriginalJson = content,
            Fields = fields,
            ReferenceFields = referenceFields.ToArray(),
            ReferenceEntries = references
                .Select(reference => new RoundtripReferenceState
                {
                    ReferenceField = reference["blackbirdReferenceField"]?.ToString() ?? string.Empty,
                    ContentId = reference.SelectToken("contentLink.id")?.ToString() ?? string.Empty,
                    ContentName = reference["name"]?.ToString(),
                    OriginalJson = reference,
                    Fields = GetFields(reference, additionalPaths)
                })
                .ToArray()
        };
    }

    public JObject BuildPatch(RoundtripContentDocument document, OptimizelyLanguageDto language)
    {
        var patch = new JObject();
        foreach (var field in document.Fields)
        {
            JsonPathHelper.SetValue(patch, field.Path, ConvertFieldValue(field));
        }

        foreach (var referenceField in document.ReferenceFields)
        {
            patch[referenceField.Path] = CreateReferenceFieldPatchValue(referenceField.Value);
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

    public IEnumerable<JObject> BuildReferencePatches(RoundtripContentDocument document, IReadOnlyDictionary<string, OptimizelyLanguageDto> languageMap)
    {
        foreach (var referenceEntry in document.ReferenceEntries)
        {
            if (!languageMap.TryGetValue(referenceEntry.ContentId, out var language))
            {
                continue;
            }

            yield return BuildPatch(new RoundtripContentDocument
            {
                ContentId = referenceEntry.ContentId,
                OriginalJson = referenceEntry.OriginalJson,
                Fields = referenceEntry.Fields
            }, language);
        }
    }

    private IReadOnlyCollection<RoundtripField> GetFields(JObject content, IEnumerable<string>? additionalPaths)
    {
        var fields = new List<RoundtripField>();
        foreach (var path in GetRequestedPaths(additionalPaths))
        {
            if (!JsonPathHelper.TryGetValue(content, path, out var value) ||
                value is null ||
                !IsSupportedFieldValue(value))
            {
                continue;
            }

            fields.Add(new RoundtripField
            {
                Path = path,
                Value = SerializeFieldValue(value),
                ValueType = GetFieldValueType(value)
            });
        }

        return fields;
    }

    private static JToken ConvertFieldValue(RoundtripField field)
    {
        return field.ValueType switch
        {
            "integer" => int.TryParse(field.Value, out var intValue) ? JToken.FromObject(intValue) : JValue.CreateString(field.Value),
            "float" or "decimal" => decimal.TryParse(field.Value, out var decimalValue) ? JToken.FromObject(decimalValue) : JValue.CreateString(field.Value),
            "boolean" => bool.TryParse(field.Value, out var boolValue) ? JToken.FromObject(boolValue) : JValue.CreateString(field.Value),
            "array" => JArray.Parse(field.Value),
            _ => JValue.CreateString(field.Value)
        };
    }

    private static bool IsSupportedFieldValue(JToken value)
    {
        return value.Type switch
        {
            JTokenType.Null or JTokenType.Object => false,
            JTokenType.Array => value is JArray array && array.All(item => item is JValue),
            _ => true
        };
    }

    private static string SerializeFieldValue(JToken value)
    {
        return value.Type == JTokenType.Array
            ? value.ToString(Newtonsoft.Json.Formatting.None)
            : value.Type == JTokenType.String
                ? value.Value<string>() ?? string.Empty
                : value.ToString();
    }

    private static string GetFieldValueType(JToken value)
    {
        return value.Type == JTokenType.Array
            ? "array"
            : value.Type.ToString().ToLowerInvariant();
    }

    private static JObject CreateReferenceFieldPatchValue(JObject referenceFieldValue)
    {
        return new JObject
        {
            ["value"] = referenceFieldValue["value"]?.DeepClone() ?? JValue.CreateNull()
        };
    }
}
