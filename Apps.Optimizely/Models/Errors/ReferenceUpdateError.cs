using Blackbird.Applications.Sdk.Common;

namespace Apps.Optimizely.Models.Errors;

public class ReferenceUpdateError
{
    [Display("Content ID")]
    public string ContentId { get; set; } = string.Empty;

    [Display("Reference field")]
    public string? ReferenceField { get; set; }

    public string Message { get; set; } = string.Empty;
}
