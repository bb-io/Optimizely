using Newtonsoft.Json;

namespace Apps.Optimizely.Models.Dtos;

public class OptimizelyLanguageDto
{
    [JsonProperty("link")]
    public string? Link { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("isMasterLanguage")]
    public bool? IsMasterLanguage { get; set; }

    public string? EffectiveLink => Link ?? Url;
}
