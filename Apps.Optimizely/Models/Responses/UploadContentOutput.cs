using Apps.Optimizely.Models.Errors;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Optimizely.Models.Responses;

public class UploadContentOutput
{
    [Display("Completed successfully")]
    public bool IsSuccessful { get; set; }

    [Display("Errors")]
    public IEnumerable<ReferenceUpdateError>? Errors { get; set; }
}
