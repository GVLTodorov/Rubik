namespace Rubik.Api;

/// <summary>Builds and runs the web host; services and the request pipeline are in <see cref="Startup"/>.</summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args)
            .Build()
            .RunAsync()
            .ConfigureAwait(true);
    }

    /// <summary>The host builder, also found by <c>WebApplicationFactory</c> in the integration tests.</summary>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
