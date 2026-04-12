using Apps.Optimizely.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Optimizely.Models.Requests;

public class ContentRequest
{
    [Display("Content ID")]
    [DataSource(typeof(ContentDataSourceHandler))]
    public string ContentId { get; set; } = string.Empty;
}
