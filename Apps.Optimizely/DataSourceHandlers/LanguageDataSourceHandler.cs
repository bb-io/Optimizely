using Apps.Optimizely.Services;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Optimizely.DataSourceHandlers;

public class LanguageDataSourceHandler(InvocationContext invocationContext) : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var service = new OptimizelyContentService(Client);
        var languages = await service.GetLanguagesAsync(cancellationToken);

        return languages
            .Where(language => string.IsNullOrWhiteSpace(context.SearchString) ||
                               language.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase) ||
                               language.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(language => new DataSourceItem(language.Name, language.DisplayName))
            .ToList();
    }
}
