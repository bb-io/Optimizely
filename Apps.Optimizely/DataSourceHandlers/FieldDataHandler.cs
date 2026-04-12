using Apps.Optimizely.Models.Requests;
using Apps.Optimizely.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Optimizely.DataSourceHandlers;

public class FieldDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] ContentRequest contentRequest,
    [ActionParameter] UploadContentRequest uploadContentRequest)
    : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var contentId = GetContentId(contentRequest, uploadContentRequest);
        var service = new OptimizelyContentService(Client);
        var fieldDiscoveryService = new OptimizelyFieldDiscoveryService();

        var content = await service.GetContentAsync(contentId, cancellationToken: cancellationToken);
        var fieldPaths = fieldDiscoveryService.GetLocalizableFieldPaths(content);

        return fieldPaths
            .Where(path => string.IsNullOrWhiteSpace(context.SearchString) ||
                           path.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(path => new DataSourceItem(path, path));
    }

    private static string GetContentId(ContentRequest contentRequest, UploadContentRequest uploadContentRequest)
    {
        var contentId = !string.IsNullOrWhiteSpace(contentRequest.ContentId)
            ? contentRequest.ContentId
            : uploadContentRequest.ContentId;

        if (string.IsNullOrWhiteSpace(contentId))
        {
            throw new PluginMisconfigurationException("Please provide Content ID first");
        }

        return contentId;
    }
}
