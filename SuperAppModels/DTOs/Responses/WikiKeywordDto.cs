namespace SuperAppModels.DTOs.Responses
{
    public class WikiKeywordDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public List<string> Synonyms { get; set; } = new();
        public List<int> InfoIds { get; set; } = new();
        public int Views { get; set; }
        public int Reads { get; set; }
        public int Edits { get; set; }
        public double? PosX { get; set; }
        public double? PosY { get; set; }
        public bool PinnedPosition { get; set; }
        public string? DeletedAt { get; set; }
    }
}
