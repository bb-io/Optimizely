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
            Name = ConnectionTypes.UserCredentials,
            DisplayName = "Username & password",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredsNames.BaseUrl) { DisplayName = "Base URL", Description = "Example: https://localhost:5000" },
                new(CredsNames.ClientId) { DisplayName = "Client ID" },
                new(CredsNames.ClientSecret) { DisplayName = "Client secret", Sensitive = true },
                new(CredsNames.Username) { DisplayName = "Username" },
                new(CredsNames.Password) { DisplayName = "Password", Sensitive = true }
            }
        },
        new()
        {
            Name = ConnectionTypes.ClientCredentials,  
            DisplayName = "Client credentials",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredsNames.BaseUrl) { DisplayName = "Base URL", Description = "Example: https://localhost:5000" },
                new(CredsNames.ClientId) { DisplayName = "Client ID" },
                new(CredsNames.ClientSecret) { DisplayName = "Client secret", Sensitive = true }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values)
    {
        var providers = new List<AuthenticationCredentialsProvider>();
        
        var connectionType = values[nameof(ConnectionPropertyGroup)] switch
        {
            var ct when ConnectionTypes.SupportedConnectionTypes.Contains(ct) => ct,
            _ => throw new Exception($"Unknown connection type: {values[nameof(ConnectionPropertyGroup)]}")
        };
        
        providers.Add(new AuthenticationCredentialsProvider(
            CredsNames.ConnectionType,
            connectionType));
        
        foreach (var value in values)
        {
            providers.Add(new AuthenticationCredentialsProvider(
                value.Key,
                value.Key == CredsNames.BaseUrl ? UrlHelper.Normalize(value.Value) : value.Value));
        }
        
        return providers;
    }
}
