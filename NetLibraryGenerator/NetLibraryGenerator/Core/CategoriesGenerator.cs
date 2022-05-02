using NetLibraryGenerator.Model;
using NetLibraryGenerator.SchemeModel;
using NetLibraryGenerator.Utilities;

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;

namespace NetLibraryGenerator.Core
{
    [SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")]
    public static class CategoriesGenerator
    {
        private const string CATEGORIES_NAMESPACE = $"{Config.PROJECT_NAME}.{Config.CATEGORIES_FOLDER_NAME}";
        public const string CATEGORIES_ABSTRACTION_NAMESPACE = $"{Config.PROJECT_NAME}.{Config.CATEGORIES_FOLDER_NAME}.{Config.CATEGORIES_ABSTRACTION_FOLDER_NAME}";

        public static async Task<List<LocalCategory>> GenerateLocalCategories(string libraryPath, ApiScheme scheme, LocalModel model)
        {
            Console.WriteLine();
            ConsoleUtils.ShowInfo("------------------------------- Generating categories -----------------------------------");

            List<LocalCategory> localCategories = BuildCategoriesGraphs(scheme, model);
            ConsoleUtils.ShowInfo("Graphs are built");

            Console.WriteLine();
            ConsoleUtils.ShowInfo("--------------------------------- Merging categories ------------------------------------");
            foreach (LocalCategory localCategory in localCategories) {
                ConsoleUtils.ShowInfo($"{localCategory.Name}:");
                (bool categoryFound, List<ApiMethod> changedMethods) = 
                    CategoriesMerger.MergeCategory(libraryPath, localCategory);
                
                if (!categoryFound) {
                    ConsoleUtils.ShowWarning($"|—-Old implementation of {localCategory.Name} is not found");
                    string abstractionPath = Path.Combine(libraryPath, Config.CATEGORIES_FOLDER_NAME, Config.CATEGORIES_ABSTRACTION_FOLDER_NAME, $"I{localCategory.Name}.cs");
                    string implementationPath = Path.Combine(libraryPath, Config.CATEGORIES_FOLDER_NAME, $"{localCategory.Name}.cs");

                    await using (StreamWriter writer = new StreamWriter(new FileStream(abstractionPath, FileMode.Create, FileAccess.Write))) {
                        writer.NewLine = "\r\n";
                        Generator.CodeProvider.GenerateCodeFromCompileUnit(localCategory.Abstraction, writer, new CodeGeneratorOptions());
                    }
                    ConsoleUtils.ShowInfo($"|—-New abstraction is written");

                    await using (StreamWriter writer = new StreamWriter(new FileStream(implementationPath, FileMode.Create, FileAccess.Write))) {
                        writer.NewLine = "\r\n";
                        Generator.CodeProvider.GenerateCodeFromCompileUnit(localCategory.Implementation, writer, new CodeGeneratorOptions());
                    }
                    ConsoleUtils.ShowInfo($"|—-New implementation is written");
                } else {
                    localCategory.ChangedMethods.AddRange(changedMethods.Select(m => m.Name));
                }
            }

            return localCategories;
        }

        private static List<LocalCategory> BuildCategoriesGraphs(ApiScheme scheme, LocalModel model)
        {
            List<LocalCategory> localCategories = new();

            ApiEntityDeclaration? apiResponse = scheme.Model.Declarations.FirstOrDefault(d => d.Name == Config.RESPONSE_BASE_CLASS_NAME);
            if (apiResponse == null) {
                throw new GeneratorException($"Class named {Config.RESPONSE_BASE_CLASS_NAME} not found in API model.");
            }

            for (int i = 0; i < scheme.Categories.Length; i++) {
                ApiCategory category = scheme.Categories[i];
                string categoryName = $"{category.Name.CaseTransform(Case.CamelCase, Case.PascalCase)}{Config.CATEGORY_IDENTIFIER}";

                ConsoleUtils.ShowInfo($"{categoryName}:");
                CodeCompileUnit abstraction = BuildAbstractionGraph(category, categoryName, model, apiResponse.Id);
                ConsoleUtils.ShowInfo($"|--Abstraction graph is built");

                CodeCompileUnit implementation = BuildImplementationGraph(abstraction, category, categoryName, model, apiResponse.Id);
                ConsoleUtils.ShowInfo($"|--Implementation graph is built");

                LocalCategory localCategory = new LocalCategory(categoryName, abstraction, implementation, category);
                localCategories.Add(localCategory);
            }

            return localCategories;
        }

        private static CodeCompileUnit BuildAbstractionGraph(ApiCategory category, string categoryName, LocalModel model, int apiResponseId)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();

            CodeNamespace importNamespace = new CodeNamespace();
            importNamespace.AddImportsForCategoryAbstraction();
            compileUnit.Namespaces.Add(importNamespace);

            CodeNamespace entityNamespace = new CodeNamespace(CATEGORIES_ABSTRACTION_NAMESPACE);
            compileUnit.Namespaces.Add(entityNamespace);

            CodeTypeDeclaration categoryInterface = new CodeTypeDeclaration($"I{categoryName}");
            categoryInterface.IsInterface = true;
            categoryInterface.Comments.Add(category.Docs.ToCSharpDoc());
            entityNamespace.Types.Add(categoryInterface);

            foreach (ApiMethod method in category.Methods) {
                CodeMemberMethod categoryMethod = new CodeMemberMethod();
                categoryMethod.Comments.Add(method.Docs.ToCSharpDoc());
                categoryMethod.Name = $"{method.Name.CaseTransform(Case.CamelCase, Case.PascalCase)}Async";
                categoryInterface.Members.Add(categoryMethod);

                if (method.RequireAccessToken) {
                    if (method.AccessibleFrom == MethodAccessPolicy.Controller) {
                        categoryMethod.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("ControllerOnly")));
                    } else if (method.AccessibleFrom == MethodAccessPolicy.Device) {
                        categoryMethod.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("DeviceOnly")));
                    }
                }

                if (method.ResponseId == apiResponseId) {
                    categoryMethod.ReturnType = new CodeTypeReference("Task");
                } else {
                    CodeTypeReference taskReference = new CodeTypeReference("Task");
                    LocalModelEntity response = model[method.ResponseId];

                    taskReference.TypeArguments.Add(response.Properties.Count > 1
                        ? new CodeTypeReference(response.Declaration.Name)
                        : response.Properties[0].Type);

                    categoryMethod.ReturnType = taskReference;
                }

                CodeParameterDeclarationExpression cancellationToken = new CodeParameterDeclarationExpression("CancellationToken", "token = default");
                categoryMethod.Parameters.Add(cancellationToken);

                if (method.ParametersId == null) {
                    continue;
                }

                int i = 0;
                LocalModelEntity parameters = model[(int)method.ParametersId];
                foreach (LocalEntityProperty property in parameters.Properties) {
                    CodeParameterDeclarationExpression parameterExpression = new CodeParameterDeclarationExpression(property.Type, property.SchemePropertyName);
                    categoryMethod.Comments.Add(property.Docs.ToParamDoc(property.SchemePropertyName));

                    if (property.InitialValue != null) {
                        parameterExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("Optional")));
                        parameterExpression.CustomAttributes.Add(ModelGenerator.BuildDefaultValueAttributeDeclaration(property.InitialValue));
                    }

                    categoryMethod.Parameters.Insert(i, parameterExpression);
                    i++;
                }
            }

            return compileUnit;
        }

        private static CodeCompileUnit BuildImplementationGraph(CodeCompileUnit abstraction, ApiCategory category, string categoryName, LocalModel model, int apiResponseId)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();

            CodeNamespace importNamespace = new CodeNamespace();
            importNamespace.AddImportsForCategoryImplementation();
            compileUnit.Namespaces.Add(importNamespace);

            CodeNamespace entityNamespace = new CodeNamespace(CATEGORIES_NAMESPACE);
            compileUnit.Namespaces.Add(entityNamespace);

            CodeTypeDeclaration categoryClass = new CodeTypeDeclaration(categoryName);
            categoryClass.Comments.Add(new CodeCommentStatement("<inheritdoc />", true));
            categoryClass.BaseTypes.Add(new CodeTypeReference($"I{categoryName}"));
            entityNamespace.Types.Add(categoryClass);

            CodeTypeReference coreClassReference = new CodeTypeReference(Config.CORE_LIBRARY_ABSTRACTION_TYPE);
            CodeMemberField apiField = new CodeMemberField(coreClassReference, "_api");
            apiField.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            categoryClass.Members.Add(apiField);

            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(coreClassReference, "api"));
            constructor.Statements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression("_api"), new CodeArgumentReferenceExpression("api")));
            categoryClass.Members.Add(constructor);


            List<CodeMemberMethod> methods = new List<CodeMemberMethod>();
            foreach (CodeTypeMember member in abstraction.Namespaces[1].Types[0].Members) {
                if (member is CodeMemberMethod method) {
                    methods.Add(method);
                }
            }

            for (int i = 0; i < methods.Count; i++) {
                ApiMethod method = category.Methods[i];
                CodeMemberMethod abstractionMethod = methods[i];
                CodeMemberMethod categoryMethod = new CodeMemberMethod();
                categoryMethod.Name = abstractionMethod.Name;
                categoryMethod.Parameters.AddRange(abstractionMethod.Parameters);
                categoryMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                categoryMethod.Comments.Add(new CodeCommentStatement("<inheritdoc />", true));

                CodeMethodInvokeExpression executeMethodInvocation = new CodeMethodInvokeExpression();
                executeMethodInvocation.Method = new CodeMethodReferenceExpression(new CodeSnippetExpression("await _api"), "ExecuteAsync");
                executeMethodInvocation.Parameters.Add(new CodePrimitiveExpression($"/{category.Name}/{method.Name}"));

                using (MemoryStream stream = new MemoryStream()) {
                    StreamWriter writer = new StreamWriter(stream);
                    Generator.CodeProvider.GenerateCodeFromExpression(new CodeTypeReferenceExpression(abstractionMethod.ReturnType), writer, new CodeGeneratorOptions());
                    writer.Flush();
                    stream.Position = 0;

                    using (StreamReader reader = new StreamReader(stream)) {
                        categoryMethod.ReturnType = new CodeTypeReference($"async {reader.ReadToEnd()}");
                    }
                }

                if (method.ParametersId != null) {
                    LocalModelEntity parameters = model[(int)method.ParametersId];
                    CodeTypeReference parametersType = new CodeTypeReference(parameters.Declaration.Name);
                    CodeVariableDeclarationStatement parametersCreationStatement = new CodeVariableDeclarationStatement(parametersType, "parameters");
                    executeMethodInvocation.Method.TypeArguments.Add(parametersType);
                    
                    CodeObjectCreateExpression parameterCreateExpression = new CodeObjectCreateExpression(parametersType);
                    parametersCreationStatement.InitExpression = parameterCreateExpression;
                    
                    for (int b = 0; b < categoryMethod.Parameters.Count - 1; b++) {
                        parameterCreateExpression.Parameters.Add(new CodeArgumentReferenceExpression(categoryMethod.Parameters[b].Name));
                    }

                    executeMethodInvocation.Parameters.Add(new CodeVariableReferenceExpression("parameters"));

                    categoryMethod.Statements.Add(new CodeCommentStatement("<auto-generated-safe-area> Code within tag borders shouldn't cause incorrect behavior and will be preserved.", false));
                    categoryMethod.Statements.Add(new CodeCommentStatement("TODO: Add parameters validation", false));
                    categoryMethod.Statements.Add(new CodeCommentStatement("</auto-generated-safe-area>", false));
                    categoryMethod.Statements.Add(parametersCreationStatement);
                }

                if (method.ResponseId != apiResponseId) {
                    LocalModelEntity responseEntity = model[method.ResponseId];
                    CodeTypeReference parametersType = new CodeTypeReference($"{responseEntity.Declaration.Name}");
                    executeMethodInvocation.Method.TypeArguments.Insert(0, parametersType);

                    CodeVariableDeclarationStatement responseDeclaration = new CodeVariableDeclarationStatement(parametersType, "response");
                    responseDeclaration.InitExpression = executeMethodInvocation;
                    categoryMethod.Statements.Add(responseDeclaration);

                    categoryMethod.Statements.Add(new CodeCommentStatement("<auto-generated-safe-area> Code within tag borders shouldn't cause incorrect behavior and will be preserved.", false));
                    categoryMethod.Statements.Add(new CodeCommentStatement("TODO: Add response validation", false));
                    categoryMethod.Statements.Add(new CodeCommentStatement("</auto-generated-safe-area>", false));

                    CodeVariableReferenceExpression responseReference = new CodeVariableReferenceExpression("response");
                    if (categoryMethod.ReturnType.BaseType == parametersType.BaseType) {
                        categoryMethod.Statements.Add(new CodeMethodReturnStatement(responseReference));
                    } else {
                        CodeFieldReferenceExpression propertyReference = new CodeFieldReferenceExpression(
                            responseReference, responseEntity.Properties[0].SchemePropertyName.CaseTransform(Case.CamelCase, Case.PascalCase));

                        categoryMethod.Statements.Add(new CodeMethodReturnStatement(propertyReference));
                    }
                } else {
                    categoryMethod.Statements.Add(new CodeExpressionStatement(executeMethodInvocation));
                }

                executeMethodInvocation.Parameters.Add(new CodePrimitiveExpression(method.RequireAccessToken));
                executeMethodInvocation.Parameters.Add(new CodeArgumentReferenceExpression("token"));
                categoryClass.Members.Add(categoryMethod);
            }

            return compileUnit;
        }   
    }
}