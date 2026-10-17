using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rubik.Api.Configuration;
using Rubik.Api.Controllers;
using Rubik.Core.Notation;
using Rubik.Core.Solving;

namespace Rubik.Test.Api;

/// <summary>Fixed configuration values, so tests don't depend on appsettings.json.</summary>
internal sealed class TestRubikConfiguration : IRubikConfiguration
{
    public string UiUrl { get; init; } = "http://ui.example.test";

    public IReadOnlyList<string> CorsOrigins { get; init; } = ["http://ui.example.test"];

    public TimeSpan SessionSlidingExpiration { get; init; } = TimeSpan.FromMinutes(60);
}

/// <summary>A memory-cache clock the test moves forward by hand.</summary>
internal sealed class ManualClock : ISystemClock
{
    public DateTimeOffset UtcNow { get; private set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan by) => UtcNow += by;
}

/// <summary>Records every log entry with its level, rendered message and exception.</summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception), exception));
}

/// <summary>Captures the problem details instead of writing them to the response.</summary>
internal sealed class CapturingProblemDetailsService : IProblemDetailsService
{
    public ProblemDetailsContext? Written { get; private set; }

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        Written = context;
        return ValueTask.CompletedTask;
    }

    public async ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
    {
        await WriteAsync(context);
        return true;
    }
}

internal static class TestControllers
{
    /// <summary>A <see cref="CubeController"/> with real engine services and its own session cache.</summary>
    public static CubeController Create(ManualClock? clock = null, TimeSpan? slidingExpiration = null) =>
        new(
            new MoveParser(),
            new ScrambleGenerator(),
            new BeginnerSolver(),
            new MemoryCache(new MemoryCacheOptions { Clock = clock ?? new ManualClock() }),
            new TestRubikConfiguration { SessionSlidingExpiration = slidingExpiration ?? TimeSpan.FromMinutes(60) },
            NullLogger<CubeController>.Instance);
}
