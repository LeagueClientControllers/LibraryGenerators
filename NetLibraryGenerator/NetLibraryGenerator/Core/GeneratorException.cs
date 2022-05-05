namespace NetLibraryGenerator.Core
{
    public class GeneratorException : Exception
    {
        public GeneratorException(string message) 
            : base($"Exception during library generation: {message}") { }

        public override string ToString()
        {
            return $"{Message} occured " +
                   $"{StackTrace?.Split("\r\n").FirstOrDefault()?.Replace("   ", "")}";
        }
    }
}