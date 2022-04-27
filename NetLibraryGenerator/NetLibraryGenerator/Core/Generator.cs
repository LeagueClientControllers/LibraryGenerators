using ICSharpCode.NRefactory.CSharp;

using NetLibraryGenerator.Model;
using NetLibraryGenerator.SchemeModel;
using NetLibraryGenerator.Utilities;

using Microsoft.CSharp;

using System.CodeDom;
using System.CodeDom.Compiler;

namespace NetLibraryGenerator.Core
{
    public static class Generator
    {
        public static readonly CSharpParser CodeParser = new CSharpParser();
        public static readonly CodeDomProvider CodeProvider = new CSharpCodeProvider(); 

        public static void GenerateLibrary(ApiScheme scheme)
        {
            ConsoleUtils.ShowInfo("Transforming declarations to local...");

            List<LocalEntityDeclaration> localDeclarations = new();
            foreach (ApiEntityDeclaration declaration in scheme.Model.Declarations) {
                localDeclarations.Add(new LocalEntityDeclaration(declaration));
            }

            ConsoleUtils.ShowInfo("Declarations transformed");

            LocalModel model = ModelGenerator.GenerateLocalModel(@"D:\Development\GitHub\LeagueClientControllers\LccApiNet\LccApiNet", scheme, localDeclarations);
            List<LocalCategory> newCategories = CategoriesGenerator.GenerateLocalCategories(Path.Combine(Environment.CurrentDirectory, "output"), scheme, model);
            CoreClassModifier.CorrectCore(@"D:\Development\GitHub\LeagueClientControllers\LccApiNet\LccApiNet", newCategories);
            EventsGenerator.GenerateEventSystem(@"D:\Development\GitHub\LeagueClientControllers\LccApiNet\LccApiNet", localDeclarations);
            ;
        }
    }
}