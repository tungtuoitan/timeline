namespace SuperAppModels.DTOs.Responses
{
    public class WikiInfoDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<int> KeywordIds { get; set; } = new();
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
        public string? DeletedAt { get; set; }
    }
}
