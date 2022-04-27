using Newtonsoft.Json;

namespace NetLibraryGenerator.SchemeModel;

public class ApiEnumMember
{
    [JsonProperty("name")] 
    public string Name { get; set; } = null!;
    
    [JsonProperty("value")] 
    public string Value { get; set; } = null!;
    
    [JsonProperty("docs")] 
    public JsDocumentationNode[] Docs { get; set; } = null!;
}