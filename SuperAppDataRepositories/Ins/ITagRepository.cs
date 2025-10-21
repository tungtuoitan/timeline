using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface ITagRepository
    {
        Task<List<Tag>> GetTags(int userId);
        Task<Tag?> GetTagById(int tagId);
        Task<Tag> CreateTagAsync(Tag tag);
        Task<Tag> UpdateTagAsync(Tag tag);
        Task<bool> DeleteTagAsync(int tagId);

        // Tag Tree and Hierarchy methods
        Task<List<TagTree>> GetTagTreeAsync(int workspaceId, int userId);
        Task<List<TagTree>> GetWorkspaceTagTreeAsync(int workspaceId, int userId);

        // Note-Tag relationship methods
        Task<List<Tag>> GetTagsByNoteId(int noteId);
        Task<List<Note>> GetNotesByTagId(int tagId);
        Task<bool> AddNoteTagAsync(int noteId, int tagId, string createdBy);
        Task<bool> RemoveNoteTagAsync(int noteId, int tagId);
        Task<bool> RemoveAllNoteTagsAsync(int noteId);
    }
}