using Newtonsoft.Json;

namespace Apps.Optimizely.Models.Dtos;

public class OptimizelySiteDto
{
    [JsonProperty("languages")]
    public List<OptimizelyLanguageDto> Languages { get; set; } = [];
}
