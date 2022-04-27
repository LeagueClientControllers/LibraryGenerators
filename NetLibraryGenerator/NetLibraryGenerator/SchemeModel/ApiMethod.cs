using Ardalis.SmartEnum;
using Ardalis.SmartEnum.JsonNet;
using Newtonsoft.Json;

namespace NetLibraryGenerator.SchemeModel;

public class ApiMethod
{
    [JsonProperty("name")]
    public string Name { get; set; } = null!;
    
    [JsonProperty("docs")]
    public JsDocumentationNode[] Docs { get; set; } = null!;
    
    [JsonProperty("parametersId")]
    public int? ParametersId { get; set; }
    
    [JsonProperty("responseId")]
    public int ResponseId { get; set; }
    
    [JsonProperty("requireAccessToken")]
    public bool RequireAccessToken { get; set; }
    
    [JsonProperty("accessibleFrom")]
    [JsonConverter(typeof(SmartEnumNameConverter<MethodAccessPolicy, int>))]
    public MethodAccessPolicy AccessibleFrom { get; set; } = null!;
}

public class MethodAccessPolicy : SmartEnum<MethodAccessPolicy>
{
    public MethodAccessPolicy(string name, int value): base(name, value) { }
    
    public static readonly MethodAccessPolicy Controller = new MethodAccessPolicy("Controller", 1);
    public static readonly MethodAccessPolicy Device     = new MethodAccessPolicy("Device", 2);
    public static readonly MethodAccessPolicy Both       = new MethodAccessPolicy("Both", 3);
}