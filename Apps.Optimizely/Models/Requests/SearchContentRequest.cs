using Apps.Optimizely.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Optimizely.Models.Requests;

public class SearchContentRequest
{
    [Display("Root content ID")]
    [DataSource(typeof(ContentDataSourceHandler))]
    public string? RootContentId { get; set; }

    [Display("Name contains")]
    public string? NameContains { get; set; }
}
