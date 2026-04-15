using Newtonsoft.Json;

namespace Apps.Optimizely.Models.Dtos;

public class OptimizelyContentReferenceDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("guidValue")]
    public string? GuidValue { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }
}
