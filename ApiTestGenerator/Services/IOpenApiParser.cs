using ApiTestGenerator.Models;
using HttpClientUtility.ClientService;
using System.Text;
using System.Text.Json;

namespace ApiTestGenerator.Services;

public interface IOpenApiParser
{
    ApiDefinition ParseOpenApiJson(string json);
}
