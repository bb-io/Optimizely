using Apps.Optimizely.Models.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Optimizely.Models.Entities;

public class LanguageEntity
{
    public string Name { get; set; } = string.Empty;

    [Display("Display name")]
    public string DisplayName { get; set; } = string.Empty;

    public string? Url { get; set; }

    [Display("Master language")]
    public bool IsMasterLanguage { get; set; }

    public static LanguageEntity FromDto(OptimizelyLanguageDto dto)
    {
        return new()
        {
            Name = dto.Name ?? string.Empty,
            DisplayName = dto.DisplayName ?? dto.Name ?? string.Empty,
            Url = dto.EffectiveLink,
            IsMasterLanguage = dto.IsMasterLanguage ?? false
        };
    }
}
