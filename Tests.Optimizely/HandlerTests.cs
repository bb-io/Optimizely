using Apps.Optimizely.DataSourceHandlers;
using Apps.Optimizely.Models.Requests;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.Optimizely.Base;

namespace Tests.Optimizely;

[TestClass]
public class HandlerTests : TestBase
{
    [TestMethod]
    public async Task Language_handler_returns_languages()
    {
        var handler = new LanguageDataSourceHandler(InvocationContext);

        var result = (await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None)).ToList();

        Assert.IsTrue(result.Any(item => item.Value == "en"));
        Assert.IsTrue(result.Any(item => item.Value == "sv"));
    }

    [TestMethod]
    public async Task Content_handler_returns_children()
    {
        var handler = new ContentDataSourceHandler(InvocationContext);

        var result = (await handler.GetDataAsync(new DataSourceContext { SearchString = "Start" }, CancellationToken.None)).ToList();

        Assert.IsTrue(result.Count > 0);
    }

    [TestMethod]
    public async Task Field_handler_returns_localizable_paths()
    {
        var handler = new FieldDataHandler(
            InvocationContext,
            new ContentRequest { ContentId = "5" },
            new UploadContentRequest());

        var result = (await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None)).ToList();

        Assert.IsTrue(result.Any(item => item.Value == "name"));
        Assert.IsTrue(result.Any(item => item.Value == "metaTitle.value"));
        Assert.IsTrue(result.Any(item => item.Value == "metaKeywords.value"));
    }

    [TestMethod]
    public async Task Reference_field_handler_returns_reference_paths()
    {
        var handler = new ReferenceFieldDataHandler(
            InvocationContext,
            new ContentRequest { ContentId = "5" },
            new UploadContentRequest());

        var result = (await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None)).ToList();

        Assert.IsTrue(result.Any(item => item.Value == "globalNewsPageLink"));
        Assert.IsTrue(result.Any(item => item.Value == "contactsPageLink"));
        Assert.IsTrue(result.Any(item => item.Value == "mainContentArea"));
    }
}
