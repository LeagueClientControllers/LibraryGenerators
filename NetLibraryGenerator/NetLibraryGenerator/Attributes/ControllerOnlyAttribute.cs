namespace NetLibraryGenerator.Attributes
{
    /// <summary>
    /// Attributed method can only be invoked when DeviceType of access token is Controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ControllerOnlyAttribute : Attribute { }
}