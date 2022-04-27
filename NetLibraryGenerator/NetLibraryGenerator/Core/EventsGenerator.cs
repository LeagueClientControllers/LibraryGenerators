using NetLibraryGenerator.Model;
using NetLibraryGenerator.SchemeModel;
using NetLibraryGenerator.Utilities;

using System.CodeDom;
using System.CodeDom.Compiler;

namespace NetLibraryGenerator.Core
{
    public static class EventsGenerator
    {
        public const string EVENT_SERVICE_NAMESPACE = $"{Config.PROJECT_NAME}.{Config.SERVICES_FOLDER_NAME}";

        public static void GenerateEventSystem(string libraryPath, List<LocalEntityDeclaration> modelDeclarations)
        {
            Console.WriteLine();
            ConsoleUtils.ShowInfo("---------------------------- Generating event system -------------------------------");
            ConsoleUtils.ShowInfo("Handlers:");

            List<LocalEntityDeclaration> eventDeclarations = new();
            Dictionary<string, CodeCompileUnit> handlerGraphs = new();
            foreach (LocalEntityDeclaration declaration in modelDeclarations) {
                if (declaration.Kind == ApiEntityKind.Event) {
                    eventDeclarations.Add(declaration);

                    string handlerName = $"{declaration.Name}{Config.EVENT_HANDLER_IDENTIFIER}";
                    handlerGraphs.Add(Path.Combine(libraryPath, Config.EVENT_HANDLERS_FOLDER_NAME, $"{handlerName}.cs"), GenerateHandlerGraph(handlerName, declaration));
                    ConsoleUtils.ShowInfo($"|--Code graph for {handlerName} is generated");
                }
            }

            foreach (KeyValuePair<string, CodeCompileUnit> graph in handlerGraphs) {
                using (StreamWriter writer = new StreamWriter(new FileStream(graph.Key, FileMode.Create, FileAccess.Write))) {
                    writer.WriteLine("#nullable enable");
                    Generator.CodeProvider.GenerateCodeFromCompileUnit(graph.Value, writer, new CodeGeneratorOptions());
                    writer.WriteLine("");
                    writer.WriteLine("#nullable restore");
                }
            }

            ConsoleUtils.ShowInfo($"|--Code is generated and written");
            ConsoleUtils.ShowInfo($"Service:");

            CodeCompileUnit serviceGraph = GenerateEventService(eventDeclarations);
            ConsoleUtils.ShowInfo($"|--Graph is generated");

            using (StreamWriter writer = new StreamWriter(new FileStream(Path.Combine(libraryPath, Config.SERVICES_FOLDER_NAME, $"{Config.EVENT_SERVICE_NAME}.g.cs"), FileMode.Create, FileAccess.Write))) {
                writer.WriteLine("#nullable enable");
                Generator.CodeProvider.GenerateCodeFromCompileUnit(serviceGraph, writer, new CodeGeneratorOptions());
                writer.WriteLine("");
                writer.WriteLine("#nullable restore");
            }

            ConsoleUtils.ShowInfo($"|--Code is generated and written");
        }

        public static CodeCompileUnit GenerateHandlerGraph(string handlerName, LocalEntityDeclaration @event)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();

            CodeNamespace importNamespace = new CodeNamespace();
            importNamespace.AddImportsForEventHandler();
            compileUnit.Namespaces.Add(importNamespace);

            CodeNamespace handlerNamespace = new CodeNamespace($"{Config.PROJECT_NAME}.{Config.EVENT_HANDLERS_FOLDER_NAME}");
            compileUnit.Namespaces.Add(handlerNamespace);

            CodeTypeDelegate handlerDelegate = new CodeTypeDelegate(handlerName);
            handlerDelegate.ReturnType = new CodeTypeReference(typeof(void));
            handlerDelegate.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            handlerDelegate.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "sender"));
            handlerDelegate.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(@event.Name), "args"));
            
            handlerDelegate.Comments.Add(new CodeCommentStatement("<summary>", true));
            handlerDelegate.Comments.Add(new CodeCommentStatement($"Represents a function that will handle <see cref=\"{Config.EVENT_SERVICE_NAME}.On{@event.Name}\"/> event.", true));
            handlerDelegate.Comments.Add(new CodeCommentStatement("</summary>", true));

            handlerNamespace.Types.Add(handlerDelegate);
            return compileUnit;
        }

        public static CodeCompileUnit GenerateEventService(List<LocalEntityDeclaration> events)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();

            CodeNamespace importNamespace = new CodeNamespace();
            importNamespace.AddImportsForEventService();
            compileUnit.Namespaces.Add(importNamespace);

            CodeNamespace serviceNamespace = new CodeNamespace(EVENT_SERVICE_NAMESPACE);
            compileUnit.Namespaces.Add(serviceNamespace);

            CodeTypeDeclaration serviceClass = new CodeTypeDeclaration(Config.EVENT_SERVICE_NAME);
            serviceClass.IsPartial = true;
            serviceClass.Attributes = MemberAttributes.Public;
            serviceNamespace.Types.Add(serviceClass);

            foreach (LocalEntityDeclaration @event in events) {
                CodeMemberEvent serviceEvent = new CodeMemberEvent();
                serviceEvent.Name = $"On{@event.Name}";
                serviceEvent.Type = new CodeTypeReference($"{@event.Name}{Config.EVENT_HANDLER_IDENTIFIER}?");
                serviceEvent.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                serviceEvent.Comments.Add(new CodeCommentStatement("<summary>", true));
                serviceEvent.Comments.Add(new CodeCommentStatement($"Fires when <see cref=\"{@event.Name}\"/> is occurred.", true));
                serviceEvent.Comments.Add(new CodeCommentStatement("</summary>", true));

                serviceClass.Members.Add(serviceEvent);
            }

            ConsoleUtils.ShowInfo($"|--Events are generated");

            CodeMemberMethod handlerMethod = new CodeMemberMethod();
            handlerMethod.Name = "HandleEventMessage";
            handlerMethod.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            handlerMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("EventMessage"), "message"));

            foreach (LocalEntityDeclaration @event in events) {
                CodeConditionStatement conditionStatement = new CodeConditionStatement();

                CodeFieldReferenceExpression eventTypeReference = new CodeFieldReferenceExpression(new CodeArgumentReferenceExpression("message"), "Type");
                CodeFieldReferenceExpression eventEnumMemberReference = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("EventType"), $"{@event.Name.Replace("Event", "")}");
                conditionStatement.Condition = new CodeBinaryOperatorExpression(eventTypeReference, CodeBinaryOperatorType.ValueEquality, eventEnumMemberReference);

                CodeVariableDeclarationStatement eventDeclaration = new CodeVariableDeclarationStatement(new CodeTypeReference($"{@event.Name}?"), "event");
                CodeFieldReferenceExpression eventFieldReference = new CodeFieldReferenceExpression(new CodeArgumentReferenceExpression("message"), "Event");
                CodeMethodInvokeExpression convertMethod = new CodeMethodInvokeExpression(eventFieldReference, "ToObject");
                convertMethod.Method.TypeArguments.Add(new CodeTypeReference(@event.Name));
                eventDeclaration.InitExpression = convertMethod;
                conditionStatement.TrueStatements.Add(eventDeclaration);

                CodeConditionStatement eventNullCheck = new CodeConditionStatement();
                eventNullCheck.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("event"), CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(null));
                eventNullCheck.TrueStatements.Add(new CodeThrowExceptionStatement(new CodeObjectCreateExpression("EventProviderException", new CodePrimitiveExpression("Event object is missing in event message."))));
                conditionStatement.TrueStatements.Add(eventNullCheck);
                conditionStatement.TrueStatements.Add(new CodeSnippetStatement(""));

                CodeMethodInvokeExpression invokeMethod = new CodeMethodInvokeExpression(new CodeSnippetExpression($"this.On{@event.Name}?"), "Invoke");
                invokeMethod.Parameters.Add(new CodeThisReferenceExpression());
                invokeMethod.Parameters.Add(new CodeVariableReferenceExpression("event"));
                conditionStatement.TrueStatements.Add(new CodeExpressionStatement(invokeMethod));

                conditionStatement.TrueStatements.Add(new CodeMethodReturnStatement());
                handlerMethod.Statements.Add(conditionStatement);
                handlerMethod.Statements.Add(new CodeSnippetStatement(""));
            }

            handlerMethod.Statements.Add(new CodeThrowExceptionStatement(new CodeObjectCreateExpression("EventProviderException", new CodePrimitiveExpression("Incoming message contains unrecognizable event type."))));
            serviceClass.Members.Add(handlerMethod);
            return compileUnit;
        }
    }
}