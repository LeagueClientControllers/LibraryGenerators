using System.Globalization;
using System.Text.RegularExpressions;

using CommandLine;

using NetLibraryGenerator.Core;
using NetLibraryGenerator.Model;
using NetLibraryGenerator.SchemeModel;
using NetLibraryGenerator.Utilities;

using Newtonsoft.Json;

namespace NetLibraryGenerator
{
    public static class Program
    {
        private const string SUPPORTED_SCHEME_VERSION = "1.0.0";

        private static ApiScheme? _scheme;

        private static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleUtils.DEFAULT_COLOR;

            try {
                await Parser.Default.ParseArguments<CommandLineOptions>(args).MapResult(Run,
                    _ => Task.FromResult(-1));
            } catch (GeneratorException e) {
                ConsoleUtils.ShowError($"Error while generating library: {e.Message}");
            } catch (Exception e) {
                ConsoleUtils.ShowError($"Fatal exception occured. {e.GetType()}: {e.Message}{e.StackTrace?.Split("\r\n").FirstOrDefault()}");
            }
        }

        private static async Task Run(CommandLineOptions options)
        {
            options.ApiSchemePath = new Regex("(\'|\")")
                .Replace(options.ApiSchemePath, "");
            options.LibraryPath = new Regex("(\'|\")")
                .Replace(options.LibraryPath, "");

            if (!File.Exists(options.ApiSchemePath)) {
                throw new ArgumentException(
                    "API scheme path should be valid path to an existing file.");
            }
            
            if (!Directory.Exists(options.LibraryPath)) {
                throw new ArgumentException(
                    "Library path should be valid path to an existing directory.");
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

            GenerationResults results = await Generator.GenerateLibrary(options.LibraryPath, _scheme);
            Console.WriteLine();
            Console.WriteLine($"Generation results: {JsonConvert.SerializeObject(results)}");
            
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