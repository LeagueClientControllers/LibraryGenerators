using NetLibraryGenerator.SchemeModel;

using System.CodeDom;
using System.Net;

namespace NetLibraryGenerator.Model
{
    public class LocalCategory
    {
        public string Name { get; }
        public ApiCategory InitialCategory { get; }
        public CodeCompileUnit Abstraction { get; }
        public CodeCompileUnit Implementation { get; }

        public LocalCategory(string name, CodeCompileUnit abstraction, CodeCompileUnit implementation, ApiCategory initialCategory)
        {
            Name = name;  
            Abstraction = abstraction;
            Implementation = implementation;
            InitialCategory = initialCategory;
        }
    }
}