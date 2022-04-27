namespace NetLibraryGenerator.Model
{
    public class LocalModel
    {
        public LocalModelEntity this[int index]
        {
            get {
                return Responses.FirstOrDefault(r => r.Declaration.Id == index) ??
                       Parameters.FirstOrDefault(p => p.Declaration.Id == index) ??
                       throw new KeyNotFoundException($"Entity with id {index} not found in local model.");
            }
        }

        public List<LocalModelEntity> Parameters { get; } = new();
        public List<LocalModelEntity> Responses { get; } = new();
    }
}
