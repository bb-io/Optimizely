using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Models.Roundtrip;

public class RoundtripState
{
    public string ContentId { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public string? ContentName { get; set; }

    public JObject OriginalJson { get; set; } = new();

    public IReadOnlyCollection<RoundtripField> Fields { get; set; } = Array.Empty<RoundtripField>();

    public IReadOnlyCollection<RoundtripReferenceField> ReferenceFields { get; set; } = Array.Empty<RoundtripReferenceField>();

    public IReadOnlyCollection<RoundtripReferenceState> ReferenceEntries { get; set; } = Array.Empty<RoundtripReferenceState>();
}
