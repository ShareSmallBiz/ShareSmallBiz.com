namespace ApiTestGenerator.Models
{
    public class RequestBodyObject
    {
        public bool Required { get; set; }
        public Dictionary<string, MediaTypeObject> Content { get; set; }
    }
}
