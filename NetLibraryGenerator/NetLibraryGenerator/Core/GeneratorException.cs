namespace NetLibraryGenerator.Core
{
    public class GeneratorException : Exception
    {
        public GeneratorException(string message) : base($"Exception during library generation. {message}") { }
    }
}