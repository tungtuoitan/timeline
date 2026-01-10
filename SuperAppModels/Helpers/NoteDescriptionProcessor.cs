using System.Text.RegularExpressions;

namespace SuperAppModels.Helpers
{
    /// <summary>
    /// Helper class for processing note descriptions
    /// Contains pure functions for transforming note content (wiki links, references, etc.)
    /// This avoids circular dependency between NoteService and NoteRepository
    /// </summary>
    public static class NoteDescriptionProcessor
    {
        /// <summary>
        /// Process note description to replace temporary/negative IDs with real IDs
        /// Handles wiki-style links: [[-1/path]] -> [[123/path]]
        /// </summary>
        /// <param name="description">Note description containing references</param>
        /// <param name="realNoteId">Real note ID to replace negative IDs with</param>
        /// <returns>Updated description with real IDs</returns>
        public static string ReplaceNegativeNoteIdInWikiLinks(string? description, int realNoteId)
        {
            if (string.IsNullOrEmpty(description))
                return description ?? string.Empty;

            // Replace all negative noteIds in wiki links: [[-1/path]] -> [[123/path]]
            // Pattern explanation:
            // \[\[ - Match literal [[
            // (-\d+) - Capture group 1: negative number (e.g., -1, -2)
            // / - Match literal /
            // ([^\]]+) - Capture group 2: any characters except ] (the path)
            // \]\] - Match literal ]]
            var wikiLinkRegex = new Regex(@"\[\[(-\d+)/([^\]]+)\]\]");
            var result = wikiLinkRegex.Replace(description, $"[[{realNoteId}/$2]]");

            return result;
        }

        /// <summary>
        /// Check if description contains any wiki-style links
        /// </summary>
        /// <param name="description">Note description to check</param>
        /// <returns>True if contains wiki links, false otherwise</returns>
        public static bool ContainsWikiLinks(string? description)
        {
            if (string.IsNullOrEmpty(description))
                return false;

            return description.Contains("[[");
        }

        /// <summary>
        /// Extract all wiki link paths from description
        /// </summary>
        /// <param name="description">Note description</param>
        /// <returns>List of wiki link paths</returns>
        public static List<string> ExtractWikiLinkPaths(string? description)
        {
            var paths = new List<string>();

            if (string.IsNullOrEmpty(description))
                return paths;

            // Match all wiki links: [[path]]
            var wikiLinkRegex = new Regex(@"\[\[([^\]]+)\]\]");
            var matches = wikiLinkRegex.Matches(description);

            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count > 1)
                {
                    paths.Add(match.Groups[1].Value);
                }
            }

            return paths;
        }
    }
}
