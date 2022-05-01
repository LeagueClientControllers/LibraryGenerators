namespace NetLibraryGenerator.Utilities
{
    public static class ConsoleUtils
    {
        public const ConsoleColor DEFAULT_COLOR = ConsoleColor.Green;

        public static void ShowInfo(string line)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[INFO]\t{line}");
            Console.ForegroundColor = DEFAULT_COLOR;
        }

        public static void ShowWarning(string line)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN]\t{line}");
            Console.ForegroundColor = DEFAULT_COLOR;
        }

        public static void ShowError(string line)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[ERROR]\t{line}");
            Console.ForegroundColor = DEFAULT_COLOR;
        }
    }
}
