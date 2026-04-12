using Apps.Optimizely.Models.Entities;
using Apps.Optimizely.Services;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Optimizely.Actions;

[ActionList("Languages")]
public class LanguageActions(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [Action("Search languages", Description = "List all site languages available in Optimizely.")]
    public async Task<List<LanguageEntity>> SearchLanguages()
    {
        var service = new OptimizelyContentService(Client);
        return await service.GetLanguagesAsync();
    }
}
