using NetLibraryGenerator.SchemeModel;

using System.CodeDom;
using System.Net;

namespace NetLibraryGenerator.Model
{
    public class LocalCategory
    {
        public string Name { get; set; }
        public CodeCompileUnit Abstraction { get; set; }
        public CodeCompileUnit Implementation { get; set; }

        public LocalCategory(string name, CodeCompileUnit abstraction, CodeCompileUnit implementation)
        {
            Name = name;  
            Abstraction = abstraction;
            Implementation = implementation;
        }
    }
}