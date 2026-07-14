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

    [JsonProperty("existingLanguages")]
    public List<OptimizelyLanguageDto>? ExistingLanguages { get; set; }

    [JsonProperty("contentType")]
    public List<string>? ContentType { get; set; }

    [JsonProperty("parentLink")]
    public OptimizelyContentReferenceDto? ParentLink { get; set; }

    [JsonProperty("startPublish")]
    public DateTime? StartPublish { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("category")]
    public OptimizelyCategoryDto? Category { get; set; }
}

public class OptimizelyCategoryDto
{
    [JsonProperty("value")]
    public List<object>? Value { get; set; }
}
