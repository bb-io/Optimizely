using Apps.Optimizely.Models.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Optimizely.Models.Entities;

public class ContentItemEntity
{
    [Display("Content ID")]
    public string ContentId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Language { get; set; }

    public string? Url { get; set; }

    [Display("Content type")]
    public string? ContentType { get; set; }

    [Display("Parent ID")]
    public string? ParentId { get; set; }

    public static ContentItemEntity FromDto(OptimizelyContentSummaryDto dto)
    {
        return new()
        {
            ContentId = dto.ContentLink?.Id.ToString() ?? string.Empty,
            Name = dto.Name ?? string.Empty,
            Language = dto.Language?.Name,
            Url = dto.ContentLink?.Url,
            ContentType = dto.ContentType is { Count: > 0 } ? string.Join(", ", dto.ContentType) : null,
            ParentId = dto.ParentLink?.Id.ToString()
        };
    }
}
