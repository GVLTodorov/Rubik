using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;
using Rubik.Api.Contracts;

namespace Rubik.Integration.Test;

[TestFixture]
internal sealed class SessionEndpointTests : ApiTestBase
{
    private const string ProblemJson = "application/problem+json";


    [Test]
    public async Task Create_Returns201WithASolvedSessionAndLocation()
    {
        using var response = await Client.PostAsync("/api/cube", content: null);
        var session = await response.Content.ReadFromJsonAsync<SessionDto>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(session, Is.Not.Null);
        Assert.That(session.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(session.History, Is.Empty);
        Assert.That(session.State.Faces["F"], Is.EqualTo(Enumerable.Repeat("G", 9)));
        Assert.That(response.Headers.Location?.ToString(), Does.EndWith($"/api/cube/{session.Id}").IgnoreCase);
    }

    [Test]
    public async Task Moves_AreKeptOnTheServerAcrossRequests()
    {
        var id = await CreateSessionAsync();

        await PostAsync($"/api/cube/{id}/moves", new { moves = "F R'" });
        var update = await PostAsync($"/api/cube/{id}/moves", new { moves = "U B' L D'" });
        var session = await Client.GetFromJsonAsync<SessionDto>($"/api/cube/{id}");

        Assert.That(update.AppliedMoves, Is.EqualTo(new[] { "U", "B'", "L", "D'" }));
        Assert.That(session!.History, Is.EqualTo(new[] { "F", "R'", "U", "B'", "L", "D'" }));
        Assert.That(string.Join(' ', session.State.Faces["U"]), Is.EqualTo("R O G B W W B B B"));
        Assert.That(string.Join(' ', session.State.Faces["D"]), Is.EqualTo("G G B R Y R R G G"));
    }

    [Test]
    public async Task UndoAndRedo_TurnTheRightMovesAndTrackTheRedoStack()
    {
        var id = await CreateSessionAsync();
        await PostAsync($"/api/cube/{id}/moves", new { moves = "F R'" });

        var undo = await PostAsync($"/api/cube/{id}/undo");
        Assert.That(undo.AppliedMoves, Is.EqualTo(new[] { "R" }));
        Assert.That(undo.Session.History, Is.EqualTo(new[] { "F" }));
        Assert.That(undo.Session.RedoStack, Is.EqualTo(new[] { "R'" }));

        var redo = await PostAsync($"/api/cube/{id}/redo");
        Assert.That(redo.AppliedMoves, Is.EqualTo(new[] { "R'" }));
        Assert.That(redo.Session.History, Is.EqualTo(new[] { "F", "R'" }));
        Assert.That(redo.Session.RedoStack, Is.Empty);
    }

    [Test]
    public async Task Reset_ReturnsToSolvedAndClearsHistory()
    {
        var id = await CreateSessionAsync();
        await PostAsync($"/api/cube/{id}/moves", new { moves = "F R' U" });

        var reset = await PostAsync($"/api/cube/{id}/reset");

        Assert.That(reset.AppliedMoves, Is.Empty);
        Assert.That(reset.Session.History, Is.Empty);
        Assert.That(reset.Session.State.Faces["U"], Is.EqualTo(Enumerable.Repeat("W", 9)));
    }

    [TestCase("undo")]
    [TestCase("redo")]
    public async Task UndoOrRedo_WithNothingToDo_Returns409Problem(string action)
    {
        var id = await CreateSessionAsync();

        using var response = await Client.PostAsync($"/api/cube/{id}/{action}", content: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(ProblemJson));
    }

    [TestCase("GET", "")]
    [TestCase("POST", "/undo")]
    [TestCase("POST", "/reset")]
    public async Task UnknownSession_Returns404Problem(string method, string suffix)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/cube/{Guid.NewGuid()}{suffix}");
        using var response = await Client.SendAsync(request);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(ProblemJson));
        Assert.That(problem.RootElement.GetProperty("detail").GetString(), Does.Contain("not found"));
    }

    [Test]
    public async Task Moves_WithInvalidToken_Returns400AndLeavesTheSessionUnchanged()
    {
        var id = await CreateSessionAsync();
        await PostAsync($"/api/cube/{id}/moves", new { moves = "F" });

        using var response = await Client.PostAsJsonAsync($"/api/cube/{id}/moves", new { moves = "R X" });
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var session = await Client.GetFromJsonAsync<SessionDto>($"/api/cube/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(problem.RootElement.GetProperty("token").GetString(), Is.EqualTo("X"));
        Assert.That(session!.History, Is.EqualTo(new[] { "F" }));
    }

    [Test]
    public async Task ConcurrentMoves_AreAllRecorded()
    {
        var id = await CreateSessionAsync();

        await Task.WhenAll(Enumerable.Range(0, 40).Select(_ => PostAsync($"/api/cube/{id}/moves", new { moves = "R" })));
        var session = await Client.GetFromJsonAsync<SessionDto>($"/api/cube/{id}");

        Assert.That(session!.History.Count, Is.EqualTo(40));
        Assert.That(session.State.Faces["U"], Is.EqualTo(Enumerable.Repeat("W", 9)));
    }

    [Test]
    public async Task Sessions_AreIndependent()
    {
        var first = await CreateSessionAsync();
        var second = await CreateSessionAsync();

        await PostAsync($"/api/cube/{first}/moves", new { moves = "F" });
        var other = await Client.GetFromJsonAsync<SessionDto>($"/api/cube/{second}");

        Assert.That(second, Is.Not.EqualTo(first));
        Assert.That(other!.History, Is.Empty);
    }

    private async Task<Guid> CreateSessionAsync()
    {
        using var response = await Client.PostAsync("/api/cube", content: null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionDto>())!.Id;
    }

    private async Task<SessionUpdateResponse> PostAsync(string url, object? body = null)
    {
        using var response = body is null
            ? await Client.PostAsync(url, content: null)
            : await Client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionUpdateResponse>())!;
    }
}
