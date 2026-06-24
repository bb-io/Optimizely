using Apps.Optimizely.Api;
using Apps.Optimizely.Models.Dtos;
using Apps.Optimizely.Models.Entities;
using Apps.Optimizely.Models.Roundtrip;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Services;

public class OptimizelyContentService
{
    private const int DefaultMaxDepth = 5;
    private const int ConcurrentFetchLimit = 5;
    private readonly Client _client;

    public OptimizelyContentService(Client client)
    {
        _client = client;
    }

    public async Task<List<ContentItemEntity>> SearchContentAsync(SearchContentFilters filters, CancellationToken cancellationToken = default)
    {
        var normalizedFilters = NormalizeFilters(filters);
        if (normalizedFilters.MaxDepth == 0 || normalizedFilters.MaxResults == 0)
        {
            return [];
        }

        var results = new List<ContentItemEntity>();
        var visitedIds = new HashSet<int> { normalizedFilters.RootContentId };
        var currentLevel = new List<int> { normalizedFilters.RootContentId };
        using var semaphore = new SemaphoreSlim(ConcurrentFetchLimit, ConcurrentFetchLimit);

        for (var depth = 0; depth < normalizedFilters.MaxDepth && currentLevel.Count > 0; depth++)
        {
            if (HasReachedMaxResults(results.Count, normalizedFilters.MaxResults))
            {
                break;
            }

            var childBatches = await FetchChildrenAsync(currentLevel, semaphore, cancellationToken);
            var nextLevel = new List<int>();

            foreach (var child in childBatches.SelectMany(batch => batch))
            {
                var childId = child.ContentLink?.Id;
                if (childId is null || childId <= 0 || !visitedIds.Add(childId.Value))
                {
                    continue;
                }

                if (MatchesFilters(child, normalizedFilters))
                {
                    results.Add(ContentItemEntity.FromDto(child));
                    if (HasReachedMaxResults(results.Count, normalizedFilters.MaxResults))
                    {
                        break;
                    }
                }

                if (depth + 1 < normalizedFilters.MaxDepth)
                {
                    nextLevel.Add(childId.Value);
                }
            }

            currentLevel = nextLevel;
        }

        return TrimResults(results, normalizedFilters.MaxResults);
    }

    public async Task<List<LanguageEntity>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        var languages = await _client.GetLanguagesAsync(cancellationToken);
        return languages
            .GroupBy(language => language.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => LanguageEntity.FromDto(group.First()))
            .OrderBy(language => language.DisplayName)
            .ToList();
    }

    public Task<JObject> GetContentAsync(string contentId, string? locale = null, CancellationToken cancellationToken = default)
        => _client.GetContentAsync(contentId, locale, cancellationToken);

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

        var languages = await _client.GetLanguagesAsync(cancellationToken);
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
                var referenceSourceContent = await _client.GetContentAsync(referenceId, null, cancellationToken);
                var referenceContent = !string.IsNullOrWhiteSpace(locale) && HasLanguage(referenceSourceContent, locale)
                    ? await _client.GetContentAsync(referenceId, locale, cancellationToken)
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

        return await _client.GetContentAsync(contentId, null, cancellationToken);
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

            var propertyDataType = fieldObject["propertyDataType"]?.ToString();
            if (propertyDataType == "PropertyCategory")
            {
                continue;
            }

            var value = fieldObject["value"];
            if (value is null || value.Type == JTokenType.Null)
            {
                continue;
            }

            payload[property.Name] = new JObject { ["value"] = value.DeepClone() };
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

    public JObject BuildCreateLanguageBranchPayload(JObject sourceContent, RoundtripContentDocument document, string locale, OptimizelyLanguageDto language)
        => BuildCreateLanguageBranchPayload(sourceContent, new RoundtripReferenceEntryDocument
        {
            ContentId = document.ContentId,
            OriginalJson = document.OriginalJson,
            Fields = document.Fields
        }, locale, language);

    private static bool IsExcludedFromCreatePayload(string propertyName)
    {
        return propertyName is
            "contentLink" or
            "language" or
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

    private async Task<List<OptimizelyContentSummaryDto>[]> FetchChildrenAsync(IEnumerable<int> parentIds, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        // Fetch each sibling branch in parallel while keeping the API fan-out bounded.
        var fetchTasks = parentIds.Select(async parentId =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await _client.GetChildrenAsync(parentId.ToString(), cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(fetchTasks);
    }

    private static NormalizedSearchContentFilters NormalizeFilters(SearchContentFilters filters)
    {
        if (string.IsNullOrWhiteSpace(filters.RootContentId) ||
            !int.TryParse(filters.RootContentId, out var rootContentId) ||
            rootContentId <= 0)
        {
            throw new PluginMisconfigurationException("Root content ID is required and must be a positive integer.");
        }

        if (filters.MaxDepth is < 0)
        {
            throw new PluginMisconfigurationException("Max depth must be zero or greater.");
        }

        if (filters.MaxResults is < 0)
        {
            throw new PluginMisconfigurationException("Max results must be zero or greater.");
        }

        return new NormalizedSearchContentFilters(
            rootContentId,
            filters.ContentReferenceGuid?.Trim(),
            filters.ContentType?.Trim(),
            filters.NameContains?.Trim(),
            filters.CategoryId,
            filters.Locale?.Trim(),
            filters.PublishedAfter,
            filters.PublishedBefore,
            filters.IncludeUnpublished ?? false,
            filters.MaxDepth ?? DefaultMaxDepth,
            filters.MaxResults);
    }

    private static bool MatchesFilters(OptimizelyContentSummaryDto content, NormalizedSearchContentFilters filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.ContentReferenceGuid) &&
            !string.Equals(content.ContentLink?.GuidValue, filters.ContentReferenceGuid, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filters.ContentType) &&
            (content.ContentType?.Any(type => type.Equals(filters.ContentType, StringComparison.OrdinalIgnoreCase)) != true))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filters.NameContains) &&
            (content.Name?.Contains(filters.NameContains, StringComparison.OrdinalIgnoreCase) != true))
        {
            return false;
        }

        if (filters.CategoryId.HasValue &&
            (content.Category?.Value?.Contains(filters.CategoryId.Value) != true))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filters.Locale) &&
            (content.ExistingLanguages?.Any(language => language.Name?.Contains(filters.Locale, StringComparison.OrdinalIgnoreCase) == true) != true))
        {
            return false;
        }

        if (filters.PublishedAfter.HasValue &&
            (!content.StartPublish.HasValue || content.StartPublish.Value < filters.PublishedAfter.Value))
        {
            return false;
        }

        if (filters.PublishedBefore.HasValue &&
            (!content.StartPublish.HasValue || content.StartPublish.Value > filters.PublishedBefore.Value))
        {
            return false;
        }

        if (!filters.IncludeUnpublished &&
            !string.Equals(content.Status, "Published", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool HasReachedMaxResults(int resultCount, int? maxResults)
        => maxResults.HasValue && resultCount >= maxResults.Value;

    private static List<ContentItemEntity> TrimResults(List<ContentItemEntity> results, int? maxResults)
        => maxResults.HasValue ? results.Take(maxResults.Value).ToList() : results;

    private sealed record NormalizedSearchContentFilters(
        int RootContentId,
        string? ContentReferenceGuid,
        string? ContentType,
        string? NameContains,
        int? CategoryId,
        string? Locale,
        DateTime? PublishedAfter,
        DateTime? PublishedBefore,
        bool IncludeUnpublished,
        int MaxDepth,
        int? MaxResults);
}

public record SearchContentFilters
{
    public string RootContentId { get; init; } = string.Empty;

    public string? ContentReferenceGuid { get; init; }

    public string? ContentType { get; init; }

    public string? NameContains { get; init; }

    public int? CategoryId { get; init; }

    public string? Locale { get; init; }

    public DateTime? PublishedAfter { get; init; }

    public DateTime? PublishedBefore { get; init; }

    public bool? IncludeUnpublished { get; init; }

    public int? MaxDepth { get; init; }

    public int? MaxResults { get; init; }
}
