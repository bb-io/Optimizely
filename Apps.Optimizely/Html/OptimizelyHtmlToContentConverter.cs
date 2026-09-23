using System.Net;
using Apps.Optimizely.Models.Roundtrip;
using Apps.Optimizely.Utils;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Html;

public class OptimizelyHtmlToContentConverter
{
    public RoundtripContentDocument Convert(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new ArgumentException("HTML content is empty", nameof(html));
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var rootNode = document.DocumentNode.SelectSingleNode("//div[@data-blackbird-state='main-entry']")
                       ?? throw new InvalidOperationException("Could not find Optimizely round-trip state in the HTML file.");

        var originalJson = WebUtility.HtmlDecode(rootNode.GetAttributeValue("data-original-json", string.Empty));
        if (string.IsNullOrWhiteSpace(originalJson))
        {
            throw new InvalidOperationException("The HTML file does not contain the original JSON state.");
        }

        var fieldNodes = rootNode.SelectNodes("./div[@data-blackbird-state='field']")?.AsEnumerable()
                         ?? Enumerable.Empty<HtmlNode>();
        var fields = fieldNodes
            .Select(node => new RoundtripField
            {
                Path = node.GetAttributeValue("data-json-path", string.Empty),
                ValueType = node.GetAttributeValue("data-value-type", "string"),
                Value = GetFieldValue(node)
            })
            .Where(field => !string.IsNullOrWhiteSpace(field.Path))
            .ToArray();

        var referenceFields = (rootNode.SelectNodes("./div[@data-blackbird-state='reference-field']")?.AsEnumerable()
                               ?? Enumerable.Empty<HtmlNode>())
            .Select(node => new RoundtripReferenceField
            {
                Path = node.GetAttributeValue("data-reference-field", string.Empty),
                Value = JObject.Parse(WebUtility.HtmlDecode(node.GetAttributeValue("data-field-json", "{}")))
            })
            .Where(referenceField => !string.IsNullOrWhiteSpace(referenceField.Path))
            .ToArray();

        var referenceEntries = (document.DocumentNode.SelectNodes("//div[@data-blackbird-state='reference-entry']")?.AsEnumerable()
                               ?? Enumerable.Empty<HtmlNode>())
            .Select(node => new { Node = node, OriginalJson = JObject.Parse(WebUtility.HtmlDecode(node.GetAttributeValue("data-original-json", "{}"))) })
            .Select(entry => new RoundtripReferenceEntryDocument
            {
                ReferenceField = entry.Node.GetAttributeValue("data-reference-field", string.Empty),
                ContentId = GetContentId(entry.Node, entry.OriginalJson),
                OriginalJson = entry.OriginalJson,
                Fields = (entry.Node.SelectNodes("./div[@data-blackbird-state='field']")?.AsEnumerable()
                          ?? Enumerable.Empty<HtmlNode>())
                    .Select(referenceFieldNode => new RoundtripField
                    {
                        Path = referenceFieldNode.GetAttributeValue("data-json-path", string.Empty),
                        ValueType = referenceFieldNode.GetAttributeValue("data-value-type", "string"),
                        Value = GetFieldValue(referenceFieldNode)
                    })
                    .Where(field => !string.IsNullOrWhiteSpace(field.Path))
                    .ToArray()
            })
            .Where(referenceEntry => !string.IsNullOrWhiteSpace(referenceEntry.ContentId))
            .ToArray();

        var originalJsonObject = JObject.Parse(originalJson);
        return new RoundtripContentDocument
        {
            ContentId = GetContentId(rootNode, originalJsonObject),
            Locale = rootNode.GetAttributeValue("data-locale", string.Empty),
            OriginalJson = originalJsonObject,
            Fields = fields,
            ReferenceFields = referenceFields,
            ReferenceEntries = referenceEntries
        };
    }

    // Files exported before provider-qualified IDs were introduced carry only the numeric ID in data-content-id,
    // which points to a different item for non-CMS providers (e.g. "65" instead of "65__CatalogContent").
    private static string GetContentId(HtmlNode node, JObject originalJson)
        => ContentReferenceHelper.GetContentId(originalJson["contentLink"])
           ?? node.GetAttributeValue("data-content-id", string.Empty);

    private static string GetFieldValue(HtmlNode node)
    {
        var valueType = node.GetAttributeValue("data-value-type", "string");
        if (valueType == "array")
        {
            var items = (node.SelectNodes("./ul/li")?.AsEnumerable() ?? Enumerable.Empty<HtmlNode>())
                .Select(GetNodeValue)
                .ToArray();

            return JArray.FromObject(items).ToString(Newtonsoft.Json.Formatting.None);
        }

        return GetNodeValue(node);
    }

    private static string GetNodeValue(HtmlNode node)
    {
        var html = node.InnerHtml.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
        var tempDocument = new HtmlDocument();
        tempDocument.LoadHtml($"<div>{html}</div>");
        return WebUtility.HtmlDecode(tempDocument.DocumentNode.InnerText);
    }
}
