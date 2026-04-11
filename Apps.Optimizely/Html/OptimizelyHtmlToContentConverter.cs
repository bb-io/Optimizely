using System.Net;
using Apps.Optimizely.Models.Roundtrip;
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
                Value = GetNodeValue(node)
            })
            .Where(field => !string.IsNullOrWhiteSpace(field.Path))
            .ToArray();

        return new RoundtripContentDocument
        {
            ContentId = rootNode.GetAttributeValue("data-content-id", string.Empty),
            Locale = rootNode.GetAttributeValue("data-locale", string.Empty),
            OriginalJson = JObject.Parse(originalJson),
            Fields = fields
        };
    }

    private static string GetNodeValue(HtmlNode node)
    {
        var html = node.InnerHtml.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
        var tempDocument = new HtmlDocument();
        tempDocument.LoadHtml($"<div>{html}</div>");
        return WebUtility.HtmlDecode(tempDocument.DocumentNode.InnerText);
    }
}
