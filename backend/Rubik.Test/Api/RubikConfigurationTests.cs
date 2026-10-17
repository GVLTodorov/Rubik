using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Rubik.Api.Configuration;

namespace Rubik.Test.Api;

[TestFixture]
internal sealed class RubikConfigurationTests
{
    private static RubikConfiguration Build(Dictionary<string, string?> settings) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

    [Test]
    public void Defaults_PointAtTheViteDevServerWithAnHourOfSessionExpiry()
    {
        var config = Build([]);

        Assert.That(config.UiUrl, Is.EqualTo("http://localhost:5173"));
        Assert.That(config.CorsOrigins, Is.EqualTo(new[] { "http://localhost:5173" }));
        Assert.That(config.SessionSlidingExpiration, Is.EqualTo(TimeSpan.FromMinutes(60)));
    }

    [Test]
    public void ReadsEverySetting()
    {
        var config = Build(new()
        {
            ["links:ui"] = "http://ui.example.test",
            ["cors:origins:0"] = "http://a.example.test",
            ["cors:origins:1"] = "http://b.example.test",
            ["sessions:slidingExpirationMinutes"] = "5",
        });

        Assert.That(config.UiUrl, Is.EqualTo("http://ui.example.test"));
        Assert.That(config.CorsOrigins, Is.EqualTo(new[] { "http://a.example.test", "http://b.example.test" }));
        Assert.That(config.SessionSlidingExpiration, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public void CorsOrigins_SkipBlankEntriesAndFallBackToTheUiUrl()
    {
        var config = Build(new()
        {
            ["links:ui"] = "http://ui.example.test",
            ["cors:origins:0"] = " ",
        });

        Assert.That(config.CorsOrigins, Is.EqualTo(new[] { "http://ui.example.test" }));
    }

    [TestCase("0")]
    [TestCase("-10")]
    public void SessionExpiry_FallsBackToTheDefaultWhenNotPositive(string minutes)
    {
        var config = Build(new() { ["sessions:slidingExpirationMinutes"] = minutes });

        Assert.That(config.SessionSlidingExpiration, Is.EqualTo(TimeSpan.FromMinutes(60)));
    }
}
