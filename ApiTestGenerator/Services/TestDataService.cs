using ApiTestGenerator.Models;
using System.Text.Json;

namespace ApiTestGenerator.Services
{
    public class TestDataService : ITestDataService
    {
        private readonly string _dataFilePath;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public TestDataService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _dataFilePath = Path.Combine(hostingEnvironment.ContentRootPath, "App_Data", "TestRequests.json");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Ensure file exists
            if (!File.Exists(_dataFilePath))
            {
                File.WriteAllText(_dataFilePath, "[]");
            }
        }

        public List<TestRequest> GetAllTestRequests()
        {
            var json = File.ReadAllText(_dataFilePath);
            return JsonSerializer.Deserialize<List<TestRequest>>(json) ?? new List<TestRequest>();
        }

        public TestRequest GetTestRequest(string id)
        {
            var tests = GetAllTestRequests();
            return tests.FirstOrDefault(t => t.Id == id);
        }

        public TestRequest SaveTestRequest(ApiTestViewModel viewModel)
        {
            var tests = GetAllTestRequests();

            var test = new TestRequest
            {
                Id = Guid.NewGuid().ToString(),
                Name = viewModel.SavedTestName,
                Path = viewModel.SelectedPath,
                Method = viewModel.SelectedMethod,
                PathParameters = viewModel.PathParameters,
                QueryParameters = viewModel.QueryParameters,
                Headers = viewModel.Headers,
                RequestBody = viewModel.RequestBody,
                BaseUrl = viewModel.SelectedServer,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            tests.Add(test);
            SaveTests(tests);

            return test;
        }

        public void DeleteTestRequest(string id)
        {
            var tests = GetAllTestRequests();
            var test = tests.FirstOrDefault(t => t.Id == id);

            if (test != null)
            {
                tests.Remove(test);
                SaveTests(tests);
            }
        }

        private void SaveTests(List<TestRequest> tests)
        {
            var json = JsonSerializer.Serialize(tests, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFilePath, json);
        }
    }
}