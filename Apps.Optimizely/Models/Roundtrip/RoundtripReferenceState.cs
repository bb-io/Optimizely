using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Models.Roundtrip;

public class RoundtripReferenceState
{
    public string ReferenceField { get; set; } = string.Empty;

    public string ContentId { get; set; } = string.Empty;

    public string? ContentName { get; set; }

    public JObject OriginalJson { get; set; } = new();

    public IReadOnlyCollection<RoundtripField> Fields { get; set; } = Array.Empty<RoundtripField>();
}
