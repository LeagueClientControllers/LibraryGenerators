namespace NetLibraryGenerator.Attributes
{
    /// <summary>
    /// Attributed method can only be invoked when DeviceType of access token is not a controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DeviceOnlyAttribute : Attribute { }
}