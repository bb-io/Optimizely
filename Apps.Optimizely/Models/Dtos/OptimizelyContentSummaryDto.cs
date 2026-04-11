using Newtonsoft.Json;

namespace Apps.Optimizely.Models.Dtos;

public class OptimizelyContentSummaryDto
{
    [JsonProperty("contentLink")]
    public OptimizelyContentReferenceDto? ContentLink { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("language")]
    public OptimizelyLanguageDto? Language { get; set; }

    [JsonProperty("contentType")]
    public List<string>? ContentType { get; set; }

    [JsonProperty("parentLink")]
    public OptimizelyContentReferenceDto? ParentLink { get; set; }
}
