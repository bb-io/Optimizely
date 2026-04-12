using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Models.Roundtrip;

public class RoundtripContentDocument
{
    public string ContentId { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public JObject OriginalJson { get; set; } = new();

    public IReadOnlyCollection<RoundtripField> Fields { get; set; } = Array.Empty<RoundtripField>();

    public IReadOnlyCollection<RoundtripReferenceField> ReferenceFields { get; set; } = Array.Empty<RoundtripReferenceField>();

    public IReadOnlyCollection<RoundtripReferenceEntryDocument> ReferenceEntries { get; set; } = Array.Empty<RoundtripReferenceEntryDocument>();
}
