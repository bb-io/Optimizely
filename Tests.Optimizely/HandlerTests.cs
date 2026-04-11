using Apps.Optimizely.DataSourceHandlers;
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
}
