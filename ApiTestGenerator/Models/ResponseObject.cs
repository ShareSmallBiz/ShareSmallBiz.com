namespace ApiTestGenerator.Models
{
    public class ResponseObject
    {
        public string Description { get; set; }
        public Dictionary<string, MediaTypeObject> Content { get; set; }
    }
}
