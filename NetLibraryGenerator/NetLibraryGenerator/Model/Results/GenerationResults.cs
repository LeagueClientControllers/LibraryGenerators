using Newtonsoft.Json;

namespace NetLibraryGenerator.Model.Results;

public class GenerationResults
{
    private GenerationResults() {}
    public static readonly GenerationResults Instance = new GenerationResults();
    
    [JsonProperty("changedMethods")]
    public List<ChangedMethod> ChangedMethods { get; } = new();
    
    [JsonProperty("addedMethods")]
    public List<AddedMethod> AddedMethods { get; } = new();
    
    [JsonProperty("addedCategories")]
    public List<string> AddedCategories { get; } = new();

    [JsonProperty("modelEntitiesCount")]
    public int ModelEntitiesCount { get; set; }
    
    [JsonProperty("modelEventsCount")]
    public int ModelEventsCount { get; set; }
    
    [JsonProperty("modelResponsesCount")]
    public int ModelResponsesCount { get; set; }
    
    [JsonProperty("modelParametersCount")]
    public int ModelParametersCount { get; set;}
    
    [JsonProperty("modelEnumsCount")]
    public int ModelEnumsCount { get; set; }
}