using NetLibraryGenerator.SchemeModel;
using NetLibraryGenerator.Utilities;

namespace NetLibraryGenerator.Model
{
    public class LocalEntityDeclaration
    {
        public int    Id          { get; set; }
        public string Name        { get; set; }
        public string LocalPath   { get; set; }
        public string Namespace   { get; set; }
        public ApiEntityKind Kind { get; set; }

        public LocalEntityDeclaration(ApiEntityDeclaration declaration)
        {
            Id = declaration.Id;
            Name = declaration.Name;
            Kind = declaration.Kind;

            string? parentFolder = Path.GetDirectoryName(declaration.Path);
            if (parentFolder == null) {
                LocalPath = Path.ChangeExtension(declaration.Path, "cs");
                Namespace = $"{Config.PROJECT_NAME}.{Config.MODEL_FOLDER_NAME}";
                return;
            }

            LocalPath = "";
            foreach (string node in parentFolder.Split('\\')) {
                LocalPath = Path.Combine(LocalPath, node.CaseTransform(Case.UnderScore, Case.PascalCase));
            }

            LocalPath = Path.Combine(LocalPath, $"{Path.GetFileNameWithoutExtension(declaration.Path)}.cs");
            Namespace = $"{Config.PROJECT_NAME}.{Config.MODEL_FOLDER_NAME}.{Path.GetDirectoryName(LocalPath)!.Replace('\\', '.')}";
        }
    }
}