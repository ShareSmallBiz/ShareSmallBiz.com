using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ShareSmallBiz.Api.OpenApi;

/// <summary>
/// Example value published on a property's OpenAPI schema. Doc tools (API Test Spark, Scalar) use it
/// to pre-fill request bodies, so examples across related requests are chosen to work together.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class OpenApiExampleAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}

/// <summary>Copies <see cref="OpenApiExampleAttribute"/> values onto property schemas.</summary>
internal sealed class ExampleSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var example = context.JsonPropertyInfo?.AttributeProvider?
            .GetCustomAttributes(typeof(OpenApiExampleAttribute), inherit: false)
            .OfType<OpenApiExampleAttribute>()
            .FirstOrDefault();
        // OpenAPI 3.1 "examples" array; API Test Spark scaffolds request bodies from examples[0]
        if (example is not null)
            schema.Examples = [JsonValue.Create(example.Value)];
        return Task.CompletedTask;
    }
}
