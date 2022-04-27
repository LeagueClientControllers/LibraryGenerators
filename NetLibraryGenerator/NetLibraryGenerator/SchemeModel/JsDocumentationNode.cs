using Newtonsoft.Json;

namespace NetLibraryGenerator.SchemeModel;

public class JsDocumentationNode
{
    [JsonProperty("text")]
    public string Text { get; set; } = null!;

    [JsonProperty("isReference")] 
    public bool IsReference { get; set; }
}