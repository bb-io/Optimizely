using Apps.Optimizely.Api;
using Apps.Optimizely.Models.Dtos;
using Apps.Optimizely.Models.Entities;
using Apps.Optimizely.Models.Roundtrip;
using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Services;

public class OptimizelyContentService(Client client)
{
    public async Task<List<ContentItemEntity>> SearchContentAsync(string? rootContentId, string? nameContains, CancellationToken cancellationToken = default)
    {
        var children = await client.GetChildrenAsync(rootContentId ?? "1", cancellationToken);
        return children
            .Where(child => string.IsNullOrWhiteSpace(nameContains) ||
                            (child.Name?.Contains(nameContains, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(ContentItemEntity.FromDto)
            .ToList();
    }

    public async Task<List<LanguageEntity>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        var languages = await client.GetLanguagesAsync(cancellationToken);
        return languages
            .GroupBy(language => language.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => LanguageEntity.FromDto(group.First()))
            .OrderBy(language => language.DisplayName)
            .ToList();
    }

    public Task<JObject> GetContentAsync(string contentId, string? locale = null, CancellationToken cancellationToken = default)
        => client.GetContentAsync(contentId, locale, cancellationToken);

    public async Task<OptimizelyLanguageDto> GetLanguageAsync(JObject content, string locale, CancellationToken cancellationToken = default)
    {
        var existingLanguage = content["existingLanguages"]?
            .Children()
            .Select(token => token.ToObject<OptimizelyLanguageDto>())
            .FirstOrDefault(language => language?.Name?.Equals(locale, StringComparison.OrdinalIgnoreCase) == true);

        if (existingLanguage is not null)
        {
            return existingLanguage;
        }

        var languages = await client.GetLanguagesAsync(cancellationToken);
        var siteLanguage = languages.FirstOrDefault(language => language.Name?.Equals(locale, StringComparison.OrdinalIgnoreCase) == true)
                           ?? throw new InvalidOperationException($"Language '{locale}' was not found in Optimizely.");

        return new OptimizelyLanguageDto
        {
            Name = siteLanguage.Name,
            DisplayName = siteLanguage.DisplayName,
            Link = siteLanguage.EffectiveLink
        };
    }

    public async Task<List<JObject>> GetReferenceContentsAsync(JObject content, IEnumerable<string>? referenceFields, string? locale = null, CancellationToken cancellationToken = default)
    {
        var fieldDiscoveryService = new OptimizelyFieldDiscoveryService();
        var fallbackContent = await GetFallbackReferenceSourceContentAsync(content, referenceFields, cancellationToken);
        var validatedFields = fieldDiscoveryService.ValidateReferenceFields(content, fallbackContent, referenceFields);

        var references = new List<JObject>();
        foreach (var referenceField in validatedFields)
        {
            var referenceIds = GetReferenceIds(content, referenceField).ToList();
            if (referenceIds.Count == 0 && fallbackContent is not null)
            {
                referenceIds = GetReferenceIds(fallbackContent, referenceField).ToList();
            }

            foreach (var referenceId in referenceIds)
            {
                var referenceSourceContent = await client.GetContentAsync(referenceId, null, cancellationToken);
                var referenceContent = !string.IsNullOrWhiteSpace(locale) && HasLanguage(referenceSourceContent, locale)
                    ? await client.GetContentAsync(referenceId, locale, cancellationToken)
                    : referenceSourceContent;
                referenceContent["blackbirdReferenceField"] = referenceField;
                references.Add(referenceContent);
            }
        }

        return references
            .GroupBy(reference => reference.SelectToken("contentLink.id")?.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    public async Task<IReadOnlyCollection<RoundtripReferenceField>> GetReferenceFieldPayloadsAsync(JObject content, IEnumerable<string>? referenceFields, CancellationToken cancellationToken = default)
    {
        var fieldDiscoveryService = new OptimizelyFieldDiscoveryService();
        var fallbackContent = await GetFallbackReferenceSourceContentAsync(content, referenceFields, cancellationToken);
        var validatedFields = fieldDiscoveryService.ValidateReferenceFields(content, fallbackContent, referenceFields);

        return validatedFields
            .Select(referenceField => new RoundtripReferenceField
            {
                Path = referenceField,
                Value = GetReferenceFieldValue(content, referenceField)?.DeepClone() as JObject
                        ?? GetReferenceFieldValue(fallbackContent, referenceField)?.DeepClone() as JObject
                        ?? throw new InvalidOperationException($"Could not resolve reference field '{referenceField}'.")
            })
            .ToArray();
    }

    public IReadOnlyCollection<RoundtripReferenceField> FilterBranchSpecificReferenceFields(JObject targetContent, IEnumerable<RoundtripReferenceField> referenceFields)
    {
        return referenceFields
            .Where(referenceField => targetContent[referenceField.Path] is not null)
            .ToArray();
    }

    private async Task<JObject?> GetFallbackReferenceSourceContentAsync(JObject content, IEnumerable<string>? referenceFields, CancellationToken cancellationToken)
    {
        if (referenceFields?.Any(field => !string.IsNullOrWhiteSpace(field)) != true)
        {
            return null;
        }

        var contentId = content.SelectToken("contentLink.id")?.ToString();
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return null;
        }

        return await client.GetContentAsync(contentId, null, cancellationToken);
    }

    private static JObject? GetReferenceFieldValue(JObject? content, string referenceField)
    {
        if (content?[referenceField] is not JObject fieldObject)
        {
            return null;
        }

        return GetReferenceIds(content, referenceField).Any() ? fieldObject : null;
    }

    public bool HasLanguage(JObject content, string locale)
    {
        return content["existingLanguages"]?
            .Children()
            .Any(token => token["name"]?.ToString().Equals(locale, StringComparison.OrdinalIgnoreCase) == true) == true;
    }

    public JObject BuildCreateLanguageBranchPayload(JObject sourceContent, RoundtripReferenceEntryDocument referenceEntry, string locale, OptimizelyLanguageDto language)
    {
        var payload = new JObject
        {
            ["contentLink"] = sourceContent["contentLink"]?.DeepClone(),
            ["name"] = referenceEntry.Fields.FirstOrDefault(field => field.Path == "name") is { } nameField
                ? JToken.FromObject(nameField.Value)
                : sourceContent["name"]?.DeepClone(),
            ["language"] = new JObject
            {
                ["link"] = language.EffectiveLink is null ? JValue.CreateNull() : JToken.FromObject(language.EffectiveLink),
                ["displayName"] = language.DisplayName,
                ["name"] = locale
            },
            ["contentType"] = sourceContent["contentType"]?.DeepClone(),
            ["parentLink"] = sourceContent["parentLink"]?.DeepClone(),
            ["routeSegment"] = sourceContent["routeSegment"]?.DeepClone(),
            ["status"] = sourceContent["status"]?.DeepClone()
        };

        foreach (var property in sourceContent.Properties())
        {
            if (IsExcludedFromCreatePayload(property.Name))
            {
                continue;
            }

            if (property.Value is not JObject fieldObject)
            {
                continue;
            }

            if (fieldObject["propertyDataType"]?.ToString() == "PropertyCategory")
            {
                continue;
            }

            payload[property.Name] = fieldObject.DeepClone();
        }

        var patch = new OptimizelyRoundtripService().BuildPatch(new RoundtripContentDocument
        {
            ContentId = referenceEntry.ContentId,
            OriginalJson = referenceEntry.OriginalJson,
            Fields = referenceEntry.Fields
        }, language);

        foreach (var property in patch.Properties().Where(property => property.Name != "language"))
        {
            payload[property.Name] = property.Value.DeepClone();
        }

        return payload;
    }

    private static IEnumerable<string> GetReferenceIds(JObject content, string referenceField)
    {
        var value = content.SelectToken($"{referenceField}.value");
        if (value is JObject obj && obj["id"] is not null)
        {
            yield return obj["id"]!.ToString();
            yield break;
        }

        if (value is not JArray array)
        {
            yield break;
        }

        foreach (var item in array)
        {
            var referenceId = item["contentLink"]?["id"]?.ToString() ?? item["id"]?.ToString();
            if (!string.IsNullOrWhiteSpace(referenceId))
            {
                yield return referenceId;
            }
        }
    }

    private static bool IsExcludedFromCreatePayload(string propertyName)
    {
        return propertyName is
            "existingLanguages" or
            "masterLanguage" or
            "url" or
            "changed" or
            "created" or
            "startPublish" or
            "stopPublish" or
            "saved" or
            "previewUrl" or
            "editUrl" or
            "blackbirdReferenceField";
    }
}
