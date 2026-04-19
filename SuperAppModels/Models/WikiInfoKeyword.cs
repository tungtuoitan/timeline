namespace SuperAppModels.Models
{
    /// <summary>
    /// Junction table linking wiki infos to keywords — wiki.info_keyword
    /// </summary>
    public class WikiInfoKeyword
    {
        public int InfoId { get; set; }
        public int KeywordId { get; set; }

        // Navigation
        public WikiInfo Info { get; set; } = null!;
        public WikiKeyword Keyword { get; set; } = null!;
    }
}
