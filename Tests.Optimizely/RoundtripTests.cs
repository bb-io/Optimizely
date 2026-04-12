using Apps.Optimizely.Html;
using Apps.Optimizely.Models.Dtos;
using Apps.Optimizely.Models.Roundtrip;
using Apps.Optimizely.Services;
using Newtonsoft.Json.Linq;
using Tests.Optimizely.SampleData;

namespace Tests.Optimizely;

[TestClass]
public class RoundtripTests
{
    [TestMethod]
    public void Get_requested_paths_always_includes_defaults_and_distincts()
    {
        var service = new OptimizelyRoundtripService();

        var paths = service.GetRequestedPaths(["name", "metaTitle.value", "teaserText.value"]);

        CollectionAssert.AreEquivalent(new[] { "name", "metaTitle.value", "teaserText.value" }, paths.ToArray());
    }

    [TestMethod]
    public void Html_roundtrip_preserves_metadata_and_selected_fields()
    {
        var content = JObject.Parse(SamplePayloads.Content);
        var referenceContent = JObject.Parse(SamplePayloads.ReferenceContent);
        var secondReferenceContent = JObject.Parse(SamplePayloads.SecondReferenceContent);
        referenceContent["blackbirdReferenceField"] = "globalNewsPageLink";
        secondReferenceContent["blackbirdReferenceField"] = "mainContentArea";
        var service = new OptimizelyRoundtripService();
        var htmlConverter = new OptimizelyContentToHtmlConverter();
        var fromHtmlConverter = new OptimizelyHtmlToContentConverter();

        var state = service.CreateState(content, "en", ["teaserText.value", "metaKeywords.value", "globalNewsPageLink.value"], [referenceContent, secondReferenceContent]);
        var html = htmlConverter.Convert(state);
        var document = fromHtmlConverter.Convert(html);

        Assert.IsTrue(html.Contains("original-content-id", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("5", document.ContentId);
        Assert.AreEqual(4, document.Fields.Count);
        Assert.IsTrue(document.Fields.Any(field => field.Path == "name" && field.Value == "Start"));
        Assert.IsTrue(document.Fields.Any(field => field.Path == "metaTitle.value" && field.Value == "Alloy title"));
        Assert.IsTrue(document.Fields.Any(field => field.Path == "metaKeywords.value" && field.ValueType == "array"));
        Assert.IsTrue(document.Fields.Any(field => field.Path == "teaserText.value" && field.Value == "Teaser text"));
        Assert.IsFalse(document.Fields.Any(field => field.Path == "globalNewsPageLink.value"));
        Assert.AreEqual(0, document.ReferenceFields.Count);
        Assert.AreEqual(2, document.ReferenceEntries.Count);
        Assert.IsTrue(document.ReferenceEntries.Any(entry => entry.ReferenceField == "globalNewsPageLink"));
        Assert.IsTrue(document.ReferenceEntries.Any(entry => entry.ReferenceField == "mainContentArea"));
        Assert.IsTrue(document.ReferenceEntries.Any(entry => entry.Fields.Any(field => field.Path == "name" && field.Value == "How to buy")));
    }

    [TestMethod]
    public void Html_roundtrip_preserves_selected_reference_fields_for_main_entry()
    {
        var content = JObject.Parse(SamplePayloads.Content);
        var service = new OptimizelyRoundtripService();
        var htmlConverter = new OptimizelyContentToHtmlConverter();
        var fromHtmlConverter = new OptimizelyHtmlToContentConverter();

        var state = service.CreateState(
            content,
            "sv",
            ["teaserText.value"],
            [
                new RoundtripReferenceField
                {
                    Path = "mainContentArea",
                    Value = (JObject)content["mainContentArea"]!.DeepClone()
                }
            ],
            []);

        var html = htmlConverter.Convert(state);
        var document = fromHtmlConverter.Convert(html);

        Assert.AreEqual(1, document.ReferenceFields.Count);
        Assert.AreEqual("mainContentArea", document.ReferenceFields.First().Path);
        Assert.AreEqual("30", document.ReferenceFields.First().Value["value"]?[0]?["contentLink"]?["id"]?.ToString());
    }

    [TestMethod]
    public void Build_patch_only_contains_selected_fields_and_language()
    {
        var content = JObject.Parse(SamplePayloads.Content);
        var service = new OptimizelyRoundtripService();
        var document = new RoundtripContentDocument
        {
            ContentId = "5",
            Locale = "sv",
            OriginalJson = content,
            Fields =
            [
                new RoundtripField { Path = "name", Value = "Start SV", ValueType = "string" },
                new RoundtripField { Path = "metaTitle.value", Value = "Alloy title SV", ValueType = "string" }
            ]
        };

        var patch = service.BuildPatch(document, new OptimizelyLanguageDto
        {
            Name = "sv",
            DisplayName = "Swedish",
            Link = "https://localhost:5000/sv/"
        });

        Assert.AreEqual("Start SV", patch["name"]?.ToString());
        Assert.AreEqual("Alloy title SV", patch["metaTitle"]?["value"]?.ToString());
        Assert.AreEqual("sv", patch["language"]?["name"]?.ToString());
        Assert.IsNull(patch["teaserText"]);
    }

    [TestMethod]
    public void Build_patch_preserves_scalar_arrays()
    {
        var service = new OptimizelyRoundtripService();
        var document = new RoundtripContentDocument
        {
            ContentId = "5",
            Fields =
            [
                new RoundtripField
                {
                    Path = "metaKeywords.value",
                    ValueType = "array",
                    Value = "[\"One\",\"Two\"]"
                }
            ]
        };

        var patch = service.BuildPatch(document, new OptimizelyLanguageDto
        {
            Name = "sv",
            DisplayName = "Swedish",
            Link = "https://localhost:5000/sv/"
        });

        Assert.AreEqual("One", patch["metaKeywords"]?["value"]?[0]?.ToString());
        Assert.AreEqual("Two", patch["metaKeywords"]?["value"]?[1]?.ToString());
    }

    [TestMethod]
    public void Build_patch_includes_selected_reference_fields()
    {
        var content = JObject.Parse(SamplePayloads.Content);
        var service = new OptimizelyRoundtripService();
        var document = new RoundtripContentDocument
        {
            ContentId = "5",
            ReferenceFields =
            [
                new RoundtripReferenceField
                {
                    Path = "mainContentArea",
                    Value = (JObject)content["mainContentArea"]!.DeepClone()
                }
            ]
        };

        var patch = service.BuildPatch(document, new OptimizelyLanguageDto
        {
            Name = "sv",
            DisplayName = "Swedish",
            Link = "https://localhost:5000/sv/"
        });

        Assert.AreEqual("30", patch["mainContentArea"]?["value"]?[0]?["contentLink"]?["id"]?.ToString());
        Assert.AreEqual("31", patch["mainContentArea"]?["value"]?[1]?["contentLink"]?["id"]?.ToString());
    }
}
