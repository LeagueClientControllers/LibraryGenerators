using NetLibraryGenerator.Model;
using NetLibraryGenerator.Utilities;

using System.CodeDom.Compiler;

using ICSharpCode.NRefactory.CSharp;

namespace NetLibraryGenerator.Core
{
    public static class CategoriesMerger
    {
        public static bool MergeCategory(string outputPath, string libraryPath, LocalCategory category)
        {
            string oldAbstractionPath = Path.Combine(libraryPath, Config.CATEGORIES_FOLDER_NAME, Config.CATEGORIES_ABSTRACTION_FOLDER_NAME, $"I{category.Name}.cs");
            string oldImplementationPath = Path.Combine(libraryPath, Config.CATEGORIES_FOLDER_NAME, $"{category.Name}.cs");
            if (!File.Exists(oldAbstractionPath) || !File.Exists(oldImplementationPath)) {
                return false;
            }

            ConsoleUtils.ShowInfo($"|—-Found old implementation");

            SyntaxTree oldAbstraction;
            string? oldAbstractionContent;
            using (StreamReader reader = new StreamReader(new FileStream(oldAbstractionPath, FileMode.Open, FileAccess.Read))) {
                oldAbstractionContent = reader.ReadToEnd();
                oldAbstraction = Generator.CodeParser.Parse(oldAbstractionContent);
            }

            SyntaxTree oldImplementation;
            string? oldImplementationContent;
            using (StreamReader reader = new StreamReader(new FileStream(oldImplementationPath, FileMode.Open, FileAccess.Read))) {
                oldImplementationContent = reader.ReadToEnd();
                oldImplementation = Generator.CodeParser.Parse(oldImplementationContent);
            }

            ConsoleUtils.ShowInfo($"|—-Old implementation is parsed");
            ConsoleUtils.ShowInfo($"|—-Generating and parsing new implementation");

            SyntaxTree newAbstraction;
            string? newAbstractionContent;
            using (MemoryStream stream = new MemoryStream()) {
                StreamWriter writer = new StreamWriter(stream);
                Generator.CodeProvider.GenerateCodeFromCompileUnit(category.Abstraction, writer, new CodeGeneratorOptions());
                writer.Flush();
                stream.Position = 0;

                using (StreamReader reader = new StreamReader(stream)) {
                    newAbstractionContent = reader.ReadToEnd();
                    newAbstraction = Generator.CodeParser.Parse(newAbstractionContent);
                }
            }

            SyntaxTree newImplementation;
            string? newImplementationContent;
            using (MemoryStream stream = new MemoryStream()) {
                StreamWriter writer = new StreamWriter(stream);
                Generator.CodeProvider.GenerateCodeFromCompileUnit(category.Implementation, writer, new CodeGeneratorOptions());
                writer.Flush();
                stream.Position = 0;

                using (StreamReader reader = new StreamReader(stream)) {
                    newImplementationContent = reader.ReadToEnd();
                    newImplementation = Generator.CodeParser.Parse(newImplementationContent);
                }
            }

            ConsoleUtils.ShowInfo($"|—-New implementation is generated");

            List<MethodDeclaration> oldAbstractionMethods = oldAbstraction.ExtractTypeMethods($"I{category.Name}");
            List<MethodDeclaration> newAbstractionMethods = newAbstraction.ExtractTypeMethods($"I{category.Name}");

            Dictionary<MethodDeclaration, MethodDeclaration> alreadyAbstractedMethods = new();
            foreach (MethodDeclaration newMethod in newAbstractionMethods) {
                MethodDeclaration? foundMethod = null;
                foreach (MethodDeclaration oldMethod in oldAbstractionMethods) { 
                    if (oldMethod.Name == newMethod.Name) {
                        foundMethod = oldMethod;
                        break;
                    }   
                }   

                if (foundMethod != null) {
                    ConsoleUtils.ShowInfo($"|—-Method '{newMethod.Name}' is already implemented");
                    alreadyAbstractedMethods.Add(foundMethod, newMethod);
                }
            }

            ConsoleUtils.ShowInfo($"|—-Found {alreadyAbstractedMethods.Count} already implemented methods out of {newAbstractionMethods.Count}");
            string mergedAbstractionContent = MergeMethods(alreadyAbstractedMethods, oldAbstractionContent, newAbstractionContent);
            ConsoleUtils.ShowInfo($"|—-Abstraction is merged.");

            List<MethodDeclaration> oldImplementationMethods = oldImplementation.ExtractTypeMethods($"{category.Name}");
            List<MethodDeclaration> newImplementationMethods = newImplementation.ExtractTypeMethods($"{category.Name}");

            Dictionary<MethodDeclaration, MethodDeclaration> alreadyImplementedMethod = new();
            foreach ((MethodDeclaration? oldMethod, MethodDeclaration? newMethod) in alreadyAbstractedMethods) {
                MethodDeclaration? oldImplementationMethod = null;
                foreach (MethodDeclaration _oldImplementationMethod in oldImplementationMethods) {
                    if (oldMethod.Name == _oldImplementationMethod.Name) {
                        oldImplementationMethod = _oldImplementationMethod;
                        oldImplementationMethods.Remove(_oldImplementationMethod);
                        break;
                    }
                }

                MethodDeclaration? newImplementationMethod = null;
                foreach (MethodDeclaration _newImplementationMethod in newImplementationMethods) {
                    if (newMethod.Name == _newImplementationMethod.Name) {
                        newImplementationMethod = _newImplementationMethod;
                        newImplementationMethods.Remove(_newImplementationMethod);
                        break;
                    }
                }

                if (oldImplementationMethod == null || newImplementationMethod == null) {
                    throw new GeneratorException($"Implementation of abstraction is not found");
                }

                alreadyImplementedMethod.Add(oldImplementationMethod, newImplementationMethod);
            }

            string mergedImplementationContent = MergeMethods(alreadyImplementedMethod, oldImplementationContent, newImplementationContent);
            ConsoleUtils.ShowInfo($"|—-Implementation is merged.");

            using (StreamWriter writer = new StreamWriter(new FileStream(oldAbstractionPath, FileMode.Create, FileAccess.Write))) {
                writer.WriteLine("#nullable enable");
                writer.Write(mergedAbstractionContent);
                writer.WriteLine("");
                writer.WriteLine("#nullable restore");
            }

            ConsoleUtils.ShowInfo($"|—-Abstraction is updated.");

            using (StreamWriter writer = new StreamWriter(new FileStream(oldImplementationPath, FileMode.Create, FileAccess.Write))) {
                writer.WriteLine("#nullable enable");
                writer.Write(mergedImplementationContent);
                writer.WriteLine("");
                writer.WriteLine("#nullable restore");
            }

            ConsoleUtils.ShowInfo($"|—-Implementation is updated.");

            return true;
        } 

        private static string MergeMethods(Dictionary<MethodDeclaration, MethodDeclaration> methods, string oldContent, string newContent)
        {
            List<string> tabularOldContent = oldContent.Split("\r\n").ToList();
            List<string> tabularNewContent = newContent.Split("\r\n").ToList();

            int linesOffset = 0;
            foreach (var method in methods) {
                MethodDeclaration oldMethod = method.Key;
                MethodDeclaration newMethod = method.Value;

                List<string> oldMethodContent = new();
                bool oldMethodEncountered = false;
                for (int i = oldMethod.StartLocation.Line - 1; i <= oldMethod.EndLocation.Line - 1; ++i) {
                    string row = tabularOldContent[i];
                    if (row.Contains(oldMethod.Name)) {
                        oldMethodEncountered = true;
                    }

                    if (oldMethodEncountered) {
                        oldMethodContent.Add(row);
                    }
                }

                List<int> newMethodLines = new();
                bool newMethodEncountered = false;
                for (int i = newMethod.StartLocation.Line + linesOffset - 1; i <= newMethod.EndLocation.Line + linesOffset - 1; ++i) {
                    string row = tabularNewContent[i];
                    if (row.Contains(newMethod.Name)) {
                        newMethodEncountered = true;
                    }

                    if (newMethodEncountered) {
                        newMethodLines.Add(i);
                    }
                }

                tabularNewContent.RemoveRange(newMethodLines.First(), newMethodLines.Count);
                int j = newMethodLines.First();
                foreach (string line in oldMethodContent) {
                    tabularNewContent.Insert(j, line);
                    j++;
                }

                linesOffset += oldMethodContent.Count - newMethodLines.Count;
                ConsoleUtils.ShowInfo($"|—-Method '{oldMethod.Name}' is merged.");
            }

            return string.Join("\r\n", tabularNewContent);
        }
    }
}