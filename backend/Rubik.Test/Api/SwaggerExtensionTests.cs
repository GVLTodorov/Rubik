using NUnit.Framework;
using Rubik.Api.Extensions;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Rubik.Test.Api;

[TestFixture]
internal sealed class SwaggerExtensionTests
{
    [Test]
    public void AddTopBarLinks_AddsAUiButtonForTheConfiguredUrl()
    {
        var options = new SwaggerUIOptions();

        SwaggerExtension.AddTopBarLinks(options, new TestRubikConfiguration { UiUrl = "http://ui.example.test:5173" });

        Assert.That(options.HeadContent, Does.Contain("""[["UI","http://ui.example.test:5173"]]"""));
        Assert.That(options.HeadContent, Does.Contain(".rubik-links"));
    }

    [Test]
    public void AddTopBarLinks_KeepsExistingHeadContent()
    {
        var options = new SwaggerUIOptions { HeadContent = "<meta name=\"existing\">" };

        SwaggerExtension.AddTopBarLinks(options, new TestRubikConfiguration());

        Assert.That(options.HeadContent, Does.StartWith("<meta name=\"existing\">"));
    }

    [Test]
    public void AddTopBarLinks_EscapesTheUrlSoItCannotCloseTheScriptTag()
    {
        var options = new SwaggerUIOptions();

        SwaggerExtension.AddTopBarLinks(options, new TestRubikConfiguration { UiUrl = "http://x/</script><script>alert(1)</script>" });

        Assert.That(options.HeadContent, Does.Not.Contain("</script><script>alert(1)"));
    }
}
