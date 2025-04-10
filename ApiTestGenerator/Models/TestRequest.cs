namespace ApiTestGenerator.Models
{
    public class TestRequest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }
        public Dictionary<string, string> PathParameters { get; set; }
        public Dictionary<string, string> QueryParameters { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string RequestBody { get; set; }
        public string BaseUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
