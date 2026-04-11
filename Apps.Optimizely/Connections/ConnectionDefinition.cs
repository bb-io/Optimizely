using Apps.Optimizely.Constants;
using Apps.Optimizely.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.Optimizely.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = "Credentials",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredsNames.BaseUrl) { DisplayName = "Base URL", Description = "Example: https://localhost:5000" },
                new(CredsNames.ClientId) { DisplayName = "Client ID" },
                new(CredsNames.ClientSecret) { DisplayName = "Client secret", Sensitive = true },
                new(CredsNames.Username) { DisplayName = "Username" },
                new(CredsNames.Password) { DisplayName = "Password", Sensitive = true }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values)
    {
        foreach (var value in values)
        {
            yield return new AuthenticationCredentialsProvider(
                value.Key,
                value.Key == CredsNames.BaseUrl ? UrlHelper.Normalize(value.Value) : value.Value);
        }
    }
}
