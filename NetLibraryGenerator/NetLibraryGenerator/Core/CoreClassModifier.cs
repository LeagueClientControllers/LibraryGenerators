using NetLibraryGenerator.Model;
using NetLibraryGenerator.Utilities;

using System.CodeDom;
using ICSharpCode.NRefactory.CSharp;

namespace NetLibraryGenerator.Core
{
    public static class CoreClassModifier
    {
        public static void CorrectCore(string libraryPath, List<LocalCategory> categories)
        {
            if (categories.Count == 0) {
                return;
            }

            Console.WriteLine();
            ConsoleUtils.ShowInfo("----------------------------- Modifying core class ---------------------------------");

            string coreAbstractionPath = Path.Combine(libraryPath, $"{Config.CORE_LIBRARY_ABSTRACTION_TYPE}.cs");
            string coreImplementationPath = Path.Combine(libraryPath, $"{Config.CORE_LIBRARY_IMPLEMENTATION_TYPE}.cs");
            
            SyntaxTree coreAbstraction = null!;
            string? coreAbstractionContent = null!;
            using (StreamReader reader = new StreamReader(new FileStream(coreAbstractionPath, FileMode.Open, FileAccess.Read))) {
                coreAbstractionContent = reader.ReadToEnd();
                coreAbstraction = Generator.CodeParser.Parse(coreAbstractionContent);
            }

            SyntaxTree coreImplementation = null!;
            string? coreImplementationContent = null!;
            using (StreamReader reader = new StreamReader(new FileStream(coreImplementationPath, FileMode.Open, FileAccess.Read))) {
                coreImplementationContent = reader.ReadToEnd();
                coreImplementation = Generator.CodeParser.Parse(coreImplementationContent);
            }

            ConsoleUtils.ShowInfo("Core abstraction and implementation are parsed");

            ConsoleUtils.ShowInfo("Abstraction:");
            string modifiedCoreAbstraction = ModifyAbstraction(coreAbstractionContent, coreAbstraction, categories);
            
            using (StreamWriter writer = new StreamWriter(new FileStream(coreAbstractionPath, FileMode.Create, FileAccess.Write))) {
                writer.Write(modifiedCoreAbstraction);
            }
            ConsoleUtils.ShowInfo($"|—-Abstraction is updated.");
            
            ConsoleUtils.ShowInfo("Implementation:");
            string modifiedCoreImplementation = ModifyImplementation(coreImplementationContent, coreImplementation, categories);

            using (StreamWriter writer = new StreamWriter(new FileStream(coreImplementationPath, FileMode.Create, FileAccess.Write))) {
                writer.Write(modifiedCoreImplementation);
            }
            ConsoleUtils.ShowInfo($"|—-Implementation is updated.");
        }

        public static string ModifyAbstraction(string content, SyntaxTree syntax, List<LocalCategory> categories)
        {
            List<string> tabularContent = content.Split("\r\n").ToList();
            Range sectionRange = ExtractCategoriesSectionRange(syntax, Config.CORE_LIBRARY_ABSTRACTION_TYPE);
            ConsoleUtils.ShowInfo("|--Categories section is found");
            tabularContent.RemoveRange(sectionRange);

            List<string> newCategoriesSection = new();
            foreach (LocalCategory category in categories) {
                foreach (CodeCommentStatement comment in category.Abstraction.Namespaces[1].Types[0].Comments) {
                    string[] commentLines = comment.Comment.Text.Split("\r\n").Select(s => $"///{s}").ToArray();
                    commentLines[0] = commentLines[0].Replace("///", "/// ");
                    newCategoriesSection.AddRange(commentLines);
                }

                newCategoriesSection.Add($"I{category.Name} {category.Name.Replace(Config.CATEGORY_IDENTIFIER, "")} {{ get; }}");
                newCategoriesSection.Add("");
            }

            tabularContent.InsertRange(sectionRange.Start.Value, newCategoriesSection.Select(s => String.IsNullOrEmpty(s) ? s : $"        {s}"));
            ConsoleUtils.ShowInfo("|--Categories section is updated with new categories");

            return String.Join("\r\n", tabularContent);
        }

        private static string ModifyImplementation(string content, SyntaxTree syntax, List<LocalCategory> categories)
        {
            List<string> tabularContent = content.Split("\r\n").ToList();
            Range sectionRange = ExtractCategoriesSectionRange(syntax, Config.CORE_LIBRARY_IMPLEMENTATION_TYPE);
            ConsoleUtils.ShowInfo("|--Categories section is found");
            tabularContent.RemoveRange(sectionRange);

            List<string> newCategoriesSection = new();
            foreach (LocalCategory category in categories) {
                newCategoriesSection.Add($"/// <inheritdoc />");
                newCategoriesSection.Add($"public I{category.Name} {category.Name.Replace(Config.CATEGORY_IDENTIFIER, "")} {{ get; }}");
                newCategoriesSection.Add("");
            }

            tabularContent.InsertRange(sectionRange.Start.Value, newCategoriesSection.Select(s => String.IsNullOrEmpty(s) ? s : $"        {s}"));
            ConsoleUtils.ShowInfo("|--Categories section is updated with new categories");

            int contentOffset = newCategoriesSection.Count - sectionRange.GetLength();
            ConstructorDeclaration constructor = syntax.ExtractTypeConstructors(Config.CORE_LIBRARY_IMPLEMENTATION_TYPE).First();

            int categoriesInitializationSectionEndIndex = 0;
            int categoriesInitializationSectionStartIndex = constructor.Body.StartLocation.Line + contentOffset;
            for (int i = categoriesInitializationSectionStartIndex; i < constructor.Body.EndLocation.Line + contentOffset - 1; ++i) {
                if (!tabularContent[i].Contains(Config.CATEGORY_IDENTIFIER)) {
                    categoriesInitializationSectionEndIndex = i - 1;
                    break;
                }
            }

            ConsoleUtils.ShowInfo("|--Initialization section is found");
            tabularContent.RemoveRange(categoriesInitializationSectionStartIndex..categoriesInitializationSectionEndIndex);

            List<string> newInitializationSection = new();
            foreach (LocalCategory category in categories) {
                newInitializationSection.Add($"{category.Name.Replace(Config.CATEGORY_IDENTIFIER, "")} = new {category.Name}(this);");
            }

            tabularContent.InsertRange(categoriesInitializationSectionStartIndex, newInitializationSection.Select(s => String.IsNullOrEmpty(s) ? s : $"            {s}"));
            ConsoleUtils.ShowInfo("|--Initialization section is updated with new categories");
            return string.Join("\r\n", tabularContent);
        }

        private static Range ExtractCategoriesSectionRange(SyntaxTree syntax, string className)
        {
            int categoriesSectionEndLine = 0;
            int categoriesSectionStartLine = 0;
            bool categoriesSectionStarted = false;
            List<PropertyDeclaration> properties = syntax.ExtractTypeProperties(className);
            for (int i = 0; i < properties.Count; i++) {
                PropertyDeclaration property = properties[i];
                if (property.ReturnType.ToString().Contains(Config.CATEGORY_IDENTIFIER) && !categoriesSectionStarted) {
                    categoriesSectionStarted = true;
                    categoriesSectionStartLine = property.StartLocation.Line;
                }

                if (!categoriesSectionStarted) {
                    continue;
                }
                
                if (i < properties.Count - 1) {
                    if (!properties[i + 1].ReturnType.ToString().Contains(Config.CATEGORY_IDENTIFIER)) {
                        categoriesSectionEndLine = property.EndLocation.Line;
                        break;
                    }
                } else {
                    categoriesSectionEndLine = property.EndLocation.Line;
                    break;
                }
            }

            return new Range(categoriesSectionStartLine - 1, categoriesSectionEndLine);
        }
    }
}