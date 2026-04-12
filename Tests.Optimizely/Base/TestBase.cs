using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.Extensions.Configuration;

namespace Tests.Optimizely.Base;
public class TestBase
{
    public IEnumerable<AuthenticationCredentialsProvider> Creds { get; set; }

    public InvocationContext InvocationContext { get; set; }

    public FileManager FileManager { get; set; }

    public TestBase()
    {
        var configFile = File.Exists("appsettings.json") ? "appsettings.json" : "appsettings.json.example";
        var config = new ConfigurationBuilder().AddJsonFile(configFile).Build();
        Creds = config.GetSection("ConnectionDefinition").GetChildren()
            .Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value ?? string.Empty)).ToList();

        var relativePath = config.GetSection("TestFolder").Value ?? "TestFiles";
        var projectDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        var folderLocation = Path.Combine(projectDirectory, relativePath);

        InvocationContext = new InvocationContext
        {
            AuthenticationCredentialsProviders = Creds,
        };

        FileManager = new FileManager();
    }
}
