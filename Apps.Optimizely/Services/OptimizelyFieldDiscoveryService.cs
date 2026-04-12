using Apps.Optimizely.Utils;
using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Services;

public class OptimizelyFieldDiscoveryService
{
    private static readonly HashSet<string> ExcludedTopLevelFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "contentLink",
        "language",
        "existingLanguages",
        "masterLanguage",
        "contentType",
        "parentLink",
        "routeSegment",
        "url",
        "changed",
        "created",
        "startPublish",
        "stopPublish",
        "saved",
        "status",
        "previewUrl",
        "editUrl"
    };

    private static readonly HashSet<string> ExcludedNestedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "propertyDataType",
        "propertyItemType"
    };

    public IReadOnlyCollection<string> GetLocalizableFieldPaths(JObject content)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (content["name"] is JValue)
        {
            paths.Add("name");
        }

        foreach (var property in content.Properties())
        {
            if (ExcludedTopLevelFields.Contains(property.Name) || property.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            CollectLocalizablePaths(property.Name, property.Value, paths);
        }

        return paths.OrderBy(path => path).ToArray();
    }

    public IReadOnlyCollection<string> GetReferenceFieldPaths(JObject content)
    {
        return content.Properties()
            .Where(property =>
                property.Value is JObject fieldObject &&
                IsReferenceValue(fieldObject["value"]))
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();
    }

    public IReadOnlyCollection<string> ValidateReferenceFields(JObject content, IEnumerable<string>? referenceFields)
        => ValidateReferenceFields(content, null, referenceFields);

    public IReadOnlyCollection<string> ValidateReferenceFields(JObject content, JObject? fallbackContent, IEnumerable<string>? referenceFields)
    {
        var validatedFields = new List<string>();
        foreach (var referenceField in referenceFields ?? [])
        {
            if (string.IsNullOrWhiteSpace(referenceField))
            {
                continue;
            }

            var normalizedField = NormalizeReferenceField(referenceField);
            if (!HasReferenceField(content, normalizedField) &&
                !HasReferenceField(fallbackContent, normalizedField))
            {
                throw new Blackbird.Applications.Sdk.Common.Exceptions.PluginMisconfigurationException(
                    $"Reference field '{referenceField}' was not found in the content JSON. Please select a valid reference field.");
            }

            validatedFields.Add(normalizedField);
        }

        return validatedFields.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool HasReferenceField(JObject? content, string normalizedField)
    {
        return content is not null &&
               JsonPathHelper.TryGetValue(content, $"{normalizedField}.value", out var value) &&
               value is not null &&
               IsReferenceValue(value);
    }

    private void CollectLocalizablePaths(string path, JToken token, ISet<string> paths)
    {
        if (token is not JObject obj)
        {
            return;
        }

        if (obj["value"] is JValue)
        {
            paths.Add($"{path}.value");
        }
        else if (obj["value"] is JArray array && array.All(item => item is JValue))
        {
            paths.Add($"{path}.value");
        }

        foreach (var property in obj.Properties())
        {
            if (ExcludedNestedFields.Contains(property.Name))
            {
                continue;
            }

            if (property.Name.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                if (property.Value is JObject or JArray)
                {
                    continue;
                }

                continue;
            }

            if (property.Value is JObject nestedObject)
            {
                CollectLocalizablePaths($"{path}.{property.Name}", nestedObject, paths);
            }
        }
    }

    private static bool IsReferenceValue(JToken? token)
    {
        return token switch
        {
            JObject obj => obj["id"] is not null,
            JArray array => array.OfType<JObject>().Any(item => item["contentLink"]?["id"] is not null || item["id"] is not null),
            _ => false
        };
    }

    private static string NormalizeReferenceField(string referenceField)
    {
        var trimmed = referenceField.Trim();
        return trimmed.EndsWith(".value", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^6]
            : trimmed;
    }
}
