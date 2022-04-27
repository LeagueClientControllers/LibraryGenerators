using Newtonsoft.Json;

namespace NetLibraryGenerator.SchemeModel;

public class ApiCategory
{
    [JsonProperty("name")] 
    public string Name { get; set; } = null!;

    [JsonProperty("docs")]
    public JsDocumentationNode[] Docs { get; set; } = null!;

    [JsonProperty("methods")] 
    public ApiMethod[] Methods { get; set; } = null!;
}