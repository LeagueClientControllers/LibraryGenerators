using NetLibraryGenerator.SchemeModel;

using System.CodeDom;

namespace NetLibraryGenerator.Model
{
    public class LocalEntityProperty
    {
        public bool   Nullable            { get; set; } 
        public object? InitialValue        { get; set; }
        public string SchemePropertyName  { get; set; }
        public CodeTypeReference Type     { get; set; }
        public JsDocumentationNode[] Docs { get; set; }

        public LocalEntityProperty(CodeTypeReference reference, ApiEntityProperty property)
        {
            SchemePropertyName = property.Name;
            Nullable = property.Type.Nullable;
            InitialValue = property.InitialValue;
            Docs = property.Docs;
            Type = reference;
        }
    }
}