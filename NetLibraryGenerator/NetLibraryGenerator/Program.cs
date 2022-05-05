using System.Text.RegularExpressions;

using CommandLine;

using NetLibraryGenerator.Core;
using NetLibraryGenerator.Model;
using NetLibraryGenerator.Model.Results;
using NetLibraryGenerator.SchemeModel;
using NetLibraryGenerator.Utilities;

using Newtonsoft.Json;

namespace NetLibraryGenerator
{
    public static class Program
    {
        private const string SUPPORTED_SCHEME_VERSION = "1.1.0";

        private static ApiScheme? _scheme;

        private static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleUtils.DEFAULT_COLOR;
            
            try {
                await Parser.Default.ParseArguments<CommandLineOptions>(args).MapResult(Run,
                    _ => {
                        Console.ForegroundColor = ConsoleColor.White;
                        return Task.FromResult(-1);
                    });
            } catch (GeneratorException e) {
                ConsoleUtils.ShowError(e.ToString());
            } catch (Exception e) {
                ConsoleUtils.ShowError($"{e.GetType()}: {e.Message} Occured " +
                                       $"{e.StackTrace?.Split("\r\n").FirstOrDefault()?.Replace("   ", "")}");
            }
        }

        private static async Task Run(CommandLineOptions options)
        {
            options.ApiSchemePath = new Regex("(\'|\")")
                .Replace(options.ApiSchemePath, "");
            options.LibraryPath = new Regex("(\'|\")")
                .Replace(options.LibraryPath, "");
            options.JsonOutputPath = new Regex("(\'|\")")
                .Replace(options.JsonOutputPath, "");
            
            if (!File.Exists(options.ApiSchemePath)) {
                throw new ArgumentException(
                    "API scheme path should be valid path to an existing file.");
            }
            
            if (!Directory.Exists(options.LibraryPath)) {
                throw new ArgumentException(
                    "Library path should be valid path to an existing directory.");
            }

            if (!Directory.Exists(Path.GetDirectoryName(options.JsonOutputPath))) {
                throw new ArgumentException(
                    "Json output directory should be valid path to an existing directory.");
            }

            if (!Path.GetExtension(options.JsonOutputPath).Contains("json")) {
                throw new ArgumentException("Output file must have .json extension");
            }
            
            ConsoleUtils.ShowInfo("Parsing scheme...");

            string schemeContent;
            using (StreamReader reader =
                new StreamReader(new FileStream(options.ApiSchemePath, FileMode.Open))) {
                schemeContent = await reader.ReadToEndAsync();
            }

            try {
                _scheme = JsonConvert.DeserializeObject<ApiScheme>(schemeContent);
            } catch {
                ConsoleUtils.ShowError("Scheme JSON parsing error");
                return;
            }

            if (_scheme == null) {
                ConsoleUtils.ShowError("Scheme structure error");
                return;
            }

            ConsoleUtils.ShowInfo("Scheme is successfully parsed");

            if (_scheme.SchemeVersion != SUPPORTED_SCHEME_VERSION) {
                ConsoleUtils.ShowError(
                    $"Scheme version is unsupported: {SUPPORTED_SCHEME_VERSION} required, {_scheme.SchemeVersion} requested");
                return;
            }

            Console.WriteLine();
            ShowHeader();
            Console.WriteLine();

            await Generator.GenerateLibrary(options.LibraryPath, _scheme);
            Console.WriteLine();
            
            await using (StreamWriter writer = new StreamWriter(new FileStream(options.JsonOutputPath, FileMode.Create))) {
                await writer.WriteAsync(JsonConvert.SerializeObject(GenerationResults.Instance));
            }
            
            ConsoleUtils.ShowInfo($"Results file is generated at [{options.JsonOutputPath}]");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void ShowHeader()
        {
            int maxLineLength = $" SCHEME GENERATED: {_scheme!.GeneratedAt} ".Length + 7 * 2;

            void WriteCenter(string line) {
                string starsString = "";
                int starsCount = (maxLineLength - line.Length - 2) / 2;
                for (int i = 0; i < starsCount; i++)
                {
                    starsString += "*";
                }

                Console.WriteLine($"/{starsString} {line} {starsString}/");
            }

            void WriteLeft(string line) {
                string starsString = "";
                int starsCount = maxLineLength - line.Length - 6;
                for (int i = 0; i < starsCount; i++)
                {
                    starsString += "*";
                }

                Console.WriteLine($"/**** {line} {starsString}/");
            }

            string starsString = "";
            for (int i = 0; i < maxLineLength; i++) {
                starsString += "*";
            }

            Console.WriteLine($"/{starsString}/");
            WriteCenter($"C# LARC API LIBRARY GENERATOR");
            Console.WriteLine($"/{starsString}/");
            Console.WriteLine($"/{starsString}/");
            WriteLeft($"SCHEME VERSION: {_scheme.SchemeVersion}");
            WriteLeft($"API VERSION: {_scheme.ApiVersion}");
            WriteLeft($"SCHEME GENERATED: {_scheme.GeneratedAt}");
            Console.WriteLine($"/{starsString}/");
        }
    }
}