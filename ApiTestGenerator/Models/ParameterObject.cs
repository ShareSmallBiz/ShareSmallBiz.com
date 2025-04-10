namespace ApiTestGenerator.Models
{
    public class ParameterObject
    {
        public string Name { get; set; }
        public string In { get; set; } // path, query, header, cookie
        public bool Required { get; set; }
        public string Description { get; set; }
        public SchemaObject Schema { get; set; }
    }
}
