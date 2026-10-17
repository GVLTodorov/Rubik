using System.Text.Json;
using Microsoft.OpenApi;
using Rubik.Api.Contracts;
using Rubik.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rubik.Api.Swagger;

/// <summary>Gives <see cref="CubeStateDto"/> a solved-cube example in Swagger.</summary>
public sealed class CubeStateExampleSchemaFilter : ISchemaFilter
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(CubeStateDto) && schema is OpenApiSchema concrete)
        {
            concrete.Example = JsonSerializer.SerializeToNode(CubeStateMapper.ToDto(Cube.Solved()), WebJson);
        }
    }
}
