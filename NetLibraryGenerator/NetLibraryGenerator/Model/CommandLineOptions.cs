using CommandLine;

namespace NetLibraryGenerator.Model;

public class CommandLineOptions
{
    [Option('s', "scheme", Required = true, HelpText = "Path to API scheme")]
    public string ApiSchemePath { get; set; } = null!;

    [Option('l', "library", Required = true, HelpText = "Path to the LarcApiNet project folder")]
    public string LibraryPath { get; set; } = null!;
}