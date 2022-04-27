using Newtonsoft.Json;

namespace NetLibraryGenerator.SchemeModel;

public class ApiEntity
{
    [JsonProperty("id")] 
    public int Id { get; set; }
    
    [JsonProperty("properties")] 
    public ApiEntityProperty[] Properties { get; set; } = null!;

    [JsonProperty("docs")] 
    public JsDocumentationNode[] Docs { get; set; } = null!;
}