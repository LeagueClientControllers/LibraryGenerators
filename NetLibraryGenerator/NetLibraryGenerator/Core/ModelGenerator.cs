using NetLibraryGenerator.Model;
using NetLibraryGenerator.SchemeModel;
using NetLibraryGenerator.Utilities;

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using ICSharpCode.NRefactory.CSharp;

namespace NetLibraryGenerator.Core
{
    [SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")]
    public static class ModelGenerator
    {
        public const string MODEL_NAMESPACE = $"{Config.PROJECT_NAME}.{Config.MODEL_FOLDER_NAME}";

        public static async Task<LocalModel> GenerateLocalModel(string libraryPath, ApiScheme scheme, List<LocalEntityDeclaration> modelDeclarations)
        {
            Console.WriteLine();
            ConsoleUtils.ShowInfo("------------------------------- Generating model -----------------------------------");

            Dictionary<string, LocalModelEntity> graphs = BuildModelGraphs(scheme, modelDeclarations);
            ConsoleUtils.ShowInfo("Code graphs are built");

            DirectoryInfo modelDirectory = new DirectoryInfo(Path.Combine(libraryPath, Config.MODEL_FOLDER_NAME));
            foreach (DirectoryInfo directory in modelDirectory.EnumerateDirectories()) {
                if (directory.Name != Config.SAFE_MODEL_FOLDER_NAME) {
                    Directory.Delete(directory.FullName, true);
                }
            }

            foreach (FileInfo file in modelDirectory.EnumerateFiles()) {
                File.Delete(file.FullName);
            }
            ConsoleUtils.ShowInfo("Old model is cleared");

            foreach (KeyValuePair<string, LocalModelEntity> graph in graphs) {
                string outputPath = Path.Combine(libraryPath, graph.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                await using (StreamWriter writer = new StreamWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write))) {
                    writer.NewLine = "\r\n";
                    
                    await writer.WriteLineAsync("#nullable enable");
                    Generator.CodeProvider.GenerateCodeFromCompileUnit(graph.Value.Implementation, writer, new CodeGeneratorOptions());
                    await writer.WriteLineAsync("");
                    await writer.WriteLineAsync("#nullable restore");
                }
            }
            ConsoleUtils.ShowInfo("New code is generated and inserted into library");

            LocalModel model = new LocalModel();
            foreach (LocalEntityDeclaration declaration in modelDeclarations) {
                foreach (LocalModelEntity entity in graphs.Values) {
                    if (entity.Declaration == declaration) {
                        if (declaration.Kind == ApiEntityKind.Parameters) {
                            model.Parameters.Add(entity);
                        } else if (declaration.Kind == ApiEntityKind.Response) {
                            model.Responses.Add(entity);
                        }

                        break;
                    }
                }   
            }
            ConsoleUtils.ShowInfo("Local model info is generated");
            

            return model;
        }

        private static Dictionary<string, LocalModelEntity> BuildModelGraphs(ApiScheme scheme, List<LocalEntityDeclaration> modelDeclarations)
        {
            Dictionary<string, LocalModelEntity> graphs = new();

            LocalEntityDeclaration? apiResponse = modelDeclarations.FirstOrDefault(d => d.Name == Config.RESPONSE_BASE_CLASS_NAME);
            if (apiResponse == null) {
                throw new GeneratorException($"Class named {Config.RESPONSE_BASE_CLASS_NAME} not found in API model.");
            }

            ConsoleUtils.ShowInfo($"Models:");
            foreach (ApiEntity entity in scheme.Model.Entities) {
                LocalEntityDeclaration declaration = modelDeclarations[entity.Id - 1];
                string filePath = Path.Combine(Config.MODEL_FOLDER_NAME, declaration.LocalPath);

                graphs.Add(filePath, BuildEntityGraph(entity, declaration, modelDeclarations));
                ConsoleUtils.ShowInfo($"|--Entity graph for {declaration.Name} is built");
            }

            ConsoleUtils.ShowInfo($"Enums:");
            foreach (ApiEnum entity in scheme.Model.Enums) {
                LocalEntityDeclaration declaration = modelDeclarations[entity.Id - 1];
               
                graphs.Add(Path.Combine(Config.MODEL_FOLDER_NAME, declaration.LocalPath), BuildEnumGraph(entity, declaration));
                ConsoleUtils.ShowInfo($"|--Enum graph for {declaration.Name} is built");
            }

            return graphs;
        } 


        private static LocalModelEntity BuildEntityGraph(ApiEntity entity, LocalEntityDeclaration declaration, List<LocalEntityDeclaration> allDeclarations)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();

            CodeNamespace importNamespace = new CodeNamespace();
            importNamespace.AddImportsForModelEntity();
            compileUnit.Namespaces.Add(importNamespace);

            CodeNamespace entityNamespace = new CodeNamespace(MODEL_NAMESPACE);
            compileUnit.Namespaces.Add(entityNamespace);

            CodeTypeDeclaration entityClass = new CodeTypeDeclaration(declaration.Name);
            entityClass.Comments.Add(entity.Docs.ToCSharpDoc());
            entityNamespace.Types.Add(entityClass);

            if (entity.Modifiable) {
                entityClass.BaseTypes.Add(new CodeTypeReference("BindableBase"));
            }
            
            if (declaration.Kind == ApiEntityKind.Response) {
                entityClass.BaseTypes.Add(new CodeTypeReference(Config.RESPONSE_BASE_CLASS_NAME));
            }

            CodeConstructor? entityConstructor = null;
            if (declaration.Kind == ApiEntityKind.Parameters) {
                entityConstructor = new CodeConstructor();
                entityConstructor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            }

            List<LocalEntityProperty> localProperties = new List<LocalEntityProperty>();
            foreach (ApiEntityProperty property in entity.Properties.OrderBy(p => p.InitialValue == null)) {
                string propertyName = property.Name.CaseTransform(Case.CamelCase, Case.PascalCase);
                CodeTypeReference propertyType = property.Type.ToTypeReference(allDeclarations);
                
                if (property.Modifiable) {
                    CodeMemberField @private = new CodeMemberField(propertyType, $"_{property.Name}") {
                        Attributes = MemberAttributes.Private | MemberAttributes.Final
                    };

                    CodeMemberProperty modifiableEntityProperty = new CodeMemberProperty() {
                        Name = propertyName,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final,
                        HasGet = true,
                        HasSet = true,
                        Type = propertyType
                    };

                    modifiableEntityProperty.GetStatements.Add(
                        new CodeMethodReturnStatement(new CodeArgumentReferenceExpression($"_{property.Name}")));

                    CodeMethodInvokeExpression setProperty = new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "SetProperty"),
                        new CodeSnippetExpression($"ref _{property.Name}"),
                        new CodeArgumentReferenceExpression("value"));

                    modifiableEntityProperty.SetStatements.Add(setProperty);
                    modifiableEntityProperty.Comments.Add(property.Docs.ToCSharpDoc());
                    modifiableEntityProperty.CustomAttributes.Add(BuildJsonPropertyAttributeDeclaration(property.JsonName));

                    if (property.Type.ReferenceId != null) {
                        LocalEntityDeclaration referenceType = allDeclarations[(int)property.Type.ReferenceId - 1];
                        if (referenceType.Kind == ApiEntityKind.Enum) {
                            modifiableEntityProperty.CustomAttributes.Add(BuildJsonEnumConverterAttributeDeclaration(referenceType.Name));
                        }
                    }

                    if (property.InitialValue is not null) {
                        @private.InitExpression = new CodePrimitiveExpression(property.InitialValue);
                    }

                    entityClass.Members.Add(@private);
                    entityClass.Members.Add(modifiableEntityProperty);
                } else {
                    CodeMemberField entityProperty = new CodeMemberField {
                        Attributes = MemberAttributes.Public | MemberAttributes.Final,
                        Name = propertyName,
                    };

                    entityProperty.Type = property.Type.ToTypeReference(allDeclarations);
                    entityProperty.Comments.Add(property.Docs.ToCSharpDoc());
                    entityProperty.CustomAttributes.Add(BuildJsonPropertyAttributeDeclaration(property.JsonName));

                    if (property.Type.ReferenceId != null) {
                        LocalEntityDeclaration referenceType = allDeclarations[(int)property.Type.ReferenceId - 1];
                        if (referenceType.Kind == ApiEntityKind.Enum) {
                            entityProperty.CustomAttributes.Add(BuildJsonEnumConverterAttributeDeclaration(referenceType.Name));
                        }
                    }

                    string namePostfix = " { get; set; }//";                
                    if (property.InitialValue == null) {
                        if (!property.Type.Nullable) {
                            namePostfix = " { get; set; } = default!";
                        }
                    } else {
                        entityProperty.InitExpression = new CodePrimitiveExpression(property.InitialValue);
                    }

                    entityProperty.Name += namePostfix;
                    entityClass.Members.Add(entityProperty);
                }
                localProperties.Add(new LocalEntityProperty(propertyType, property));

                if (entityConstructor == null) {
                    continue;
                }
                
                if (property.InitialValue == null) {
                    entityConstructor.Parameters.Add(new CodeParameterDeclarationExpression(propertyType, property.Name));
                } else {
                    CodeParameterDeclarationExpression parameterDeclaration = new CodeParameterDeclarationExpression(propertyType, property.Name.CaseTransform(Case.PascalCase, Case.CamelCase));
                    parameterDeclaration.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("Optional")));
                    if (property.InitialValue != null) {
                        parameterDeclaration.CustomAttributes.Add(BuildDefaultValueAttributeDeclaration(property.InitialValue));
                    }

                    entityConstructor.Parameters.Add(parameterDeclaration);
                }

                CodeAssignStatement statement = new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), propertyName), new CodeArgumentReferenceExpression(property.Name));
                entityConstructor.Statements.Add(statement);
            }

            if (entityConstructor != null) { 
                entityClass.Members.Add(entityConstructor);
            }

            LocalModelEntity localEntity = new LocalModelEntity(declaration, compileUnit, localProperties);
            return localEntity;
        }

        private static LocalModelEntity BuildEnumGraph(ApiEnum entity, LocalEntityDeclaration declaration)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();

            CodeNamespace importNamespace = new CodeNamespace();
            importNamespace.AddImportsForEnum();
            compileUnit.Namespaces.Add(importNamespace);

            CodeNamespace entityNamespace = new CodeNamespace(MODEL_NAMESPACE);
            compileUnit.Namespaces.Add(entityNamespace);

            CodeTypeDeclaration entityEnum = new CodeTypeDeclaration(declaration.Name);
            entityEnum.Comments.Add(entity.Docs.ToCSharpDoc());
            entityNamespace.Types.Add(entityEnum);

            CodeTypeReference smartEnum = new CodeTypeReference("SmartEnum");
            smartEnum.TypeArguments.Add(new CodeTypeReference(declaration.Name));
            entityEnum.BaseTypes.Add(smartEnum);

            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "value"));
            constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("name"));
            constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("value"));

            entityEnum.Members.Add(constructor);

            for (int i = 0; i < entity.Members.Length; i++) {
                ApiEnumMember member = entity.Members[i];
                CodeMemberField entityMember = new CodeMemberField {
                    Name = member.Name,
                    Attributes = MemberAttributes.Public | MemberAttributes.Static,
                    Type = new CodeTypeReference(declaration.Name)
                };

                entityMember.Comments.Add(member.Docs.ToCSharpDoc());
                entityMember.InitExpression = new CodeObjectCreateExpression(declaration.Name, new CodePrimitiveExpression(member.Value), new CodePrimitiveExpression(i + 1));
                entityEnum.Members.Add(entityMember);
            }

            LocalModelEntity localEntity = new LocalModelEntity(declaration, compileUnit, new List<LocalEntityProperty>());
            return localEntity;
        }

        private static CodeAttributeDeclaration BuildJsonPropertyAttributeDeclaration(string jsonKey)
        {
            CodeTypeReference propertyAttributeReference = new CodeTypeReference("JsonProperty");
            CodeAttributeArgument jsonKeyArgument = new CodeAttributeArgument(new CodePrimitiveExpression(jsonKey));
            return new CodeAttributeDeclaration(propertyAttributeReference, jsonKeyArgument);
        }

        private static CodeAttributeDeclaration BuildJsonEnumConverterAttributeDeclaration(string enumType)
        {
            CodeTypeReference propertyAttributeReference = new CodeTypeReference("JsonConverter");
            CodeTypeReference smartEnumConverterType = new CodeTypeReference("SmartEnumNameConverter");
            smartEnumConverterType.TypeArguments.Add(new CodeTypeReference(enumType));
            smartEnumConverterType.TypeArguments.Add(new CodeTypeReference(typeof(int)));

            CodeAttributeArgument jsonKeyArgument = new CodeAttributeArgument(new CodeTypeOfExpression(smartEnumConverterType));
            return new CodeAttributeDeclaration(propertyAttributeReference, jsonKeyArgument);
        }

        public static CodeAttributeDeclaration BuildDefaultValueAttributeDeclaration(object defaultValue)
        {
            CodeTypeReference propertyAttributeReference = new CodeTypeReference("DefaultParameterValue");
            CodeAttributeArgument jsonKeyArgument = new CodeAttributeArgument(new CodePrimitiveExpression(defaultValue));
            return new CodeAttributeDeclaration(propertyAttributeReference, jsonKeyArgument);
        }
    }
}