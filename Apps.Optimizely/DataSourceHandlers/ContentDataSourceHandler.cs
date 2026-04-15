using Apps.Optimizely.Services;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Optimizely.DataSourceHandlers;

public class ContentDataSourceHandler(InvocationContext invocationContext) : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var service = new OptimizelyContentService(Client);
        var contentItems = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            NameContains = context.SearchString,
            IncludeUnpublished = true,
            MaxDepth = 5,
            MaxResults = 50
        }, cancellationToken);

        return contentItems
            .Select(item => new DataSourceItem(item.ContentId, $"{item.Name} ({item.ContentId})"));
    }
}
