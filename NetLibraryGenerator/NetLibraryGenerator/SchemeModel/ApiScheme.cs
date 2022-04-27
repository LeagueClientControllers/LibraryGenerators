using Newtonsoft.Json;

namespace NetLibraryGenerator.SchemeModel;

public class ApiScheme
{
    [JsonProperty("apiVersion")] 
    public string ApiVersion { get; set; } = null!;

    [JsonProperty("schemeVersion")]
    public string SchemeVersion { get; set; } = default!;
    
    [JsonProperty("generatedAt")] 
    public string GeneratedAt { get; set; } = null!;

    [JsonProperty("model")]
    public ApiModel Model { get; set; } = null!;
    
    [JsonProperty("categories")] 
    public ApiCategory[] Categories { get; set; } = null!;
}