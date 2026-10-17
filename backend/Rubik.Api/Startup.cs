using System.Reflection;
using Microsoft.OpenApi;
using Rubik.Api.Configuration;
using Rubik.Api.Errors;
using Rubik.Api.Extensions;
using Rubik.Api.Swagger;
using Rubik.Core.Notation;
using Rubik.Core.Solving;

namespace Rubik.Api;

public class Startup(IConfiguration configuration)
{
    private readonly RubikConfiguration _config = new(configuration);

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IRubikConfiguration>(_config)
            .AddSingleton<IMoveParser, MoveParser>()
            .AddSingleton<IScrambleGenerator, ScrambleGenerator>()
            .AddSingleton<ISolver, BeginnerSolver>();

        // Cube sessions live in memory (see CubeController).
        services.AddMemoryCache();

        services.AddControllers();
        services.AddProblemDetails();
        services.AddExceptionHandler<CubeExceptionHandler>();
        services.AddHealthChecks();

        services.AddCors(options => options.AddPolicy(
            RubikConstants.Policy.Cors,
            policy => policy.WithOrigins([.. _config.CorsOrigins]).AllowAnyHeader().AllowAnyMethod()));

        ConfigureSwagger(services);
    }

    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Rubik's Cube API",
                Version = "v1",
                Description = "3×3 cube engine, all under /api/cube. POST /api/cube starts a session: the server keeps a cube " +
                              "and its undo/redo history in memory under a session id (expires after a period without use). " +
                              "/api/cube/scramble and /api/cube/{id}/solution only suggest moves; send them to /api/cube/{id}/moves " +
                              "to turn them. Solved orientation: Green front, Red right, White up.",
            });
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
            options.SchemaFilter<CubeStateExampleSchemaFilter>();
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseExceptionHandler()
           .UseStatusCodePages();

        // Swagger is on in every environment so reviewers can always exercise the API.
        app.UseSwagger()
           .UseSwaggerUI(options =>
           {
               options.DocumentTitle = "Rubik's Cube API";
               options.SwaggerEndpoint("/swagger/v1/swagger.json", "Rubik's Cube API v1");
               SwaggerExtension.AddTopBarLinks(options, _config);
           });

        app.UseRouting()
           .UseCors(RubikConstants.Policy.Cors);

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks(RubikConstants.Route.Health);
        });
    }
}
