using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Rubik.Api;
using Rubik.Api.Configuration;

namespace Rubik.Integration.Test;

/// <summary>Hosts the real API (Startup.cs) in memory with a known UI URL for the Swagger top-bar test.</summary>
public sealed class RubikWebApplicationFactory : WebApplicationFactory<Startup>
{
    public const string UiUrl = "http://ui.example.test:5173";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Added last, so it overrides appsettings.json (UseSetting would only set host configuration,
        // which appsettings.json overrides).
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [RubikConstants.ConfigurationKey.UiUrl] = UiUrl,
        }));
    }
}
