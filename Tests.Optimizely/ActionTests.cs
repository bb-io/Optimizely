using System.Net.Mime;
using Apps.Optimizely.Actions;
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

        var result = await actions.SearchContent(new SearchContentRequest { NameContains = "Start" });

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
    public async Task Upload_content_updates_variant_from_html_file()
    {
        var actions = new ContentActions(InvocationContext, FileManager);
        var download = await actions.DownloadContent(new DownloadContentRequest
        {
            ContentId = "5",
            Locale = "en",
            LocalizableFields = ["teaserText.value"]
        });

        var html = FileManager.ReadOutputAsString(download.Content)
            .Replace(">Start<", $">Start SV {DateTime.UtcNow:HHmmss}<", StringComparison.Ordinal)
            .Replace(">Alloy title<", $">Alloy title SV {DateTime.UtcNow:HHmmss}<", StringComparison.Ordinal);

        FileManager.WriteInput("optimizely-upload.html", html);

        await actions.UploadContent(new UploadContentRequest
        {
            Content = new FileReference { Name = "optimizely-upload.html", ContentType = MediaTypeNames.Text.Html },
            Locale = "sv"
        });
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
