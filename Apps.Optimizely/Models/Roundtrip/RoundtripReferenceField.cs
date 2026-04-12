using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Models.Roundtrip;

public class RoundtripReferenceField
{
    public string Path { get; set; } = string.Empty;

    public JObject Value { get; set; } = new();
}
