using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Rubik.Api.Errors;
using Rubik.Core;
using Rubik.Core.Notation;
using Rubik.Core.Solving;

namespace Rubik.Test.Api;

[TestFixture]
internal sealed class CubeExceptionHandlerTests
{
    private CapturingProblemDetailsService _problemDetails = null!;
    private ListLogger<CubeExceptionHandler> _logger = null!;

    [SetUp]
    public void CreateDoubles()
    {
        _problemDetails = new CapturingProblemDetailsService();
        _logger = new ListLogger<CubeExceptionHandler>();
    }

    private async Task<HttpContext> HandleAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";

        var handled = await new CubeExceptionHandler(_problemDetails, _logger)
            .TryHandleAsync(context, exception, CancellationToken.None);

        Assert.That(handled, Is.True);
        return context;
    }

    [Test]
    public async Task InvalidMove_Is400WithTokenAndPosition()
    {
        var context = await HandleAsync(new MoveParseException("Q", 2, 2));

        var problem = _problemDetails.Written!.ProblemDetails;
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(problem.Title, Is.EqualTo("Invalid move"));
        Assert.That(problem.Extensions["token"], Is.EqualTo("Q"));
        Assert.That(problem.Extensions["position"], Is.EqualTo(2));
        Assert.That(problem.Extensions["tokenNumber"], Is.EqualTo(2));
    }

    [Test]
    public async Task InvalidState_Is400WithTheMessage()
    {
        var context = await HandleAsync(new InvalidCubeStateException("Face 'D' is missing."));

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(_problemDetails.Written!.ProblemDetails.Title, Is.EqualTo("Invalid cube state"));
        Assert.That(_problemDetails.Written.ProblemDetails.Detail, Is.EqualTo("Face 'D' is missing."));
    }

    [Test]
    public async Task UnsolvableCube_Is422()
    {
        var context = await HandleAsync(new UnsolvableCubeException("A corner is twisted."));

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status422UnprocessableEntity));
        Assert.That(_problemDetails.Written!.ProblemDetails.Title, Is.EqualTo("Unsolvable cube"));
        Assert.That(_problemDetails.Written.ProblemDetails.Detail, Is.EqualTo("A corner is twisted."));
    }

    [Test]
    public async Task HandledErrors_AreLoggedAsWarnings()
    {
        await HandleAsync(new InvalidCubeStateException("Face 'D' is missing."));

        Assert.That(_logger.Entries, Has.Count.EqualTo(1));
        var entry = _logger.Entries[0];
        Assert.That(entry.Level, Is.EqualTo(LogLevel.Warning));
        Assert.That(entry.Message, Does.Contain("/api/test"));
        Assert.That(entry.Message, Does.Contain("Face 'D' is missing."));
    }

    [Test]
    public async Task AnythingElse_IsAGeneric500WithoutDetailsAndIsLoggedAsAnError()
    {
        var boom = new InvalidOperationException("secret internals");

        var context = await HandleAsync(boom);

        var problem = _problemDetails.Written!.ProblemDetails;
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        Assert.That(problem.Title, Is.EqualTo("An unexpected error occurred."));
        Assert.That(problem.Detail, Is.Null);

        Assert.That(_logger.Entries, Has.Count.EqualTo(1));
        var entry = _logger.Entries[0];
        Assert.That(entry.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(entry.Exception, Is.SameAs(boom));
    }
}
