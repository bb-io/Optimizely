using Apps.Optimizely.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Optimizely.Models.Requests;

public class SearchContentRequest
{
    [Display("Root content ID")]
    [DataSource(typeof(ContentDataSourceHandler))]
    public string RootContentId { get; set; } = string.Empty;

    [Display("Content reference GUID")]
    public string? ContentReferenceGuid { get; set; }

    [Display("Content type")]
    public string? ContentType { get; set; }

    [Display("Name contains")]
    public string? NameContains { get; set; }

    [Display("Category ID")]
    public int? CategoryId { get; set; }

    [Display("Locale")]
    public string? Locale { get; set; }

    [Display("Published after")]
    public DateTime? PublishedAfter { get; set; }

    [Display("Published before")]
    public DateTime? PublishedBefore { get; set; }

    [Display("Include unpublished")]
    public bool? IncludeUnpublished { get; set; }

    [Display("Max depth")]
    public int? MaxDepth { get; set; }

    [Display("Max results")]
    public int? MaxResults { get; set; }
}
