using ApiTestGenerator.Models;
using HttpClientUtility.ClientService;
using System.Text;
using System.Text.Json;

namespace ApiTestGenerator.Services
{
    public class ApiTestService : IApiTestService
    {
        private readonly IHttpClientService _httpClientService;

        public ApiTestService(IHttpClientService httpClientService)
        {
            _httpClientService = httpClientService;
        }

        public async Task<(string ResponseContent, string Status)> ExecuteApiTest(ApiTestViewModel viewModel)
        {
            var baseUrl = viewModel.SelectedServer;
            var path = viewModel.SelectedPath;

            // Replace path parameters
            foreach (var param in viewModel.PathParameters)
            {
                path = path.Replace($"{{{param.Key}}}", Uri.EscapeDataString(param.Value));
            }

            // Add query parameters
            var queryString = string.Empty;
            if (viewModel.QueryParameters.Any())
            {
                queryString = "?" + string.Join("&", viewModel.QueryParameters
                    .Where(p => !string.IsNullOrEmpty(p.Value))
                    .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
            }

            var fullUrl = new Uri($"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}{queryString}");

            // Create HttpClient
            var client = _httpClientService.CreateConfiguredClient();

            // Add headers
            foreach (var header in viewModel.Headers)
            {
                if (!string.IsNullOrEmpty(header.Value))
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            try
            {
                HttpResponseMessage response;

                switch (viewModel.SelectedMethod.ToUpper())
                {
                    case "GET":
                        response = await client.GetAsync(fullUrl);
                        break;
                    case "POST":
                        var postContent = new StringContent(viewModel.RequestBody, Encoding.UTF8, "application/json");
                        response = await client.PostAsync(fullUrl, postContent);
                        break;
                    case "PUT":
                        var putContent = new StringContent(viewModel.RequestBody, Encoding.UTF8, "application/json");
                        response = await client.PutAsync(fullUrl, putContent);
                        break;
                    case "DELETE":
                        response = await client.DeleteAsync(fullUrl);
                        break;
                    case "PATCH":
                        var patchContent = new StringContent(viewModel.RequestBody, Encoding.UTF8, "application/json");
                        response = await client.PatchAsync(fullUrl, patchContent);
                        break;
                    default:
                        return ("Unsupported HTTP method", "Error");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var status = $"{(int)response.StatusCode} {response.StatusCode}";

                return (responseContent, status);
            }
            catch (Exception ex)
            {
                return ($"Error executing request: {ex.Message}", "Error");
            }
        }

        public object BuildRequestPayload(ApiTestViewModel viewModel)
        {
            if (string.IsNullOrEmpty(viewModel.RequestBody))
                return null;

            try
            {
                return JsonSerializer.Deserialize<object>(viewModel.RequestBody);
            }
            catch
            {
                return viewModel.RequestBody;
            }
        }
    }
}
