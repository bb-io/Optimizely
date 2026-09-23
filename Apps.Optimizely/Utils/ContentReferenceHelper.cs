using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Utils;

public static class ContentReferenceHelper
{
    // Content from non-default providers (Commerce catalog, DAM assets) shares numeric IDs with CMS content,
    // so the "{id}__{providerName}" form is required to address the right item.
    public static string? GetContentId(JToken? contentLink)
    {
        if (contentLink is not JObject link)
        {
            return null;
        }

        var id = link["id"]?.ToString();
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var providerName = link["providerName"]?.ToString();
        return string.IsNullOrWhiteSpace(providerName) ? id : $"{id}__{providerName}";
    }
}
