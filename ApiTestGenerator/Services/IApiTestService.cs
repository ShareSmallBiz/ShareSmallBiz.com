using ApiTestGenerator.Models;

namespace ApiTestGenerator.Services
{
    public interface IApiTestService
    {
        Task<(string ResponseContent, string Status)> ExecuteApiTest(ApiTestViewModel viewModel);
        object BuildRequestPayload(ApiTestViewModel viewModel);
    }
}
