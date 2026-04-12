using Apps.Optimizely.Services;
using Newtonsoft.Json.Linq;
using Tests.Optimizely.SampleData;

namespace Tests.Optimizely;

[TestClass]
public class ContentServiceTests
{
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
}
