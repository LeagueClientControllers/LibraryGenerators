using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;

using NetLibraryGenerator.Core;
using NetLibraryGenerator.Model;
using NetLibraryGenerator.SchemeModel;

using System.CodeDom;
using System.CodeDom.Compiler;

namespace NetLibraryGenerator.Utilities
{
    public static class Extensions
    {
        public static int GetLength(this Range range)
        {
            return range.End.Value - range.Start.Value + 1;
        }

        public static void RemoveRange<T>(this List<T> list, Range range) {
            (int offset, int length) = range.GetOffsetAndLength(list.Count);
            list.RemoveRange(offset, length + 1);
        }

        public static CodeCommentStatement ToCSharpDoc(this IEnumerable<JsDocumentationNode> nodes)
        {
            string comment = "<summary>\r\n ";
            foreach (JsDocumentationNode node in nodes) {
                if (node.IsReference) {
                    comment += $"<see cref=\"{node.Text}\"/>";
                } else {
                    comment += node.Text.Replace("\r\n", "\r\n ");
                }
            }

            comment += "\r\n </summary>";
            return new CodeCommentStatement(comment, true);
        }

        public static CodeCommentStatement ToParamDoc(this IEnumerable<JsDocumentationNode> nodes, string paramName)
        {
            bool unfoldedComment = nodes.Any(n => n.Text.Contains("\r\n"));
            string comment = $"<param name=\"{paramName}\">{(unfoldedComment ? "\r\n " : "")}";
            foreach (JsDocumentationNode node in nodes) {
                if (node.IsReference) {
                    comment += $"<see cref=\"{node.Text}\"/>";
                } else {
                    comment += node.Text.Replace("\r\n", "\r\n ");
                }
            }

            comment += $"{(unfoldedComment ? "\r\n " : "")}</param>";
            return new CodeCommentStatement(comment, true);
        }

        public static CodeTypeReference ToTypeReference(this ApiPropertyType type, List<LocalEntityDeclaration> localTypes)
        {
            CodeTypeReference typeReference;

            if (type.ReferenceId != null) {
                LocalEntityDeclaration referencedDeclaration = localTypes[(int)type.ReferenceId - 1];
                typeReference = new CodeTypeReference(referencedDeclaration.Name);
            } else if (type.Primitive != null) { 
                if (type.Primitive == SchemeModel.PrimitiveType.Number) {
                    typeReference = new CodeTypeReference(typeof(int));
                } else if (type.Primitive == SchemeModel.PrimitiveType.Decimal) {
                    typeReference = new CodeTypeReference(typeof(decimal));
                } else if (type.Primitive == SchemeModel.PrimitiveType.String) {
                    typeReference = new CodeTypeReference(typeof(string));
                } else if (type.Primitive == SchemeModel.PrimitiveType.Boolean) {
                    typeReference = new CodeTypeReference(typeof(bool));
                } else if (type.Primitive == SchemeModel.PrimitiveType.Object) {
                    typeReference = new CodeTypeReference(typeof(object));
                } else if (type.Primitive == SchemeModel.PrimitiveType.Date) {
                    typeReference = new CodeTypeReference(typeof(DateTime));
                } else if (type.Primitive == SchemeModel.PrimitiveType.Array) {
                    typeReference = new CodeTypeReference("List");
                } else if (type.Primitive == SchemeModel.PrimitiveType.Dictionary) {
                    typeReference = new CodeTypeReference("Dictionary");
                } else {
                    throw new ArgumentException("Primitive type is defined but is not recognizable.");
                }
            } else {
                throw new ArgumentException("Type is not defined by either primitive or the reference id");
            }

            foreach (ApiPropertyType innerType in type.GenericTypeArguments) {
                typeReference.TypeArguments.Add(innerType.ToTypeReference(localTypes));
            }

            if (type.Nullable) {
                string typeString;
                using (MemoryStream stream = new MemoryStream()) {
                    StreamWriter writer = new StreamWriter(stream);
                    Generator.CodeProvider.GenerateCodeFromExpression(new CodeTypeReferenceExpression(typeReference), writer, new CodeGeneratorOptions());
                    writer.Flush();
                    stream.Position = 0;

                    using (StreamReader reader = new StreamReader(stream)) {
                        typeString = reader.ReadToEnd();
                    }
                }

                typeReference = new CodeTypeReference($"{typeString}?");
            }

            return typeReference;
        }

        public static List<MethodDeclaration> ExtractTypeMethods(this SyntaxTree entity, string typeName)
        {
            NamespaceDeclaration oldAbstractionNamespace = (NamespaceDeclaration)entity.Members.Last();
            TypeDeclaration oldAbstractionInterface = (TypeDeclaration)oldAbstractionNamespace.Members.First(m => m is TypeDeclaration tD && tD.Name == typeName);
            return oldAbstractionInterface.Members.Where(m => m is MethodDeclaration).Cast<MethodDeclaration>().ToList();
        }

        public static List<PropertyDeclaration> ExtractTypeProperties(this SyntaxTree entity, string typeName)
        {
            NamespaceDeclaration oldAbstractionNamespace = (NamespaceDeclaration)entity.Members.Last();
            TypeDeclaration oldAbstractionInterface = (TypeDeclaration)oldAbstractionNamespace.Members.First(m => m is TypeDeclaration tD && tD.Name == typeName);
            return oldAbstractionInterface.Members.Where(m => m is PropertyDeclaration).Cast<PropertyDeclaration>().ToList();
        }

        public static List<ConstructorDeclaration> ExtractTypeConstructors(this SyntaxTree entity, string typeName)
        {
            NamespaceDeclaration oldAbstractionNamespace = (NamespaceDeclaration)entity.Members.Last();
            TypeDeclaration oldAbstractionInterface = (TypeDeclaration)oldAbstractionNamespace.Members.First(m => m is TypeDeclaration tD && tD.Name == typeName);
            return oldAbstractionInterface.Members.Where(m => m is ConstructorDeclaration).Cast<ConstructorDeclaration>().ToList();
        }
    }
}