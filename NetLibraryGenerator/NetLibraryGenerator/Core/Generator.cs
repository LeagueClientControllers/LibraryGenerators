using ICSharpCode.NRefactory.CSharp;

using NetLibraryGenerator.Model;
using NetLibraryGenerator.SchemeModel;
using NetLibraryGenerator.Utilities;

using Microsoft.CSharp;

using System.CodeDom.Compiler;

namespace NetLibraryGenerator.Core
{
    public static class Generator
    {
        public static readonly CSharpParser CodeParser = new CSharpParser();
        public static readonly CodeDomProvider CodeProvider = new CSharpCodeProvider(); 

        public static async Task GenerateLibrary(string libraryPath, ApiScheme scheme)
        {
            ConsoleUtils.ShowInfo("Transforming declarations to local...");

            List<LocalEntityDeclaration> localDeclarations = new List<LocalEntityDeclaration>();
            foreach (ApiEntityDeclaration declaration in scheme.Model.Declarations) {
                localDeclarations.Add(new LocalEntityDeclaration(declaration));
            }

            ConsoleUtils.ShowInfo("Declarations transformed");

            LocalModel model = await ModelGenerator.GenerateLocalModel(libraryPath, scheme, localDeclarations);
            List<LocalCategory> newCategories = await CategoriesGenerator.GenerateLocalCategories(
                libraryPath, scheme, model);
            
            await CoreClassModifier.CorrectCore(libraryPath, newCategories);
            await EventsGenerator.GenerateEventSystem(libraryPath, localDeclarations);
        }
    }
}