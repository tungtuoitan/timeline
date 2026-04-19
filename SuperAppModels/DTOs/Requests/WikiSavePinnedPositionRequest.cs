using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class WikiSavePinnedPositionRequest
    {
        public double X { get; set; }
        public double Y { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
