namespace SuperAppModels.Helpers
{
    /// <summary>
    /// A "link" is a dbo.files row whose url points outside SuperApp (GitHub, Drive, any web page),
    /// shown in the workspace as a file item (entity_type 4). Task #1477.
    /// </summary>
    public static class LinkRules
    {
        public const string MimeType = "text/x-uri";
        public const int MaxUrlLength = 1000;
        public const int MaxNameLength = 255;

        /// <summary>Folder at the root of a project's workspace that holds the project's links.</summary>
        public const string ProjectFolderName = "Links";

        public const byte EntityTypeFolder = 2;
        public const byte EntityTypeNote = 3;
        public const byte EntityTypeFile = 4;

        public static bool IsLink(string? mimeType) => mimeType == MimeType;

        /// <summary>Only absolute http/https URLs — FE opens them in a new tab (no javascript:, data:...).</summary>
        public static bool IsValidUrl(string? url) =>
            !string.IsNullOrWhiteSpace(url)
            && url.Length <= MaxUrlLength
            && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        /// <summary>Name given by the user, or the URL's host + path when empty.</summary>
        public static string NameOrDefault(string? name, string url)
        {
            if (!string.IsNullOrWhiteSpace(name))
                return Truncate(name.Trim());
            var uri = new Uri(url.Trim());
            var fallback = (uri.Host + uri.AbsolutePath).TrimEnd('/');
            return Truncate(string.IsNullOrEmpty(fallback) ? url.Trim() : fallback);
        }

        private static string Truncate(string value) =>
            value.Length <= MaxNameLength ? value : value[..MaxNameLength];
    }
}
