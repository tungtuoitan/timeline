namespace SuperAppModels.DTOs.Responses
{
    public class KeywordSyncReportDto
    {
        public int TotalKeywords { get; set; }
        public Dictionary<string, int> CountByType { get; set; } = new();
        public int HardDeletedCount { get; set; }
        public int NameMismatchCount { get; set; }
        public int LinkMismatchCount { get; set; }
        public int UpdatedCount { get; set; }
        public int CreatedCount { get; set; }
        public List<KeywordSyncItemDto> Updates { get; set; } = new();
        public List<KeywordSyncItemDto> Created { get; set; } = new();
    }

    public class KeywordSyncItemDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string OldName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
        public string OldLink { get; set; } = string.Empty;
        public string NewLink { get; set; } = string.Empty;
        public bool NameChanged { get; set; }
        public bool LinkChanged { get; set; }
        public string? Description { get; set; }
    }
}
