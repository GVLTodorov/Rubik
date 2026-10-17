using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;
using Rubik.Api.Contracts;

namespace Rubik.Integration.Test;

[TestFixture]
internal sealed class CubeEndpointTests : ApiTestBase
{
    private const string ProblemJson = "application/problem+json";

    private static readonly Dictionary<string, string> AcceptanceFaces = new()
    {
        ["U"] = "R O G B W W B B B",
        ["L"] = "G Y Y O O G B G O",
        ["F"] = "O R R O G W W W W",
        ["R"] = "Y B O R R W O Y R",
        ["B"] = "Y B W O B Y Y Y W",
        ["D"] = "G G B R Y R R G G",
    };

    [Test]
    public async Task PostMoves_ToANewSession_AppliesTheAcceptanceSequenceToASolvedCube()
    {
        var id = await CreateSessionAsync();

        using var response = await Client.PostAsJsonAsync($"/api/cube/{id}/moves", new { moves = "F R' U B' L D'" });
        var result = await response.Content.ReadFromJsonAsync<SessionUpdateResponse>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AppliedMoves, Is.EqualTo(new[] { "F", "R'", "U", "B'", "L", "D'" }));
        foreach (var (face, expected) in AcceptanceFaces)
        {
            Assert.That(string.Join(' ', result.Session.State.Faces[face]), Is.EqualTo(expected));
        }
    }

    [Test]
    public async Task PostMoves_UsesCamelCaseContract()
    {
        var id = await CreateSessionAsync();

        using var response = await Client.PostAsJsonAsync($"/api/cube/{id}/moves", new { moves = "F" });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(json.RootElement.TryGetProperty("session", out var session), Is.True);
        Assert.That(session.TryGetProperty("redoStack", out _), Is.True);
        Assert.That(session.GetProperty("state").TryGetProperty("faces", out _), Is.True);
        Assert.That(json.RootElement.TryGetProperty("appliedMoves", out _), Is.True);
    }

    [Test]
    public async Task PostMoves_WithInvalidToken_Returns400ProblemWithTokenAndPosition()
    {
        var id = await CreateSessionAsync();

        using var response = await Client.PostAsJsonAsync($"/api/cube/{id}/moves", new { moves = "F X U" });
        using var problem = await ReadProblemAsync(response);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(ProblemJson));
        Assert.That(problem.RootElement.GetProperty("status").GetInt32(), Is.EqualTo(400));
        Assert.That(problem.RootElement.GetProperty("token").GetString(), Is.EqualTo("X"));
        Assert.That(problem.RootElement.GetProperty("position").GetInt32(), Is.EqualTo(2));
        Assert.That(problem.RootElement.GetProperty("detail").GetString(), Does.Contain("'X'"));
    }

    [TestCase("{}")]
    [TestCase("not json")]
    public async Task PostMoves_WithMalformedBody_Returns400Problem(string body)
    {
        var id = await CreateSessionAsync();

        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync($"/api/cube/{id}/moves", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(ProblemJson));
    }

    [Test]
    public async Task GetScramble_DefaultsToTwentyMoves()
    {
        var scramble = await Client.GetFromJsonAsync<ScrambleResponse>("/api/cube/scramble");

        Assert.That(scramble!.Moves.Split(' ').Length, Is.EqualTo(20));
    }

    [Test]
    public async Task GetScramble_WithSeed_IsDeterministic()
    {
        var first = await Client.GetFromJsonAsync<ScrambleResponse>("/api/cube/scramble?length=12&seed=123");
        var second = await Client.GetFromJsonAsync<ScrambleResponse>("/api/cube/scramble?length=12&seed=123");

        Assert.That(first!.Moves.Split(' ').Length, Is.EqualTo(12));
        Assert.That(second!.Moves, Is.EqualTo(first.Moves));
    }

    [TestCase(0)]
    [TestCase(101)]
    public async Task GetScramble_WithLengthOutOfRange_Returns400Problem(int length)
    {
        using var response = await Client.GetAsync($"/api/cube/scramble?length={length}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(ProblemJson));
    }

    [Test]
    public async Task Health_ReturnsOk()
    {
        using var response = await Client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Cors_AllowsTheViteDevOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/cube");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await Client.SendAsync(request);

        Assert.That(response.Headers.GetValues("Access-Control-Allow-Origin"), Is.EqualTo(new[] { "http://localhost:5173" }));
    }

    private async Task<Guid> CreateSessionAsync()
    {
        using var response = await Client.PostAsync("/api/cube", content: null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionDto>())!.Id;
    }

    private static async Task<JsonDocument> ReadProblemAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
