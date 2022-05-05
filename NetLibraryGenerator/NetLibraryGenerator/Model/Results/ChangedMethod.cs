using Newtonsoft.Json;

namespace NetLibraryGenerator.Model.Results;

public class ChangedMethod
{
    public ChangedMethod(string category, string name, string oldSignature, string newSignature)
    {
        Category = category;
        Name = name;
        OldSignature = oldSignature;
        NewSignature = newSignature;
    }

    [JsonProperty("category")]
    public string Category { get; }

    [JsonProperty("name")]
    public string Name { get; }
    
    [JsonProperty("oldSignature")]
    public string OldSignature { get; }
    
    [JsonProperty("newSignature")]
    public string NewSignature { get; }
}