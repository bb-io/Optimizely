using System.Net.Mime;
using Apps.Optimizely.Actions;
using Apps.Optimizely.Api;
using Apps.Optimizely.Models.Requests;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Filters.Transformations;
using Tests.Optimizely.Base;

namespace Tests.Optimizely;

[TestClass]
public class ActionTests : TestBase
{
    [TestMethod]
    public async Task Search_content_filters_by_name()
    {
        var actions = new ContentActions(InvocationContext, FileManager);

        var result = await actions.SearchContent(new SearchContentRequest
        {
            RootContentId = "1",
            NameContains = "Start"
        });

        Assert.IsTrue(result.Any(item => item.Name.Contains("Start", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task Search_languages_returns_available_languages()
    {
        var actions = new LanguageActions(InvocationContext);

        var result = await actions.SearchLanguages();

        Assert.IsTrue(result.Any(item => item.Name == "en"));
        Assert.IsTrue(result.Any(item => item.Name == "sv"));
    }

    [TestMethod]
    public async Task Download_content_creates_html_file()
    {
        var actions = new ContentActions(InvocationContext, FileManager);

        var result = await actions.DownloadContent(new DownloadContentRequest
        {
            ContentId = "5",
            Locale = "en",
            LocalizableFields = ["teaserText.value", "globalNewsPageLink.value"]
        });

        var html = FileManager.ReadOutputAsString(result.Content);

        Assert.IsTrue(html.Contains("original-content-id", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(html.Contains("data-json-path=\"name\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(html.Contains("data-json-path=\"metaTitle.value\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(html.Contains("data-json-path=\"teaserText.value\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("globalNewsPageLink.value", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task Download_content_with_reference_fields_includes_reference_entries()
    {
        var actions = new ContentActions(InvocationContext, FileManager);

        var result = await actions.DownloadContent(new DownloadContentRequest
        {
            ContentId = "5",
            Locale = "en",
            LocalizableFields = ["teaserText.value", "globalNewsPageLink.value", "imageDescription.value", "heading.value", "subHeading.value", "buttonText.value", "text.value"],
            ReferenceFields = ["globalNewsPageLink", "mainContentArea.value"]
        });

        var html = FileManager.ReadOutputAsString(result.Content);

        Assert.IsTrue(html.Contains("data-blackbird-state=\"reference-entry\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(html.Contains("data-reference-field=\"mainContentArea\"", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task Download_content_with_sv_locale_uses_fallback_reference_source_when_field_is_empty()
    {
        var actions = new ContentActions(InvocationContext, FileManager);

        var result = await actions.DownloadContent(new DownloadContentRequest
        {
            ContentId = "5",
            Locale = "sv",
            LocalizableFields = ["heading.value", "subHeading.value", "buttonText.value", "text.value"],
            ReferenceFields = ["mainContentArea", "globalNewsPageLink"]
        });

        var html = FileManager.ReadOutputAsString(result.Content);

        Assert.IsTrue(html.Contains("data-reference-field=\"mainContentArea\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(html.Contains("data-content-id=\"36\"", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task Upload_content_updates_variant_from_html_file()
    {
        var actions = new ContentActions(InvocationContext, FileManager);
        var client = new Client(Creds);
        var download = await actions.DownloadContent(new DownloadContentRequest
        {
            ContentId = "5",
            Locale = "en",
            LocalizableFields = ["teaserText.value", "globalNewsPageLink.value", "imageDescription.value", "heading.value", "subHeading.value", "buttonText.value", "text.value"],
            ReferenceFields = ["globalNewsPageLink", "mainContentArea.value"]
        });

        var suffix = DateTime.UtcNow.ToString("HHmmss");
        var html = FileManager.ReadOutputAsString(download.Content)
            .Replace(">Start<", $">Start SV {suffix}<", StringComparison.Ordinal)
            .Replace(">Alloy - samarbete och projektledning online<", $">Alloy title SV {suffix}<", StringComparison.Ordinal)
            .Replace(">Wherever you meet!<", $">Wherever you meet SV {suffix}<", StringComparison.Ordinal);

        FileManager.WriteInput("optimizely-upload.html", html);

        var result = await actions.UploadContent(new UploadContentRequest
        {
            Content = new FileReference { Name = "optimizely-upload.html", ContentType = MediaTypeNames.Text.Html },
            Locale = "sv"
        });

        Assert.IsTrue(result.IsSuccessful);

        var mainContent = await client.GetContentAsync("5", "sv");
        var referenceContent = await client.GetContentAsync("36", "sv");

        Assert.AreEqual($"Start SV {suffix}", mainContent["name"]?.ToString());
        Assert.IsNotNull(mainContent["mainContentArea"]?["value"]);
        Assert.IsTrue(mainContent["mainContentArea"]?["value"]?.Any() == true);
        Assert.AreEqual($"Wherever you meet SV {suffix}", referenceContent["heading"]?["value"]?.ToString());
    }

    [TestMethod]
    public void Valid_xliff_can_be_converted_back_to_html()
    {
        var repoRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName;
        var path = Path.Combine(repoRoot, "Contentful", "Tests.Contentful", "TestFiles", "Input", "The Loire Valley_en-US.html.xlf");
        var xliff = File.ReadAllText(path);

        var html = Transformation.Parse(xliff, Path.GetFileName(path)).Target().Serialize();

        Assert.IsFalse(string.IsNullOrWhiteSpace(html));
        Assert.IsTrue(html.Contains("<html", StringComparison.OrdinalIgnoreCase));
    }
}
