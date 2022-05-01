using Newtonsoft.Json;

namespace NetLibraryGenerator.Model;

public class ChangedMethod
{
    public ChangedMethod(string category, string name)
    {
        Category = category;
        Name = name;
    }

    [JsonProperty("category")]
    public string Category { get; }

    [JsonProperty("name")]
    public string Name { get; }
}