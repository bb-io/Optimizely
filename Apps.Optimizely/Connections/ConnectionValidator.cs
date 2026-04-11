using Apps.Optimizely.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Net;

namespace Apps.Optimizely.Connections;

public class ConnectionValidator(InvocationContext invocationContext) : BaseInvocable(invocationContext), IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = new Client(authenticationCredentialsProviders);
            var request = client.CreateTokenRequest();

            var response = await client.ExecuteAsync(request, cancellationToken);
            var isUnauthorized = response.StatusCode == HttpStatusCode.Unauthorized;
            var hasInvalidGrantMessage = !string.IsNullOrWhiteSpace(response.Content) &&
                                         response.Content.Contains("login failed", StringComparison.OrdinalIgnoreCase);
            var isValid = !isUnauthorized && !hasInvalidGrantMessage;
            var message = response.IsSuccessStatusCode ? "Success" : Client.ExtractErrorMessage(response);

            return new ConnectionValidationResponse
            {
                IsValid = isValid,
                Message = message,
            };

        }
        catch (Exception ex)
        {
            InvocationContext.Logger?.LogError($"Connection validation failed: {ex.Message}", []);

            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }

    }
}
