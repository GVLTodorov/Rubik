using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using Rubik.Api.Contracts;

namespace Rubik.Integration.Test;

[TestFixture]
internal sealed class SolverEndpointTests : ApiTestBase
{
    private const string ProblemJson = "application/problem+json";


    private async Task<Guid> CreateScrambledSessionAsync(string moves)
    {
        using var created = await Client.PostAsync("/api/cube", content: null);
        var session = await created.Content.ReadFromJsonAsync<SessionDto>();
        using var turned = await Client.PostAsJsonAsync($"/api/cube/{session!.Id}/moves", new { moves });
        turned.EnsureSuccessStatusCode();
        return session.Id;
    }

    [Test]
    public async Task SessionSolution_AppliedToTheSession_SolvesIt()
    {
        var id = await CreateScrambledSessionAsync("F R' U B' L D' R2 U' F B2");

        var solution = await Client.GetFromJsonAsync<SolutionResponse>($"/api/cube/{id}/solution");
        Assert.That(solution, Is.Not.Null);
        Assert.That(solution.Steps.Select(step => step.Title), Is.EqualTo(new[] { "White cross", "White corners", "Middle layer", "Yellow cross", "Yellow face", "Yellow corners", "Yellow edges" }));
        Assert.That(solution.MoveCount, Is.InRange(1, 250));

        // Suggesting does not change the session.
        var before = await Client.GetFromJsonAsync<SessionDto>($"/api/cube/{id}");
        Assert.That(before!.History.Count, Is.EqualTo(10));

        using var applied = await Client.PostAsJsonAsync($"/api/cube/{id}/moves", new { moves = solution.Moves });
        var update = await applied.Content.ReadFromJsonAsync<SessionUpdateResponse>();

        Assert.That(applied.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(update!.Session.State.Faces.Values.Select(face => face.Distinct().Count()), Is.All.EqualTo(1));
        Assert.That(update.AppliedMoves.Count, Is.EqualTo(solution.MoveCount));
    }

    [Test]
    public async Task SessionSolution_ForAnUnknownSession_Is404()
    {
        using var response = await Client.GetAsync($"/api/cube/{Guid.NewGuid()}/solution");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(ProblemJson));
    }
}
