using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Partial update request for a project.
    /// Only non-null fields are updated; null fields are left unchanged in DB.
    /// </summary>
    public class PatchProjectRequest
    {
        /// <summary>Rows of the Reflect tab (Type / I think / Reality) as a JSON array string</summary>
        [JsonPropertyName("reflectJson")]
        public string? ReflectJson { get; set; }

        /// <summary>
        /// Fields to set to null — null in the body means "unchanged", so clearing needs this.
        /// Allowed: reflectJson.
        /// </summary>
        [JsonPropertyName("clearFields")]
        public List<string>? ClearFields { get; set; }

        public static readonly HashSet<string> ClearableFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "reflectJson"
        };
    }
}
