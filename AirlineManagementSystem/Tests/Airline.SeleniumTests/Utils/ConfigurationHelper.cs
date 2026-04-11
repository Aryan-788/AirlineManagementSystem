using Microsoft.Extensions.Configuration;

namespace Airline.SeleniumTests.Utils;

public class UserCredentials
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class TestSettings
{
    public string BaseUrl { get; set; } = "http://localhost:4200";
    public int TimeoutSeconds { get; set; } = 20;
}

public static class ConfigurationHelper
{
    private static readonly IConfiguration Root;

    static ConfigurationHelper()
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        
        Root = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    public static string BaseUrl => Root["TestSettings:BaseUrl"] ?? "http://localhost:4200";
    public static int TimeoutSeconds => int.Parse(Root["TestSettings:TimeoutSeconds"] ?? "20");

    public static UserCredentials GetUser(string role)
    {
        var credentials = new UserCredentials();
        Root.GetSection($"Users:{role}").Bind(credentials);
        return credentials;
    }

    public static UserCredentials Admin => GetUser("Admin");
    public static UserCredentials Passenger => GetUser("Passenger");
    public static UserCredentials Dealer => GetUser("Dealer");
    public static UserCredentials GroundStaff => GetUser("GroundStaff");
}
