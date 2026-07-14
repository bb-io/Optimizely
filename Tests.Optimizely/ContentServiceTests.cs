using Apps.Optimizely.Api;
using Apps.Optimizely.Constants;
using Apps.Optimizely.Models.Dtos;
using Apps.Optimizely.Services;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;
using Tests.Optimizely.SampleData;

namespace Tests.Optimizely;

[TestClass]
public class ContentServiceTests
{
    private static readonly DateTime Jan10 = new(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Jan15 = new(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Feb01 = new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Mar01 = new(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Filter_branch_specific_reference_fields_excludes_missing_target_properties()
    {
        var service = new OptimizelyContentService(null!);
        var targetContent = JObject.Parse("""
        {
          "mainContentArea": {
            "value": null,
            "propertyDataType": "PropertyContentArea"
          }
        }
        """);
        var sourceContent = JObject.Parse(SamplePayloads.Content);

        var filtered = service.FilterBranchSpecificReferenceFields(
            targetContent,
            [
                new()
                {
                    Path = "globalNewsPageLink",
                    Value = (JObject)sourceContent["globalNewsPageLink"]!.DeepClone()
                },
                new()
                {
                    Path = "mainContentArea",
                    Value = (JObject)sourceContent["mainContentArea"]!.DeepClone()
                }
            ]);

        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual("mainContentArea", filtered.First().Path);
    }

    [TestMethod]
    public async Task Search_content_throws_for_missing_root_content_id()
    {
        var service = CreateSearchService();

        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            service.SearchContentAsync(new SearchContentFilters { RootContentId = string.Empty }));

        Assert.AreEqual("Root content ID is required and must be a positive integer.", exception.Message);
    }

    [TestMethod]
    public async Task Search_content_throws_for_non_positive_root_content_id()
    {
        var service = CreateSearchService();

        var exception = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() =>
            service.SearchContentAsync(new SearchContentFilters { RootContentId = "0" }));

        Assert.AreEqual("Root content ID is required and must be a positive integer.", exception.Message);
    }

    [TestMethod]
    public async Task Search_content_filters_by_content_reference_guid()
    {
        var service = CreateSearchService();

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            ContentReferenceGuid = "guid-5",
            IncludeUnpublished = true
        });

        CollectionAssert.AreEqual(new[] { "5" }, result.Select(x => x.ContentId).ToList());
    }

    [TestMethod]
    public async Task Search_content_filters_by_content_type_case_insensitively()
    {
        var service = CreateSearchService();

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            ContentType = "articlepage",
            IncludeUnpublished = true
        });

        CollectionAssert.AreEquivalent(new[] { "4", "5", "6" }, result.Select(x => x.ContentId).ToList());
    }

    [TestMethod]
    public async Task Search_content_filters_by_name_contains_case_insensitively()
    {
        var service = CreateSearchService();

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            NameContains = "nested",
            IncludeUnpublished = true
        });

        CollectionAssert.AreEqual(new[] { "6" }, result.Select(x => x.ContentId).ToList());
    }

    [TestMethod]
    public async Task Search_content_filters_by_category_id()
    {
        var service = CreateSearchService();

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            CategoryId = 42,
            IncludeUnpublished = true
        });

        CollectionAssert.AreEqual(new[] { "6" }, result.Select(x => x.ContentId).ToList());
    }

    [TestMethod]
    public async Task Search_content_filters_by_locale()
    {
        var service = CreateSearchService();

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            Locale = "en-us",
            IncludeUnpublished = true
        });

        CollectionAssert.AreEqual(new[] { "6" }, result.Select(x => x.ContentId).ToList());
    }

    [TestMethod]
    public async Task Search_content_filters_by_published_after()
    {
        var service = CreateSearchService();

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            PublishedAfter = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            IncludeUnpublished = true
        });

        CollectionAssert.AreEquivalent(new[] { "5", "6" }, result.Select(x => x.ContentId).ToList());
    }

    [TestMethod]
    public async Task Search_content_filters_by_published_before()
    {
        var service = CreateSearchService();

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            PublishedBefore = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            IncludeUnpublished = true
        });

        CollectionAssert.AreEquivalent(new[] { "3", "4" }, result.Select(x => x.ContentId).ToList());
    }

    [TestMethod]
    public async Task Search_content_excludes_unpublished_by_default()
    {
        var service = CreateSearchService();

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            ContentType = "ArticlePage"
        });

        CollectionAssert.AreEquivalent(new[] { "5", "6" }, result.Select(x => x.ContentId).ToList());
    }

    [TestMethod]
    public async Task Search_content_can_include_unpublished_items()
    {
        var service = CreateSearchService();

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            ContentType = "ArticlePage",
            IncludeUnpublished = true
        });

        CollectionAssert.AreEquivalent(new[] { "4", "5", "6" }, result.Select(x => x.ContentId).ToList());
    }

    [TestMethod]
    public async Task Search_content_respects_max_depth()
    {
        var requests = new List<string>();
        var service = CreateSearchService(requests);

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            IncludeUnpublished = true,
            MaxDepth = 1
        });

        CollectionAssert.DoesNotContain(result.Select(x => x.ContentId).ToList(), "6");
        CollectionAssert.AreEquivalent(new[] { "1" }, requests);
    }

    [TestMethod]
    public async Task Search_content_respects_max_results_and_short_circuits_deeper_walks()
    {
        var requests = new List<string>();
        var service = CreateSearchService(requests);

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            IncludeUnpublished = true,
            MaxResults = 2
        });

        Assert.AreEqual(2, result.Count);
        CollectionAssert.AreEquivalent(new[] { "1" }, requests);
    }

    [TestMethod]
    public async Task Search_content_avoids_cycles()
    {
        var service = CreateSearchService();

        var result = await service.SearchContentAsync(new SearchContentFilters
        {
            RootContentId = "1",
            IncludeUnpublished = true,
            MaxDepth = 5
        });

        Assert.AreEqual(result.Count, result.Select(x => x.ContentId).Distinct().Count());
    }

    private static OptimizelyContentService CreateSearchService(List<string>? requests = null)
    {
        return new OptimizelyContentService(new FakeClient(CreateTree(), requests));
    }

    private static Dictionary<string, List<OptimizelyContentSummaryDto>> CreateTree()
    {
        return new Dictionary<string, List<OptimizelyContentSummaryDto>>
        {
            ["1"] =
            [
                CreateNode(2, "For All Sites", "guid-2", ["Folder", "SysContentFolder"], null, [], null, []),
                CreateNode(3, "Start page", "guid-3", ["Page", "StandardPage"], "Published", [7], Jan10, ["en", "sv"], 1),
                CreateNode(4, "Draft article", "guid-4", ["Page", "ArticlePage"], "CheckedOut", [9], Jan15, ["en"], 1),
                CreateNode(5, "News item", "guid-5", ["Page", "ArticlePage"], "Published", [9], Feb01, ["en"], 1)
            ],
            ["2"] =
            [
                CreateNode(6, "Nested page", "guid-6", ["Page", "ArticlePage"], "Published", [42], Mar01, ["en-US", "sv"], 2)
            ],
            ["6"] =
            [
                CreateNode(2, "For All Sites", "guid-2", ["Folder", "SysContentFolder"], null, [], null, [], 6)
            ]
        };
    }

    private static OptimizelyContentSummaryDto CreateNode(
        int id,
        string name,
        string guidValue,
        List<string> contentTypes,
        string? status,
        List<object> categories,
        DateTime? startPublish,
        List<string> languages,
        int? parentId = null)
    {
        return new OptimizelyContentSummaryDto
        {
            ContentLink = new OptimizelyContentReferenceDto
            {
                Id = id,
                GuidValue = guidValue,
                Url = $"https://example.com/{id}"
            },
            Name = name,
            ContentType = contentTypes,
            Status = status,
            StartPublish = startPublish,
            ParentLink = parentId.HasValue ? new OptimizelyContentReferenceDto { Id = parentId.Value } : null,
            ExistingLanguages = languages.Select(language => new OptimizelyLanguageDto { Name = language }).ToList(),
            Category = new OptimizelyCategoryDto { Value = categories }
        };
    }

    private sealed class FakeClient : Client
    {
        private readonly IReadOnlyDictionary<string, List<OptimizelyContentSummaryDto>> _tree;
        private readonly List<string>? _requests;

        public FakeClient(IReadOnlyDictionary<string, List<OptimizelyContentSummaryDto>> tree, List<string>? requests)
            : base([new AuthenticationCredentialsProvider(CredsNames.BaseUrl, "https://example.com")])
        {
            _tree = tree;
            _requests = requests;
        }

        public override Task<List<OptimizelyContentSummaryDto>> GetChildrenAsync(string rootContentId, CancellationToken cancellationToken = default)
        {
            _requests?.Add(rootContentId);

            var children = _tree.TryGetValue(rootContentId, out var value)
                ? value
                : [];

            return Task.FromResult(children);
        }
    }
}
