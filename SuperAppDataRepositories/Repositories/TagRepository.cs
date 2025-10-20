using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Extensions;
using SuperAppDataRepositories.Data;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class TagRepository : BaseRepository, ITagRepository
    {
        public TagRepository(ILogger<TagRepository> logger, IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
        }

        public async Task<List<Tag>> GetTags(int userId)
        {
            _logger.LogInformation("Getting tags for userId: {UserId} using stored procedure: {StoredProcedure}", 
                userId, StoredProcedures.spSelectTagsWithHierarchy);

            return await ExecuteStoredProcedureAsync(
                StoredProcedures.spSelectTagsWithHierarchy,
                addParameters: async (command) =>
                {
                    command.Parameters.Add(new SqlParameter("@user_id", userId));
                    await Task.CompletedTask;
                },
                mapResult: MapToTagListAsync, // Use custom mapping for lowercase columns
                useSuperAppConnection: true
            );
        }

        public async Task<Tag?> GetTagById(int tagId)
        {
            try
            {
                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectTagById,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_TagId", tagId));
                        await Task.CompletedTask;
                    },
                    mapResult: MapToTagSingleAsync, // Use custom mapping for Tag objects
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tag with ID: {TagId}", tagId);
                throw;
            }
        }

        public async Task<Tag> CreateTagAsync(Tag tag)
        {
            try
            {
                _logger.LogInformation("Creating new tag with name: {Name} for user: {UserId}", tag.Name, tag.UserId);

                await ExecuteStoredProcedureAsync(
                    StoredProcedures.spInsertTag,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@user_id", tag.UserId));
                        command.Parameters.Add(new SqlParameter("@name", tag.Name));
                        // AddParameterIfNotNull(command, "@parent_id", tag.ParentId); // Property not in model
                        AddParameterIfNotNull(command, "@slug", tag.Slug);
                        AddParameterIfNotNull(command, "@color", tag.Color);
                        AddParameterIfNotNull(command, "@icon", tag.Icon);
                        AddParameterIfNotNull(command, "@description", tag.Description);
                        // command.Parameters.Add(new SqlParameter("@is_public", tag.IsPublic ?? false)); // Property not in model
                        // AddParameterIfNotNull(command, "@public_slug", tag.PublicSlug); // Property not in model
                        await Task.CompletedTask;
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return new List<object>();
                    },
                    useSuperAppConnection: true
                );

                // After successful creation, fetch the created tag by name and user_id
                // Since the stored procedure doesn't return the created tag, we need to fetch it
                var createdTags = await GetTags(tag.UserId);
                var createdTag = createdTags
                    .Where(t => t.Name == tag.Name) // Removed ParentId check as property doesn't exist
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefault();

                if (createdTag == null)
                {
                    throw new InvalidOperationException("Failed to create tag: Unable to retrieve created tag from database");
                }

                _logger.LogInformation("Successfully created tag with ID: {TagId}", createdTag.TagId);
                return createdTag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating tag");
                throw;
            }
        }

        public async Task<Tag> UpdateTagAsync(Tag tag)
        {
            try
            {
                _logger.LogInformation("Updating tag with ID: {TagId}", tag.TagId);

                if (tag.TagId <= 0)
                {
                    throw new ArgumentException("Tag ID must be greater than 0 for updates", nameof(tag));
                }

                var (tags, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spInsertUpdateTag,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        DataTable tagTable = tag.ToDataTable();
                        AddStructuredParameter(command, "@Tag", tagTable);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: MapToTagListAsync, // Use custom mapping for Tag objects
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error updating tag: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to update tag: {errorMessage}");
                }

                var updatedTag = tags.FirstOrDefault();
                if (updatedTag == null)
                {
                    throw new InvalidOperationException("Failed to update tag: No tag returned from database");
                }

                _logger.LogInformation("Successfully updated tag with ID: {TagId}", updatedTag.TagId);
                return updatedTag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating tag with ID: {TagId}", tag.TagId);
                throw;
            }
        }

        public async Task<bool> DeleteTagAsync(int tagId)
        {
            try
            {
                _logger.LogInformation("Deleting tag with ID: {TagId}", tagId);

                if (tagId <= 0)
                {
                    throw new ArgumentException("Tag ID must be greater than 0", nameof(tagId));
                }

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spDeleteTag,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_TagId", tagId));
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return new List<object>();
                    },
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error deleting tag: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to delete tag: {errorMessage}");
                }

                _logger.LogInformation("Successfully deleted tag with ID: {TagId}", tagId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting tag with ID: {TagId}", tagId);
                throw;
            }
        }

        public async Task<List<Tag>> GetTagsByNoteId(int noteId)
        {
            return await ExecuteStoredProcedureAsync(
                StoredProcedures.spSelectNoteTagsByNoteId,
                addParameters: async (command) =>
                {
                    command.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                    await Task.CompletedTask;
                },
                mapResult: MapToTagListAsync, // Use custom mapping for Tag objects
                useSuperAppConnection: true
            );
        }

        public async Task<List<Note>> GetNotesByTagId(int tagId)
        {
            return await ExecuteStoredProcedureAsync(
                StoredProcedures.spSelectNoteTagsByTagId,
                addParameters: async (command) =>
                {
                    command.Parameters.Add(new SqlParameter("@iv_TagId", tagId));
                    await Task.CompletedTask;
                },
                mapResult: MapToListAsync<Note>,
                useSuperAppConnection: true
            );
        }

        public async Task<bool> AddNoteTagAsync(int noteId, int tagId, string createdBy)
        {
            try
            {
                _logger.LogInformation("Adding tag {TagId} to note {NoteId}", tagId, noteId);

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spInsertNoteTag,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                        command.Parameters.Add(new SqlParameter("@iv_TagId", tagId));
                        AddParameterIfNotNull(command, "@iv_CreatedBy", createdBy);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return new List<object>();
                    },
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error adding note-tag association: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to add note-tag association: {errorMessage}");
                }

                _logger.LogInformation("Successfully added tag {TagId} to note {NoteId}", tagId, noteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding tag {TagId} to note {NoteId}", tagId, noteId);
                throw;
            }
        }

        public async Task<bool> RemoveNoteTagAsync(int noteId, int tagId)
        {
            try
            {
                _logger.LogInformation("Removing tag {TagId} from note {NoteId}", tagId, noteId);

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spDeleteNoteTag,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                        command.Parameters.Add(new SqlParameter("@iv_TagId", tagId));
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return new List<object>();
                    },
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error removing note-tag association: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to remove note-tag association: {errorMessage}");
                }

                _logger.LogInformation("Successfully removed tag {TagId} from note {NoteId}", tagId, noteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing tag {TagId} from note {NoteId}", tagId, noteId);
                throw;
            }
        }

        public async Task<bool> RemoveAllNoteTagsAsync(int noteId)
        {
            try
            {
                _logger.LogInformation("Removing all tags from note {NoteId}", noteId);

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spDeleteNoteTagsByNoteId,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return new List<object>();
                    },
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error removing all note tags: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to remove all note tags: {errorMessage}");
                }

                _logger.LogInformation("Successfully removed all tags from note {NoteId}", noteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing all tags from note {NoteId}", noteId);
                throw;
            }
        }

        public async Task<List<TagTree>> GetTagTreeAsync(int userId, bool includeShared = true)
        {
            try
            {
                _logger.LogInformation("Getting tag tree for userId: {UserId}, includeShared: {IncludeShared}", 
                    userId, includeShared);

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectTagTreeWithSharing,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@user_id", userId));
                        command.Parameters.Add(new SqlParameter("@include_shared", includeShared));
                        await Task.CompletedTask;
                    },
                    mapResult: MapToTagTreeListAsync,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tag tree for userId: {UserId}", userId);
                throw;
            }
        }
    }
}