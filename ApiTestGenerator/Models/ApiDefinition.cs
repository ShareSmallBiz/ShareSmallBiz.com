using System.Text.Json.Serialization;

namespace ApiTestGenerator.Models;

public class ApiDefinition
{
    public string Title { get; set; }
    public string Version { get; set; }
    public Dictionary<string, PathItem> Paths { get; set; }
    public Dictionary<string, SchemaObject> Components { get; set; }
    public Dictionary<string, ServerObject> Servers { get; set; }
}
