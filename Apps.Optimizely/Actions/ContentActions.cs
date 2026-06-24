using System.Net.Mime;
using System.Text;
using Apps.Optimizely.Html;
using Apps.Optimizely.Models.Errors;
using Apps.Optimizely.Models.Requests;
using Apps.Optimizely.Models.Responses;
using Apps.Optimizely.Models.Roundtrip;
using Apps.Optimizely.Services;
using Apps.Optimizely.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff1;
using Blackbird.Filters.Xliff.Xliff2;

namespace Apps.Optimizely.Actions;

[ActionList("Content")]
public class ContentActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [BlueprintActionDefinition(BlueprintAction.SearchContent)]
    [Action("Search content", Description = "Search content items under the specified root content.")]
    public async Task<List<Models.Entities.ContentItemEntity>> SearchContent([ActionParameter] SearchContentRequest input)
    {
        var service = new OptimizelyContentService(Client);
        return await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = input.RootContentId,
            ContentReferenceGuid = input.ContentReferenceGuid,
            ContentType = input.ContentType,
            NameContains = input.NameContains,
            CategoryId = input.CategoryId,
            Locale = input.Locale,
            PublishedAfter = input.PublishedAfter,
            PublishedBefore = input.PublishedBefore,
            IncludeUnpublished = input.IncludeUnpublished,
            MaxDepth = input.MaxDepth,
            MaxResults = input.MaxResults
        });
    }

    [BlueprintActionDefinition(BlueprintAction.DownloadContent)]
    [Action("Download content", Description = "Download selected localizable Optimizely fields as a Blackbird interoperable HTML file.")]
    public async Task<DownloadContentOutput> DownloadContent([ActionParameter] DownloadContentRequest input)
    {
        var service = new OptimizelyContentService(Client);
        var roundtripService = new OptimizelyRoundtripService();
        var htmlConverter = new OptimizelyContentToHtmlConverter();

        var content = await service.GetContentAsync(input.ContentId, input.Locale);
        var locale = roundtripService.ResolveLocale(content, input.Locale);
        var references = await service.GetReferenceContentsAsync(content, input.ReferenceFields, locale);
        var referenceFields = await service.GetReferenceFieldPayloadsAsync(content, input.ReferenceFields);
        var state = roundtripService.CreateState(content, locale, input.LocalizableFields, referenceFields, references);
        var html = htmlConverter.Convert(state);

        var fileName = $"{FileNameSanitizer.Sanitize(state.ContentName, input.ContentId)}_{locale}.html";
        var file = await fileManagementClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(html)),
            MediaTypeNames.Text.Html,
            fileName);

        return new DownloadContentOutput
        {
            Content = file,
            ContentId = input.ContentId
        };
    }

    [BlueprintActionDefinition(BlueprintAction.UploadContent)]
    [Action("Upload content", Description = "Upload translated Optimizely content from a Blackbird HTML or XLIFF file.")]
    public async Task<UploadContentOutput> UploadContent([ActionParameter] UploadContentRequest input)
    {
        if (!input.Content.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase) &&
            !input.Content.Name.EndsWith(".xlf", StringComparison.OrdinalIgnoreCase) &&
            !input.Content.Name.EndsWith(".xliff", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException("Only .html, .xlf and .xliff files are supported.");
        }

        await using var downloadedStream = await fileManagementClient.DownloadAsync(input.Content);
        await using var memoryStream = new MemoryStream();
        await downloadedStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        string content;
        using (var reader = new StreamReader(memoryStream, Encoding.UTF8, true, 1024, true))
        {
            content = await reader.ReadToEndAsync();
        }

        if (Xliff2Serializer.IsXliff2(content) || Xliff1Serializer.IsXliff1(content))
        {
            var transformation = Transformation.Parse(content, input.Content.Name);
            content = transformation.Target().Serialize()
                      ?? throw new PluginMisconfigurationException("XLIFF did not contain any files");
        }

        var htmlConverter = new OptimizelyHtmlToContentConverter();
        var roundtripDocument = htmlConverter.Convert(content);
        if (string.IsNullOrWhiteSpace(roundtripDocument.ContentId) && !string.IsNullOrWhiteSpace(input.ContentId))
        {
            roundtripDocument.ContentId = input.ContentId;
        }

        var service = new OptimizelyContentService(Client);
        var roundtripService = new OptimizelyRoundtripService();

        var originalContent = roundtripDocument.OriginalJson;
        var targetContent = await service.GetContentAsync(roundtripDocument.ContentId, input.Locale);
        roundtripDocument.ReferenceFields = service.FilterBranchSpecificReferenceFields(targetContent, roundtripDocument.ReferenceFields);
        var language = await service.GetLanguageAsync(originalContent, input.Locale);

        if (service.HasLanguage(originalContent, input.Locale))
        {
            var patch = roundtripService.BuildPatch(roundtripDocument, language);
            await Client.PatchContentAsync(roundtripDocument.ContentId, input.Locale, patch);
        }
        else
        {
            var contentGuid = originalContent.SelectToken("contentLink.guidValue")?.ToString()
                              ?? throw new PluginMisconfigurationException($"Content '{roundtripDocument.ContentId}' is missing contentLink.guidValue.");
            var createPayload = service.BuildCreateLanguageBranchPayload(originalContent, roundtripDocument, input.Locale, language);
            await Client.PutContentAsync(contentGuid, createPayload);
        }

        var errors = new List<ReferenceUpdateError>();
        foreach (var referenceEntry in roundtripDocument.ReferenceEntries)
        {
            try
            {
                var referenceLanguage = await service.GetLanguageAsync(referenceEntry.OriginalJson, input.Locale);
                var referenceDocument = new RoundtripContentDocument
                {
                    ContentId = referenceEntry.ContentId,
                    OriginalJson = referenceEntry.OriginalJson,
                    Fields = referenceEntry.Fields
                };

                if (service.HasLanguage(referenceEntry.OriginalJson, input.Locale))
                {
                    var referencePatch = roundtripService.BuildPatch(referenceDocument, referenceLanguage);
                    await Client.PatchContentAsync(referenceEntry.ContentId, input.Locale, referencePatch);
                }
                else
                {
                    var contentGuid = referenceEntry.OriginalJson.SelectToken("contentLink.guidValue")?.ToString()
                                      ?? throw new PluginMisconfigurationException($"Reference entry '{referenceEntry.ContentId}' is missing contentLink.guidValue.");
                    var createPayload = service.BuildCreateLanguageBranchPayload(referenceEntry.OriginalJson, referenceEntry, input.Locale, referenceLanguage);
                    await Client.PutContentAsync(contentGuid, createPayload);
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ReferenceUpdateError
                {
                    ContentId = referenceEntry.ContentId,
                    ReferenceField = referenceEntry.ReferenceField,
                    Message = ex.Message
                });
            }
        }

        return new UploadContentOutput
        {
            IsSuccessful = errors.Count == 0,
            Errors = errors.Count == 0 ? null : errors
        };
    }
}
