using Newtonsoft.Json;

namespace NetLibraryGenerator.Model;

public class GenerationResults
{
    [JsonProperty("changedMethods")]
    public List<ChangedMethod> ChangedMethods { get; } = new();
}