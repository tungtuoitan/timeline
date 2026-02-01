namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Context information for file uploads
    /// Specifies where the file belongs (project or workspace)
    /// </summary>
    public class FileUploadContext
    {
        /// <summary>
        /// Context type: "project" or "workspace"
        /// </summary>
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// Context ID: project_id or workspace_id
        /// </summary>
        public int ContextId { get; set; }

        /// <summary>
        /// Validates the context
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Context))
                return false;

            if (Context != "project" && Context != "workspace")
                return false;

            if (ContextId <= 0)
                return false;

            return true;
        }
    }
}
