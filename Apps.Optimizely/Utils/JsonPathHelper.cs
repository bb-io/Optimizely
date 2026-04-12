using Newtonsoft.Json.Linq;

namespace Apps.Optimizely.Utils;

public static class JsonPathHelper
{
    public static bool TryGetValue(JToken root, string path, out JToken? value)
    {
        value = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (value is not JObject currentObject || !currentObject.TryGetValue(segment, out value))
            {
                value = null;
                return false;
            }
        }

        return value is not null;
    }

    public static void SetValue(JObject root, string path, JToken value)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            throw new ArgumentException("Path cannot be empty", nameof(path));
        }

        JObject current = root;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            if (current[segment] is not JObject nestedObject)
            {
                nestedObject = new JObject();
                current[segment] = nestedObject;
            }

            current = nestedObject;
        }

        current[segments[^1]] = value;
    }
}
