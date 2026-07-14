namespace Apps.Optimizely.Constants;

public class ConnectionTypes
{
    public const string ClientCredentials = "ClientCredentials";
    public const string UserCredentials = "Credentials";

    public static readonly IEnumerable<string> SupportedConnectionTypes = [ClientCredentials, UserCredentials];
}