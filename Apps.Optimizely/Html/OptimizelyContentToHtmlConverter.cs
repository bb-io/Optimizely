using System.Net;
using Apps.Optimizely.Models.Roundtrip;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        AddFieldNodes(document, rootNode, state.Fields);
        AddReferenceFieldNodes(document, rootNode, state.ReferenceFields);

        foreach (var referenceEntry in state.ReferenceEntries)
        {
            var referenceNode = document.CreateElement("div");
            referenceNode.SetAttributeValue("data-blackbird-state", "reference-entry");
            referenceNode.SetAttributeValue("data-reference-field", referenceEntry.ReferenceField);
            referenceNode.SetAttributeValue("data-content-id", referenceEntry.ContentId);
            referenceNode.SetAttributeValue("data-original-json", WebUtility.HtmlEncode(referenceEntry.OriginalJson.ToString(Formatting.None)));
            bodyNode.AppendChild(referenceNode);

            AddFieldNodes(document, referenceNode, referenceEntry.Fields);
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

    private static void AddReferenceFieldNodes(HtmlDocument document, HtmlNode parentNode, IEnumerable<RoundtripReferenceField> referenceFields)
    {
        foreach (var referenceField in referenceFields)
        {
            var referenceFieldNode = document.CreateElement("div");
            referenceFieldNode.SetAttributeValue("data-blackbird-state", "reference-field");
            referenceFieldNode.SetAttributeValue("data-reference-field", referenceField.Path);
            referenceFieldNode.SetAttributeValue("data-field-json", WebUtility.HtmlEncode(referenceField.Value.ToString(Formatting.None)));
            parentNode.AppendChild(referenceFieldNode);
        }
    }

    private static void AddFieldNodes(HtmlDocument document, HtmlNode parentNode, IEnumerable<RoundtripField> fields)
    {
        foreach (var field in fields)
        {
            var fieldNode = document.CreateElement("div");
            fieldNode.SetAttributeValue("data-blackbird-state", "field");
            fieldNode.SetAttributeValue("data-json-path", field.Path);
            fieldNode.SetAttributeValue("data-value-type", field.ValueType);
            if (field.ValueType == "array")
            {
                var listNode = document.CreateElement("ul");
                var items = JArray.Parse(field.Value);
                foreach (var item in items)
                {
                    var listItemNode = document.CreateElement("li");
                    listItemNode.InnerHtml = WebUtility.HtmlEncode(item.ToString()).Replace("\r\n", "<br/>").Replace("\n", "<br/>");
                    listNode.AppendChild(listItemNode);
                }

                fieldNode.AppendChild(listNode);
            }
            else
            {
                fieldNode.InnerHtml = WebUtility.HtmlEncode(field.Value).Replace("\r\n", "<br/>").Replace("\n", "<br/>");
            }
            parentNode.AppendChild(fieldNode);
        }
    }
}
