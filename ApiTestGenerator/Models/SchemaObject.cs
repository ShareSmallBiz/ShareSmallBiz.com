using System.Text.Json.Serialization;

namespace ApiTestGenerator.Models
{
    public class SchemaObject
    {
        public string Type { get; set; }
        public string Format { get; set; }
        public Dictionary<string, SchemaObject> Properties { get; set; }
        public List<string> Required { get; set; }
        public SchemaObject Items { get; set; }
        public string Ref { get; set; }
        public List<SchemaObject> OneOf { get; set; }
        public List<SchemaObject> AnyOf { get; set; }
        public List<SchemaObject> AllOf { get; set; }
        public object Default { get; set; }
        public object Example { get; set; }

        [JsonPropertyName("$ref")]
        public string Reference { get; set; }

        public string GetRefName()
        {
            if (!string.IsNullOrEmpty(Reference))
            {
                return Reference.Split('/').Last();
            }
            return null;
        }
    }
}
