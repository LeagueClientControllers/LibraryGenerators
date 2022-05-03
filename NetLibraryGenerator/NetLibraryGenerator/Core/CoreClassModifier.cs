using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using ICSharpCode.NRefactory.CSharp;

using NetLibraryGenerator.Model;
using NetLibraryGenerator.Utilities;

namespace NetLibraryGenerator.Core
{
    [SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")]
    public static class CoreClassModifier
    {
        public static async Task CorrectCore(string libraryPath, List<LocalCategory> categories)
        {
            if (categories.Count == 0) {
                return;
            }

            Console.WriteLine();
            ConsoleUtils.ShowInfo("----------------------------- Modifying core class ---------------------------------");

            string coreAbstractionPath = Path.Combine(libraryPath, $"{Config.CORE_LIBRARY_ABSTRACTION_TYPE}.cs");
            string coreImplementationPath = Path.Combine(libraryPath, $"{Config.CORE_LIBRARY_IMPLEMENTATION_TYPE}.cs");
            
            ConsoleUtils.ShowInfo("Abstraction:");
            
            SyntaxTree coreAbstraction;
            string? coreAbstractionContent;
            using (StreamReader reader = new StreamReader(new FileStream(coreAbstractionPath, FileMode.Open, FileAccess.Read))) {
                coreAbstractionContent = await reader.ReadToEndAsync();
                coreAbstraction = Generator.CodeParser.Parse(coreAbstractionContent);
                coreAbstractionContent = coreAbstractionPath.ReplaceLineEndings("\r\n");
            }
            
            ConsoleUtils.ShowInfo("Core abstraction is parsed");
            string modifiedCoreAbstraction = ModifyAbstraction(coreAbstractionContent, coreAbstraction, categories);
            await using (StreamWriter writer = new StreamWriter(new FileStream(coreAbstractionPath, FileMode.Create, FileAccess.Write))) {
                writer.NewLine = "\r\n";
                await writer.WriteAsync(modifiedCoreAbstraction);
            }
            
            ConsoleUtils.ShowInfo("|—-Abstraction is updated.");
            
            ConsoleUtils.ShowInfo("Implementation:");
            SyntaxTree coreImplementation;
            string? coreImplementationContent;
            using (StreamReader reader = new StreamReader(new FileStream(coreImplementationPath, FileMode.Open, FileAccess.Read))) {
                coreImplementationContent = await reader.ReadToEndAsync();
                coreImplementation = Generator.CodeParser.Parse(coreImplementationContent);
                coreImplementationContent = coreImplementationContent.ReplaceLineEndings("\r\n");
            }
            
            ConsoleUtils.ShowInfo("Core implementation is parsed");
            string modifiedCoreImplementation = ModifyImplementation(coreImplementationContent, coreImplementation, categories);
            await using (StreamWriter writer = new StreamWriter(new FileStream(coreImplementationPath, FileMode.Create, FileAccess.Write))) {
                writer.NewLine = "\r\n";
                await writer.WriteAsync(modifiedCoreImplementation);
            }
            
            ConsoleUtils.ShowInfo("|—-Implementation is updated.");
        }

        private static string ModifyAbstraction(string content, SyntaxTree abstraction, List<LocalCategory> categories)
        {
            List<string> tabularContent = content.Split("\r\n").ToList();
            Region categoriesRegion = ExtractRegions(abstraction.ExtractType(Config.CORE_LIBRARY_ABSTRACTION_TYPE)).First();
            Console.WriteLine($"[categoriesRegion.StartLineIndex] = {categoriesRegion.StartLineIndex}, [categoriesRegion.EndLineIndex] = {categoriesRegion.EndLineIndex}");
            Console.WriteLine(String.Join("\r\n", tabularContent));
            tabularContent.RemoveRange(new Range(categoriesRegion.StartLineIndex + 1, categoriesRegion.EndLineIndex - 1));
            ConsoleUtils.ShowInfo("|--Categories section is found");

            List<string> newCategoriesSection = new() { "" };
            foreach (LocalCategory category in categories) {
                CodeMemberField categoryProperty = new CodeMemberField();
                categoryProperty.Name = $"{category.InitialCategory.Name.CaseTransform(Case.CamelCase, Case.PascalCase)} {{ get; }}//";
                categoryProperty.Type = new CodeTypeReference($"I{category.Name}");
                categoryProperty.Attributes = MemberAttributes.Public;
                categoryProperty.Comments.Add(category.InitialCategory.Docs.ToCSharpDoc());
                
                newCategoriesSection.Add(Generator.CodeProvider.GenerateCodeFromMember(categoryProperty, new CodeGeneratorOptions())
                    .Replace("//;", "")
                    .Trim()
                    .Split("\r\n")
                    .Select(s => string.IsNullOrEmpty(s) ? "" : $"        {s}")
                    .JoinString("\r\n"));
                newCategoriesSection.Add("");
            }

            tabularContent.InsertRange(categoriesRegion.StartLineIndex + 1, newCategoriesSection);
            ConsoleUtils.ShowInfo("|--Categories section is updated with new categories");

            return string.Join("\r\n", tabularContent);
        }

        private static string ModifyImplementation(string content, SyntaxTree syntax, List<LocalCategory> categories)
        {
            List<string> tabularContent = content.Split("\r\n").ToList();
            List<Region> autoGeneratedRegions =
                ExtractRegions(syntax.ExtractType(Config.CORE_LIBRARY_IMPLEMENTATION_TYPE))
                    .Where(r => r.Name.StartsWith("<auto-generated>"))
                    .ToList();

            Region categoriesImplementationRegion = autoGeneratedRegions[0];
            Region categoriesInitializationRegion = autoGeneratedRegions[1];
            tabularContent.RemoveRange(new Range(categoriesImplementationRegion.StartLineIndex + 1, categoriesImplementationRegion.EndLineIndex - 1));

            List<string> categoriesImplementationSection = new() { "" };
            foreach (LocalCategory category in categories) {
                CodeMemberField categoryProperty = new CodeMemberField();
                categoryProperty.Name = $"{category.InitialCategory.Name.CaseTransform(Case.CamelCase, Case.PascalCase)} {{ get; }}//";
                categoryProperty.Type = new CodeTypeReference($"I{category.Name}");
                categoryProperty.Comments.Add(new CodeCommentStatement("<inheritdoc />", true));

                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                categoryProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                
                categoriesImplementationSection.Add(Generator.CodeProvider.GenerateCodeFromMember(categoryProperty, new CodeGeneratorOptions())
                    .Replace("//;", "")
                    .Trim()
                    .Split("\r\n")
                    .Select(s => string.IsNullOrEmpty(s) ? "" : $"        {s}")
                    .JoinString("\r\n"));
                categoriesImplementationSection.Add("");
            }

            tabularContent.InsertRange(categoriesImplementationRegion.StartLineIndex + 1, categoriesImplementationSection);
            ConsoleUtils.ShowInfo("|--Categories section is updated with new categories");

            int contentOffset = categoriesImplementationSection.Count - 
                (categoriesImplementationRegion.EndLineIndex - 1 - categoriesImplementationRegion.StartLineIndex);

            tabularContent.RemoveRange(
                (categoriesInitializationRegion.StartLineIndex + 1 + contentOffset)
                ..(categoriesInitializationRegion.EndLineIndex - 1 + contentOffset));

            List<string> newInitializationSection = new();
            foreach (LocalCategory category in categories) {
                string categoryName = category.InitialCategory.Name.CaseTransform(Case.CamelCase, Case.PascalCase);
                CodeStatement initialization = new CodeAssignStatement(
                    new CodeArgumentReferenceExpression(categoryName),
                    new CodeObjectCreateExpression(category.Name, new CodeThisReferenceExpression()));

                newInitializationSection.Add(
                    Generator.CodeProvider.GenerateCodeFromStatement(initialization, new CodeGeneratorOptions()).Trim());
            }

            tabularContent.InsertRange(categoriesInitializationRegion.StartLineIndex + 1 + contentOffset, 
                newInitializationSection.Select(s => string.IsNullOrEmpty(s) ? s : $"            {s}"));
            
            ConsoleUtils.ShowInfo("|--Initialization section is updated with new categories");
            return string.Join("\r\n", tabularContent);
        }


        private static List<Region> ExtractRegions(TypeDeclaration type)
        {
            List<Region> regions = new List<Region>();
            foreach (AstNode child in type.Children) {
                SearchForRegionInNode(regions, child);
            }

            return regions;
        }

        private static void SearchForRegionInNode(List<Region> regions, AstNode node)
        {
             if (node is not PreProcessorDirective directive) {
                node.Children.ToList().ForEach(c => SearchForRegionInNode(regions, c));
                return;
             }

             switch (directive.Type) {
                 case PreProcessorDirectiveType.Region: {
                     regions.Add(new Region {
                         StartLineIndex = directive.StartLocation.Line - 1,
                         Name = directive.Argument
                     });
                     break;
                 }

                 case PreProcessorDirectiveType.Endregion: {
                     Region lastIncomplete = regions.Last(r => !r.Complete);
                     lastIncomplete.Complete = true;
                     lastIncomplete.EndLineIndex = directive.StartLocation.Line - 1;
                     break;
                 }

                 default:
                     return;
             }
        }
    }
}