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
      "metaKeywords": {
        "value": [
          "Alloy Plan",
          "Alloy Meet"
        ]
      },
      "teaserText": {
        "value": "Teaser text"
      },
      "globalNewsPageLink": {
        "value": {
          "id": 30
        }
      },
      "mainContentArea": {
        "value": [
          {
            "contentLink": {
              "id": 30
            }
          },
          {
            "contentLink": {
              "id": 31
            }
          }
        ]
      }
    }
    """;

    public const string ReferenceContent = """
    {
      "contentLink": {
        "id": 30,
        "url": "https://localhost:5000/en/how-to-buy/"
      },
      "name": "How to buy",
      "language": {
        "link": "https://localhost:5000/en/",
        "displayName": "English",
        "name": "en"
      },
      "metaTitle": {
        "value": "How to buy title"
      },
      "teaserText": {
        "value": "Reference teaser"
      }
    }
    """;

    public const string SecondReferenceContent = """
    {
      "contentLink": {
        "id": 31,
        "url": "https://localhost:5000/en/about-us/"
      },
      "name": "About us",
      "language": {
        "link": "https://localhost:5000/en/",
        "displayName": "English",
        "name": "en"
      },
      "metaTitle": {
        "value": "About us title"
      }
    }
    """;
}
