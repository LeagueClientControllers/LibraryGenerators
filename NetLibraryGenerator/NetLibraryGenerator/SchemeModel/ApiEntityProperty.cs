using Newtonsoft.Json;

namespace NetLibraryGenerator.SchemeModel;

public class ApiEntityProperty
{
    [JsonProperty("name")] 
    public string Name { get; set; } = null!;

    [JsonProperty("jsonName")] 
    public string JsonName { get; set; } = null!;

    [JsonProperty("type")] 
    public ApiPropertyType Type { get; set; } = null!;

    [JsonProperty("initialValue")] 
    public object? InitialValue { get; set; }

    [JsonProperty("docs")] 
    public JsDocumentationNode[] Docs { get; set; } = null!;

    [JsonProperty("modifiable")] 
    public bool Modifiable { get; set; }
}
