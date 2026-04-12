using Apps.Optimizely.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Optimizely.Models.Requests;

public class DownloadContentRequest : ContentRequest, IDownloadContentInput
{
    [DataSource(typeof(LanguageDataSourceHandler))]
    public string? Locale { get; set; }

    [Display("Localizable fields")]
    [DataSource(typeof(FieldDataHandler))]
    public IEnumerable<string>? LocalizableFields { get; set; }

    [Display("Reference fields")]
    [DataSource(typeof(ReferenceFieldDataHandler))]
    public IEnumerable<string>? ReferenceFields { get; set; }
}
