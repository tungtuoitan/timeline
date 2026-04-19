namespace SuperAppModels.DTOs.Responses
{
    public class WikiGetAllResponse
    {
        public List<WikiKeywordDto> Keywords { get; set; } = new();
        public List<WikiInfoDto> Infos { get; set; } = new();
    }
}
