using Newtonsoft.Json;

namespace NetLibraryGenerator.SchemeModel;

public class ApiModel
{
    [JsonProperty("declarations")]
    public ApiEntityDeclaration[] Declarations { get; set; } = null!;
    
    [JsonProperty("entities")]
    public ApiEntity[] Entities { get; set; } = null!;

    [JsonProperty("eventIds")]
    public int[] EventIds { get; set; } = null!;
    
    [JsonProperty("enums")]
    public ApiEnum[] Enums { get; set; } = null!;
}