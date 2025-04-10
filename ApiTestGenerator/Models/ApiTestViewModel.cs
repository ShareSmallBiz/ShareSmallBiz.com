namespace ApiTestGenerator.Models
{
    public class ApiTestViewModel
    {
        public string SelectedServer { get; set; }
        public string SelectedPath { get; set; }
        public string SelectedMethod { get; set; }
        public Dictionary<string, string> PathParameters { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> QueryParameters { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string RequestBody { get; set; }
        public string SavedTestName { get; set; }
        public List<TestRequest> SavedTests { get; set; } = new List<TestRequest>();
        public ApiDefinition ApiDefinition { get; set; }
        public string ApiResponse { get; set; }
        public string ApiResponseStatus { get; set; }
        public Dictionary<string, object> FormattedRequestBody { get; set; } = new Dictionary<string, object>();
    }
}