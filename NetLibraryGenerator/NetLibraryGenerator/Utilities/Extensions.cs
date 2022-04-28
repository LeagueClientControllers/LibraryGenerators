using ICSharpCode.NRefactory.CSharp;

using NetLibraryGenerator.Core;
using NetLibraryGenerator.Model;
using NetLibraryGenerator.SchemeModel;

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;

using PrimitiveType = NetLibraryGenerator.SchemeModel.PrimitiveType;

namespace NetLibraryGenerator.Utilities
{
    public static class Extensions
    {
        public static string JoinString<T>(this IEnumerable<T> enumerable, string separator) =>
            string.Join(separator, enumerable);

        public static void RemoveRange<T>(this List<T> list, Range range) {
            (int offset, int length) = range.GetOffsetAndLength(list.Count);
            list.RemoveRange(offset, length + 1);
        }

        public static string GenerateCodeFromMember(this CodeDomProvider provider, CodeTypeMember member, CodeGeneratorOptions options)
        {
            using (MemoryStream stream = new MemoryStream()) {
                IndentedTextWriter writer = new IndentedTextWriter(new StreamWriter(stream), "    ");
                provider.GenerateCodeFromMember(member, writer, options);
                writer.Flush();
                stream.Position = 0;

                using (StreamReader reader = new StreamReader(stream)) {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string GenerateCodeFromStatement(this CodeDomProvider provider, CodeStatement statement, CodeGeneratorOptions options)
        {
            using (MemoryStream stream = new MemoryStream()) {
                IndentedTextWriter writer = new IndentedTextWriter(new StreamWriter(stream), "    ");
                provider.GenerateCodeFromStatement(statement, writer, options);
                writer.Flush();
                stream.Position = 0;

                using (StreamReader reader = new StreamReader(stream)) {
                    return reader.ReadToEnd();
                }
            }
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
                if (type.Primitive == PrimitiveType.Number) {
                    typeReference = new CodeTypeReference(typeof(int));
                } else if (type.Primitive == PrimitiveType.Decimal) {
                    typeReference = new CodeTypeReference(typeof(decimal));
                } else if (type.Primitive == PrimitiveType.String) {
                    typeReference = new CodeTypeReference(typeof(string));
                } else if (type.Primitive == PrimitiveType.Boolean) {
                    typeReference = new CodeTypeReference(typeof(bool));
                } else if (type.Primitive == PrimitiveType.Object) {
                    typeReference = new CodeTypeReference(typeof(object));
                } else if (type.Primitive == PrimitiveType.Date) {
                    typeReference = new CodeTypeReference(typeof(DateTime));
                } else if (type.Primitive == PrimitiveType.Array) {
                    typeReference = new CodeTypeReference("List");
                } else if (type.Primitive == PrimitiveType.Dictionary) {
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

        public static TypeDeclaration ExtractType(this SyntaxTree entity, string typeName)
        {
            NamespaceDeclaration oldAbstractionNamespace = (NamespaceDeclaration)entity.Members.Last();
            return (TypeDeclaration)oldAbstractionNamespace.Members.First(m => m is TypeDeclaration tD && tD.Name == typeName);
        }
        
        public static List<MethodDeclaration> ExtractTypeMethods(this SyntaxTree entity, string typeName)
        {
            NamespaceDeclaration oldAbstractionNamespace = (NamespaceDeclaration)entity.Members.Last();
            TypeDeclaration oldAbstractionInterface = (TypeDeclaration)oldAbstractionNamespace.Members.First(m => m is TypeDeclaration tD && tD.Name == typeName);
            return oldAbstractionInterface.Members.Where(m => m is MethodDeclaration).Cast<MethodDeclaration>().ToList();
        }

        public static string ExtractMethodSignature(this MethodDeclaration method)
        {
            StringBuilder buffer = new StringBuilder();
            foreach (AstNode child in method.Children) {
                if (child is NewLineNode or Comment or AttributeSection) {
                    continue;
                }

                buffer.Append(child);
            }

            return buffer.ToString();
        }
    }
}