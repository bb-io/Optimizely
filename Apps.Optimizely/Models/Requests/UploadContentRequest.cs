using Blackbird.Applications.Sdk.Common;
using Apps.Optimizely.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Optimizely.Models.Requests;

public class UploadContentRequest : IUploadContentInput
{
    public FileReference Content { get; set; } = default!;

    [Display("Content ID")]
    [DataSource(typeof(ContentDataSourceHandler))]
    public string? ContentId { get; set; }

    [DataSource(typeof(LanguageDataSourceHandler))]
    public string Locale { get; set; } = string.Empty;
}
