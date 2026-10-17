using NUnit.Framework;

namespace Rubik.Integration.Test;

/// <summary>Starts the real API in memory once per fixture, with an <see cref="HttpClient"/> for its tests.</summary>
internal abstract class ApiTestBase
{
    protected RubikWebApplicationFactory Factory { get; private set; } = null!;

    protected HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public void StartApi()
    {
        Factory = new RubikWebApplicationFactory();
        Client = Factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task StopApi()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }
}
