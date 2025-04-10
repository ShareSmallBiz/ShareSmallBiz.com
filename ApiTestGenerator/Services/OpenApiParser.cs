using ApiTestGenerator.Models;
using System.Text.Json;

namespace ApiTestGenerator.Services
{
    public class OpenApiParser : IOpenApiParser
    {
        public ApiDefinition ParseOpenApiJson(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                var apiDefinition = JsonSerializer.Deserialize<ApiDefinition>(json, options);
                return apiDefinition;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse OpenAPI JSON: {ex.Message}");
            }
        }
    }
}
