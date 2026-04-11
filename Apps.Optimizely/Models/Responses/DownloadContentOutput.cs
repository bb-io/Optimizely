using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Optimizely.Models.Responses;

public class DownloadContentOutput : IDownloadContentOutput
{
    public FileReference Content { get; set; } = default!;

    [Display("Original content ID")]
    public string ContentId { get; set; } = string.Empty;
}
