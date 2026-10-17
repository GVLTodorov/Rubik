using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Rubik.Integration.Test;

/// <summary>
/// Fetches the real Swagger document and Swagger UI page to check every endpoint is documented and
/// that the injected top-bar "UI" button points at the configured React app.
/// </summary>
[TestFixture]
internal sealed class SwaggerEndpointTests : ApiTestBase
{

    [Test]
    public async Task Root_RedirectsToSwaggerAndIsNotDocumented()
    {
        using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location?.ToString(), Is.EqualTo("swagger"));
        Assert.That((await GetSwaggerDocumentAsync()).GetProperty("paths").TryGetProperty("/", out _), Is.False);
    }

    [Test]
    public async Task SwaggerUi_ServesTopBarLinksWithTheConfiguredUiUrl()
    {
        using var response = await Client.GetAsync("/swagger/index.html");
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(html, Does.Contain("rubik-links"));
        Assert.That(html, Does.Contain(RubikWebApplicationFactory.UiUrl));
        Assert.That(html, Does.Contain("\"UI\""));
    }

    [TestCase("/api/cube", "post")]
    [TestCase("/api/cube/{id}", "get")]
    [TestCase("/api/cube/{id}/moves", "post")]
    [TestCase("/api/cube/{id}/undo", "post")]
    [TestCase("/api/cube/{id}/redo", "post")]
    [TestCase("/api/cube/{id}/reset", "post")]
    [TestCase("/api/cube/{id}/solution", "get")]
    [TestCase("/api/cube/scramble", "get")]
    public async Task SwaggerJson_DocumentsEveryEndpointWithASummary(string path, string method)
    {
        var operation = (await GetSwaggerDocumentAsync())
            .GetProperty("paths")
            .GetProperty(path)
            .GetProperty(method);

        Assert.That(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()), Is.False);
    }

    [Test]
    public async Task SwaggerJson_HasACubeStateExampleAndNoSecuritySchemes()
    {
        var document = await GetSwaggerDocumentAsync();
        var components = document.GetProperty("components");
        var example = components.GetProperty("schemas").GetProperty("CubeStateDto").GetProperty("example");

        Assert.That(example.GetProperty("faces").GetProperty("U").GetArrayLength(), Is.EqualTo(9));
        Assert.That(components.TryGetProperty("securitySchemes", out _), Is.False);
    }

    private async Task<JsonElement> GetSwaggerDocumentAsync()
    {
        using var response = await Client.GetAsync("/swagger/v1/swagger.json");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
