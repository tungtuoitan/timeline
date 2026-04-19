namespace SuperAppModels.Models
{
    /// <summary>
    /// Synonym for a wiki keyword — wiki.keyword_synonym table
    /// </summary>
    public class WikiKeywordSynonym
    {
        public int Id { get; set; }
        public int KeywordId { get; set; }
        public string Synonym { get; set; } = string.Empty;

        // Navigation
        public WikiKeyword Keyword { get; set; } = null!;
    }
}
