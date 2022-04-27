using NetLibraryGenerator.Core;
using System.CodeDom;

namespace NetLibraryGenerator.Utilities
{
    public static class NamespaceImportExtensions
    {
        public static void AddImportsForEnum(this CodeNamespace @namespace)
        {
            @namespace.Imports.Add(new CodeNamespaceImport($"Ardalis.SmartEnum"));
        }

        public static void AddImportsForEventHandler(this CodeNamespace @namespace)
        {
            @namespace.Imports.Add(new CodeNamespaceImport(ModelGenerator.MODEL_NAMESPACE));
            @namespace.Imports.Add(new CodeNamespaceImport(EventsGenerator.EVENT_SERVICE_NAMESPACE));
        }

        public static void AddImportsForEventService(this CodeNamespace @namespace)
        {
            @namespace.Imports.Add(new CodeNamespaceImport(ModelGenerator.MODEL_NAMESPACE));
            @namespace.Imports.Add(new CodeNamespaceImport($"{ModelGenerator.MODEL_NAMESPACE}.Local"));
            @namespace.Imports.Add(new CodeNamespaceImport($"{Config.PROJECT_NAME}.{Config.EXCEPTIONS_FOLDER_NAME}"));
            @namespace.Imports.Add(new CodeNamespaceImport($"{Config.PROJECT_NAME}.{Config.EVENT_HANDLERS_FOLDER_NAME}"));
        }

        public static void AddImportsForModelEntity(this CodeNamespace @namespace)
        {
            @namespace.Imports.Add(new CodeNamespaceImport($"System"));
            @namespace.Imports.Add(new CodeNamespaceImport($"System.Collections.Generic"));
            @namespace.Imports.Add(new CodeNamespaceImport($"System.Runtime.InteropServices"));
            @namespace.Imports.Add(new CodeNamespaceImport($"Newtonsoft.Json"));
            @namespace.Imports.Add(new CodeNamespaceImport($"Ardalis.SmartEnum.JsonNet"));
        }

        public static void AddImportsForCategoryAbstraction(this CodeNamespace @namespace)
        {
            @namespace.Imports.Add(new CodeNamespaceImport("System"));
            @namespace.Imports.Add(new CodeNamespaceImport("System.Threading"));
            @namespace.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            @namespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            @namespace.Imports.Add(new CodeNamespaceImport("System.Runtime.InteropServices"));
            @namespace.Imports.Add(new CodeNamespaceImport(ModelGenerator.MODEL_NAMESPACE));
            @namespace.Imports.Add(new CodeNamespaceImport($"{Config.PROJECT_NAME}.{Config.EXCEPTIONS_FOLDER_NAME}"));
            @namespace.Imports.Add(new CodeNamespaceImport("NetLibraryGenerator.Attributes"));
        }

        public static void AddImportsForCategoryImplementation(this CodeNamespace @namespace)
        {
            @namespace.Imports.Add(new CodeNamespaceImport($"System"));
            @namespace.Imports.Add(new CodeNamespaceImport("System.Threading"));
            @namespace.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            @namespace.Imports.Add(new CodeNamespaceImport($"System.Collections.Generic"));
            @namespace.Imports.Add(new CodeNamespaceImport($"System.Runtime.InteropServices"));
            @namespace.Imports.Add(new CodeNamespaceImport(Config.PROJECT_NAME));
            @namespace.Imports.Add(new CodeNamespaceImport(ModelGenerator.MODEL_NAMESPACE));
            @namespace.Imports.Add(new CodeNamespaceImport(CategoriesGenerator.CATEGORIES_ABSTRACTION_NAMESPACE));
            @namespace.Imports.Add(new CodeNamespaceImport($"{Config.PROJECT_NAME}.{Config.EXCEPTIONS_FOLDER_NAME}"));
        }
    }
}