using Apps.Optimizely.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Optimizely.Models.Requests;

public class DownloadContentRequest : IDownloadContentInput
{
    [Display("Content ID")]
    [DataSource(typeof(ContentDataSourceHandler))]
    public string ContentId { get; set; } = string.Empty;

    [DataSource(typeof(LanguageDataSourceHandler))]
    public string? Locale { get; set; }

    [Display("Localizable fields")]
    public IEnumerable<string>? LocalizableFields { get; set; }
}
