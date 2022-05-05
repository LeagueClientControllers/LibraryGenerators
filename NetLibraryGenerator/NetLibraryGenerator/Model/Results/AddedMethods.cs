using Newtonsoft.Json;

namespace NetLibraryGenerator.Model.Results;

public class AddedMethod
{
    public AddedMethod(string category, string name)
    {
        Category = category;
        Name = name;
    }

    [JsonProperty("category")]
    public string Category { get; }

    [JsonProperty("name")]
    public string Name { get; }
}