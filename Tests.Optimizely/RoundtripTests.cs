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
        var service = new OptimizelyRoundtripService();
        var htmlConverter = new OptimizelyContentToHtmlConverter();
        var fromHtmlConverter = new OptimizelyHtmlToContentConverter();

        var state = service.CreateState(content, "en", ["teaserText.value", "globalNewsPageLink.value"]);
        var html = htmlConverter.Convert(state);
        var document = fromHtmlConverter.Convert(html);

        Assert.IsTrue(html.Contains("original-content-id", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("5", document.ContentId);
        Assert.AreEqual(3, document.Fields.Count);
        Assert.IsTrue(document.Fields.Any(field => field.Path == "name" && field.Value == "Start"));
        Assert.IsTrue(document.Fields.Any(field => field.Path == "metaTitle.value" && field.Value == "Alloy title"));
        Assert.IsTrue(document.Fields.Any(field => field.Path == "teaserText.value" && field.Value == "Teaser text"));
        Assert.IsFalse(document.Fields.Any(field => field.Path == "globalNewsPageLink.value"));
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
}
