namespace Tests.Optimizely.SampleData;

public static class SamplePayloads
{
    public const string Content = """
    {
      "contentLink": {
        "id": 5,
        "url": "https://localhost:5000/en/"
      },
      "name": "Start",
      "language": {
        "link": "https://localhost:5000/en/",
        "displayName": "English",
        "name": "en"
      },
      "existingLanguages": [
        {
          "link": "https://localhost:5000/en/",
          "displayName": "English",
          "name": "en"
        },
        {
          "link": "https://localhost:5000/sv/",
          "displayName": "Swedish",
          "name": "sv"
        }
      ],
      "masterLanguage": {
        "link": "https://localhost:5000/en/",
        "displayName": "English",
        "name": "en"
      },
      "metaTitle": {
        "value": "Alloy title"
      },
      "teaserText": {
        "value": "Teaser text"
      },
      "globalNewsPageLink": {
        "value": {
          "id": 30
        }
      }
    }
    """;
}
