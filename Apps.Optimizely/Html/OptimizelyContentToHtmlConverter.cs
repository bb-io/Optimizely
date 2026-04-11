using System.Net;
using Apps.Optimizely.Models.Roundtrip;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Apps.Optimizely.Html;

public class OptimizelyContentToHtmlConverter
{
    public string Convert(RoundtripState state)
    {
        var document = new HtmlDocument();
        var htmlNode = document.CreateElement("html");
        htmlNode.SetAttributeValue("lang", state.Locale);
        document.DocumentNode.AppendChild(htmlNode);

        var headNode = document.CreateElement("head");
        htmlNode.AppendChild(headNode);

        AddMeta(document, headNode, "original-content-id", state.ContentId);
        AddMeta(document, headNode, "blackbird-original-content-id", state.ContentId);
        AddMeta(document, headNode, "blackbird-locale", state.Locale);
        AddMeta(document, headNode, "blackbird-content-name", state.ContentName ?? state.ContentId);

        var bodyNode = document.CreateElement("body");
        htmlNode.AppendChild(bodyNode);

        var rootNode = document.CreateElement("div");
        rootNode.SetAttributeValue("data-blackbird-state", "main-entry");
        rootNode.SetAttributeValue("data-content-id", state.ContentId);
        rootNode.SetAttributeValue("data-locale", state.Locale);
        rootNode.SetAttributeValue("data-original-json", WebUtility.HtmlEncode(state.OriginalJson.ToString(Formatting.None)));
        bodyNode.AppendChild(rootNode);

        foreach (var field in state.Fields)
        {
            var fieldNode = document.CreateElement("div");
            fieldNode.SetAttributeValue("data-blackbird-state", "field");
            fieldNode.SetAttributeValue("data-json-path", field.Path);
            fieldNode.SetAttributeValue("data-value-type", field.ValueType);
            fieldNode.InnerHtml = WebUtility.HtmlEncode(field.Value).Replace("\r\n", "<br/>").Replace("\n", "<br/>");
            rootNode.AppendChild(fieldNode);
        }

        return document.DocumentNode.OuterHtml;
    }

    private static void AddMeta(HtmlDocument document, HtmlNode headNode, string name, string value)
    {
        var metaNode = document.CreateElement("meta");
        metaNode.SetAttributeValue("name", name);
        metaNode.SetAttributeValue("content", value);
        headNode.AppendChild(metaNode);
    }
}
