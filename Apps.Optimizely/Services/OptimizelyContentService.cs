using Apps.Optimizely.Api;
using Apps.Optimizely.Models.Dtos;
using Apps.Optimizely.Models.Entities;
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
}
