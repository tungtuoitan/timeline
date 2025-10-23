using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Data;
using SuperAppModels.Models;
using FileInfo = SuperAppModels.Models.FileInfo;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository implementation for file/document operations
    /// Handles CRUD operations, workspace integration, and storage management
    /// </summary>
    public class FileRepository : BaseRepository, IFileRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FileRepository> _logger;

        public FileRepository(
            ApplicationDbContext context,
            ILogger<FileRepository> logger,
            IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a file by ID with user ownership validation
        /// </summary>
        public async Task<FileInfo?> GetFileByIdAsync(int fileId, int userId)
        {
            try
            {
                _logger.LogDebug("Retrieving file {FileId} for user {UserId}", fileId, userId);

                var file = await _context.Files
                    .AsNoTracking()
                    .Where(f => f.FileId == fileId 
                             && f.UserId == userId 
                             && f.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (file == null)
                {
                    _logger.LogWarning("File {FileId} not found or not owned by user {UserId}", fileId, userId);
                }

                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file {FileId} for user {UserId}", fileId, userId);
                throw;
            }
        }

        /// <summary>
        /// Gets all files for a specific user
        /// </summary>
        public async Task<List<FileInfo>> GetFilesByUserAsync(int userId, bool includeArchived = false, string? searchText = null)
        {
            try
            {
                _logger.LogDebug("Retrieving files for user {UserId}, includeArchived={IncludeArchived}, search={SearchText}", 
                    userId, includeArchived, searchText);

                var query = _context.Files
                    .AsNoTracking()
                    .Where(f => f.UserId == userId && f.DeletedAt == null);

                // Filter archived files
                if (!includeArchived)
                {
                    query = query.Where(f => !f.IsArchived);
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchLower = searchText.ToLower();
                    query = query.Where(f => 
                        f.Name.ToLower().Contains(searchLower) ||
                        (f.OriginalFilename != null && f.OriginalFilename.ToLower().Contains(searchLower)) ||
                        (f.Description != null && f.Description.ToLower().Contains(searchLower)));
                }

                // Order by creation date (newest first)
                query = query.OrderByDescending(f => f.CreatedAt);

                var files = await query.ToListAsync();

                _logger.LogInformation("Retrieved {Count} files for user {UserId}", files.Count, userId);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets all files in a workspace (via workspace_items table)
        /// </summary>
        public async Task<List<FileInfo>> GetFilesByWorkspaceAsync(int workspaceId, int userId)
        {
            try
            {
                _logger.LogDebug("Retrieving files for workspace {WorkspaceId} and user {UserId}", workspaceId, userId);

                // Join with workspace_items to get files in workspace
                var files = await _context.Files
                    .AsNoTracking()
                    .Where(f => f.DeletedAt == null)
                    .Where(f => _context.WorkspaceItems
                        .Any(wi => wi.WorkspaceId == workspaceId
                                && wi.ChildType == "file"
                                && wi.ChildId == f.FileId
                                && wi.DeletedAt == null))
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} files for workspace {WorkspaceId}", files.Count, workspaceId);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Creates a new file record
        /// </summary>
        public async Task<FileInfo> CreateFileAsync(FileInfo file, int? parentTagId = null, int? workspaceId = null)
        {
            try
            {
                _logger.LogDebug("Creating file {FileName} for user {UserId}", file.Name, file.UserId);

                // Set creation timestamp
                file.CreatedAt = DateTime.UtcNow;
                file.UpdatedAt = DateTime.UtcNow;

                // Add to context
                _context.Files.Add(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created file {FileId} with name {FileName}", file.FileId, file.Name);

                // If workspace specified, add to workspace_items
                if (workspaceId.HasValue)
                {
                    var workspaceItem = new WorkspaceItem
                    {
                        WorkspaceId = workspaceId.Value,
                        ParentTagId = parentTagId,
                        ChildType = "file",
                        ChildId = file.FileId,
                        SortOrder = 0,
                        Depth = parentTagId.HasValue ? 1 : 0,
                        AddedBy = file.UserId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.WorkspaceItems.Add(workspaceItem);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Added file {FileId} to workspace {WorkspaceId}", file.FileId, workspaceId.Value);
                }

                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating file {FileName} for user {UserId}", file.Name, file.UserId);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing file's metadata
        /// </summary>
        public async Task<FileInfo> UpdateFileAsync(FileInfo file, int userId)
        {
            try
            {
                _logger.LogDebug("Updating file {FileId} for user {UserId}", file.FileId, userId);

                // Verify ownership
                var existingFile = await _context.Files
                    .Where(f => f.FileId == file.FileId 
                             && f.UserId == userId 
                             && f.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (existingFile == null)
                {
                    _logger.LogWarning("File {FileId} not found or not owned by user {UserId}", file.FileId, userId);
                    throw new UnauthorizedAccessException($"File {file.FileId} not found or access denied");
                }

                // Update allowed fields
                existingFile.Name = file.Name;
                existingFile.Description = file.Description;
                existingFile.IsArchived = file.IsArchived;
                existingFile.IsPinned = file.IsPinned;
                existingFile.IsFavorite = file.IsFavorite;
                existingFile.UpdatedAt = DateTime.UtcNow;

                _context.Files.Update(existingFile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated file {FileId}", file.FileId);
                return existingFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file {FileId} for user {UserId}", file.FileId, userId);
                throw;
            }
        }

        /// <summary>
        /// Soft deletes a file (sets DeletedAt timestamp)
        /// </summary>
        public async Task<bool> DeleteFileAsync(int fileId, int userId)
        {
            try
            {
                _logger.LogDebug("Soft deleting file {FileId} for user {UserId}", fileId, userId);

                var file = await _context.Files
                    .Where(f => f.FileId == fileId 
                             && f.UserId == userId 
                             && f.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (file == null)
                {
                    _logger.LogWarning("File {FileId} not found or not owned by user {UserId}", fileId, userId);
                    return false;
                }

                // Soft delete
                file.DeletedAt = DateTime.UtcNow;
                _context.Files.Update(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted file {FileId}", fileId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting file {FileId} for user {UserId}", fileId, userId);
                throw;
            }
        }

        /// <summary>
        /// Permanently deletes a file record
        /// </summary>
        public async Task<bool> PermanentDeleteFileAsync(int fileId, int userId)
        {
            try
            {
                _logger.LogDebug("Permanently deleting file {FileId} for user {UserId}", fileId, userId);

                var file = await _context.Files
                    .Where(f => f.FileId == fileId && f.UserId == userId)
                    .FirstOrDefaultAsync();

                if (file == null)
                {
                    _logger.LogWarning("File {FileId} not found or not owned by user {UserId}", fileId, userId);
                    return false;
                }

                // Remove from workspace_items first (if any)
                var workspaceItems = await _context.WorkspaceItems
                    .Where(wi => wi.ChildType == "file" && wi.ChildId == fileId)
                    .ToListAsync();

                if (workspaceItems.Any())
                {
                    _context.WorkspaceItems.RemoveRange(workspaceItems);
                }

                // Remove file record
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Permanently deleted file {FileId} - physical file cleanup required", fileId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting file {FileId} for user {UserId}", fileId, userId);
                throw;
            }
        }

        /// <summary>
        /// Archives a file
        /// </summary>
        public async Task<bool> ArchiveFileAsync(int fileId, int userId)
        {
            try
            {
                _logger.LogDebug("Archiving file {FileId} for user {UserId}", fileId, userId);

                var file = await _context.Files
                    .Where(f => f.FileId == fileId 
                             && f.UserId == userId 
                             && f.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (file == null)
                {
                    return false;
                }

                file.IsArchived = true;
                file.UpdatedAt = DateTime.UtcNow;

                _context.Files.Update(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Archived file {FileId}", fileId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving file {FileId}", fileId);
                throw;
            }
        }

        /// <summary>
        /// Unarchives a file
        /// </summary>
        public async Task<bool> UnarchiveFileAsync(int fileId, int userId)
        {
            try
            {
                _logger.LogDebug("Unarchiving file {FileId} for user {UserId}", fileId, userId);

                var file = await _context.Files
                    .Where(f => f.FileId == fileId 
                             && f.UserId == userId 
                             && f.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (file == null)
                {
                    return false;
                }

                file.IsArchived = false;
                file.UpdatedAt = DateTime.UtcNow;

                _context.Files.Update(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Unarchived file {FileId}", fileId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unarchiving file {FileId}", fileId);
                throw;
            }
        }

        /// <summary>
        /// Increments download count and updates LastDownloadedAt
        /// </summary>
        public async Task<int> IncrementDownloadCountAsync(int fileId)
        {
            try
            {
                _logger.LogDebug("Incrementing download count for file {FileId}", fileId);

                var file = await _context.Files
                    .Where(f => f.FileId == fileId && f.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (file == null)
                {
                    _logger.LogWarning("File {FileId} not found", fileId);
                    return 0;
                }

                file.DownloadCount++;
                file.LastDownloadedAt = DateTime.UtcNow;

                _context.Files.Update(file);
                await _context.SaveChangesAsync();

                _logger.LogDebug("File {FileId} download count now {Count}", fileId, file.DownloadCount);
                return file.DownloadCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing download count for file {FileId}", fileId);
                throw;
            }
        }

        /// <summary>
        /// Checks if file exists and is owned by user
        /// </summary>
        public async Task<bool> ExistsAsync(int fileId, int userId)
        {
            try
            {
                return await _context.Files
                    .AsNoTracking()
                    .AnyAsync(f => f.FileId == fileId 
                                && f.UserId == userId 
                                && f.DeletedAt == null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence {FileId} for user {UserId}", fileId, userId);
                throw;
            }
        }

        /// <summary>
        /// Gets total storage size used by user's files
        /// </summary>
        public async Task<long> GetTotalStorageSizeAsync(int userId)
        {
            try
            {
                _logger.LogDebug("Calculating total storage for user {UserId}", userId);

                var totalSize = await _context.Files
                    .AsNoTracking()
                    .Where(f => f.UserId == userId && f.DeletedAt == null)
                    .SumAsync(f => (long?)f.FileSize ?? 0);

                _logger.LogInformation("User {UserId} total storage: {Size} bytes", userId, totalSize);
                return totalSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total storage for user {UserId}", userId);
                throw;
            }
        }
    }
}
