using NetLibraryGenerator.Model;
using NetLibraryGenerator.Utilities;

using System.CodeDom.Compiler;

using ICSharpCode.NRefactory.CSharp;

using NetLibraryGenerator.SchemeModel;

namespace NetLibraryGenerator.Core
{
    public static class CategoriesMerger
    {
        public static (bool, List<ApiMethod>) MergeCategory(string libraryPath, LocalCategory category)
        {
            string oldAbstractionPath = Path.Combine(libraryPath, Config.CATEGORIES_FOLDER_NAME, Config.CATEGORIES_ABSTRACTION_FOLDER_NAME, $"I{category.Name}.cs");
            string oldImplementationPath = Path.Combine(libraryPath, Config.CATEGORIES_FOLDER_NAME, $"{category.Name}.cs");
            if (!File.Exists(oldAbstractionPath) || !File.Exists(oldImplementationPath)) {
                return (false, new());
            }

            ConsoleUtils.ShowInfo($"|—-Found old implementation");

            SyntaxTree oldAbstraction;
            using (StreamReader reader = new StreamReader(new FileStream(oldAbstractionPath, FileMode.Open, FileAccess.Read))) {
                string oldAbstractionContent = reader.ReadToEnd();
                oldAbstraction = Generator.CodeParser.Parse(oldAbstractionContent);
            }

            SyntaxTree oldImplementation;
            using (StreamReader reader = new StreamReader(new FileStream(oldImplementationPath, FileMode.Open, FileAccess.Read))) {
                string oldImplementationContent = reader.ReadToEnd();
                oldImplementation = Generator.CodeParser.Parse(oldImplementationContent);
            }

            ConsoleUtils.ShowInfo($"|—-Old implementation is parsed");
            ConsoleUtils.ShowInfo($"|—-Generating and parsing new implementation");

            SyntaxTree newAbstraction;
            string? newAbstractionContent;
            using (MemoryStream stream = new MemoryStream()) {
                StreamWriter writer = new StreamWriter(stream);
                writer.NewLine = "\r\n";
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
                writer.NewLine = "\r\n";
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
            List<ApiMethod> changedMethods = new List<ApiMethod>();
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

                    string newMethodSignature = newMethod.ExtractMethodSignature();
                    string oldMethodSignature = foundMethod.ExtractMethodSignature();
                    if (newMethodSignature != oldMethodSignature) {
                        ConsoleUtils.ShowWarning($"|—-Method '{newMethod.Name}' in {category.Name} has been changed. Revision requested."); 
                        changedMethods.Add(category.InitialCategory.Methods
                            .First(m => m.Name == newMethod.Name.Replace("Async", "")
                                .CaseTransform(Case.PascalCase, Case.CamelCase)));
                    }
                    
                    alreadyAbstractedMethods.Add(foundMethod, newMethod);
                }
            }

            ConsoleUtils.ShowInfo($"|—-Found {alreadyAbstractedMethods.Count} already implemented methods out of {newAbstractionMethods.Count}");

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

            string mergedImplementationContent = MergeMethods(alreadyImplementedMethod, changedMethods, newImplementationContent);
            ConsoleUtils.ShowInfo($"|—-Implementation is merged.");

            using (StreamWriter writer = new StreamWriter(new FileStream(oldAbstractionPath, FileMode.Create, FileAccess.Write))) {
                writer.NewLine = "\r\n";
                writer.WriteLine("#nullable enable");
                writer.Write(newAbstractionContent);
                writer.WriteLine("");
                writer.WriteLine("#nullable restore");
            }

            ConsoleUtils.ShowInfo($"|—-Abstraction is updated.");

            using (StreamWriter writer = new StreamWriter(new FileStream(oldImplementationPath, FileMode.Create, FileAccess.Write))) {
                writer.NewLine = "\r\n";
                writer.WriteLine("#nullable enable");
                writer.Write(mergedImplementationContent);
                writer.WriteLine("");
                writer.WriteLine("#nullable restore");
            }

            ConsoleUtils.ShowInfo($"|—-Implementation is updated.");

            return (true, changedMethods);
        } 

        private static string MergeMethods(Dictionary<MethodDeclaration, MethodDeclaration> methods, List<ApiMethod> changedMethods, string newContent)
        {
            List<string> tabularNewContent = newContent.Split("\r\n").ToList();

            int linesOffset = 0;
            foreach ((MethodDeclaration? oldMethod, MethodDeclaration? newMethod) in methods) {
                List<string> oldMethodBody = new();
                oldMethodBody.AddRange(oldMethod.Body.ToString().ReplaceLineEndings("\r\n").Split("\r\n")
                    .Select(s => string.IsNullOrWhiteSpace(s) ? "" : $"        {s}").Take(..^1));
                
                Console.WriteLine(oldMethod.Body.ToString().Count(c => c == '\r'));
                Console.WriteLine(oldMethod.Body.ToString().Count(c => c == '\n'));
                Console.WriteLine(oldMethod.Body.ToString());

                tabularNewContent[newMethod.Body.StartLocation.Line - 1 + linesOffset] = 
                    tabularNewContent[newMethod.Body.StartLocation.Line - 1 + linesOffset][..(newMethod.Body.StartLocation.Column - 1)];
                tabularNewContent.RemoveRange(
                    (newMethod.Body.StartLocation.Line + linesOffset)..(newMethod.Body.EndLocation.Line - 1 + linesOffset));
                tabularNewContent.InsertRange(newMethod.Body.StartLocation.Line + linesOffset, oldMethodBody);

                if (changedMethods.Any(m => 
                        $"{m.Name.CaseTransform(Case.CamelCase, Case.PascalCase)}Async" == newMethod.Name) && 
                    !tabularNewContent[newMethod.StartLocation.Line + linesOffset].Contains(
                        "TODO: Needs revision due to a signature changes.")) {
                    
                    tabularNewContent.Insert(newMethod.StartLocation.Line + linesOffset,
                        "        /// TODO: Needs revision due to a signature changes.");
                    linesOffset++;
                }
                
                linesOffset += oldMethodBody.Count - (newMethod.Body.EndLocation.Line
                    - newMethod.Body.StartLocation.Line + (newMethod.Body.StartLocation.Column == 10 ? 1 : 0));
                ConsoleUtils.ShowInfo($"|—-Method '{oldMethod.Name}' is merged.");
            }

            return string.Join("\r\n", tabularNewContent);
        }
    }
}