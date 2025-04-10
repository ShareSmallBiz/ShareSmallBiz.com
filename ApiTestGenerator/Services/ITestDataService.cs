using ApiTestGenerator.Models;

namespace ApiTestGenerator.Services
{
    public interface ITestDataService
    {
        List<TestRequest> GetAllTestRequests();
        TestRequest GetTestRequest(string id);
        TestRequest SaveTestRequest(ApiTestViewModel viewModel);
        void DeleteTestRequest(string id);
    }
}
