using Apps.Optimizely.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;
using Tests.Optimizely.Base;

namespace Tests.Optimizely;

[TestClass]
public class ConnectionValidatorTests : TestBase
{
    [TestMethod]
    public async Task ValidateConnection_ValidData_ShouldBeSuccessful()
    {
        var validator = new ConnectionValidator(InvocationContext);

        var result = await validator.ValidateConnection(Creds, CancellationToken.None);
        Console.WriteLine(result.Message);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ValidateConnection_InvalidPassword_ShouldFail()
    {
        var validator = new ConnectionValidator(InvocationContext);
        var invalidCredentials = Creds
            .Select(x => x.KeyName == "password"
                ? new AuthenticationCredentialsProvider(x.KeyName, x.Value + "_incorrect")
                : new AuthenticationCredentialsProvider(x.KeyName, x.Value));

        var result = await validator.ValidateConnection(invalidCredentials, CancellationToken.None);
        Console.WriteLine(result.Message);
        Assert.IsFalse(result.IsValid);
    }
}
