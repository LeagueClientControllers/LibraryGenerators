using Newtonsoft.Json;
using Ardalis.SmartEnum;
using Ardalis.SmartEnum.JsonNet;

namespace NetLibraryGenerator.SchemeModel;

public class ApiPropertyType
{ 
    [JsonProperty("primitive")]
    [JsonConverter(typeof(SmartEnumNameConverter<PrimitiveType, int>))]
    public PrimitiveType? Primitive { get; set; }

    [JsonProperty("nullable")]
    public bool Nullable { get; set; }

    [JsonProperty("referenceId")] 
    public int? ReferenceId { get; set; }

    [JsonProperty("genericTypeArguments")] 
    public ApiPropertyType[] GenericTypeArguments { get; set; } = null!;
}

public class PrimitiveType : SmartEnum<PrimitiveType>
{
    private PrimitiveType(string name, int value): base(name, value) {}
    
    public static readonly PrimitiveType Number     = new PrimitiveType("Number", 1);
    public static readonly PrimitiveType Decimal    = new PrimitiveType("Decimal", 2);
    public static readonly PrimitiveType String     = new PrimitiveType("String", 3);
    public static readonly PrimitiveType Boolean    = new PrimitiveType("Boolean", 4);
    public static readonly PrimitiveType Object     = new PrimitiveType("Object", 5);
    public static readonly PrimitiveType Date       = new PrimitiveType("Date", 6);
    public static readonly PrimitiveType Array      = new PrimitiveType("Array", 7);
    public static readonly PrimitiveType Dictionary = new PrimitiveType("Dictionary", 8);
}
