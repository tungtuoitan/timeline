using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class WorkspaceRepository : IWorkspaceRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkspaceRepository> _logger;

        public WorkspaceRepository(
            ApplicationDbContext context,
            ILogger<WorkspaceRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the complete workspace tree (folders, notes, and files) with hierarchy
        /// </summary>
        public async Task<WorkspaceWithTree?> GetWorkspaceTreeAsync(int workspaceId, int userId)
        {
            try
            {
                _logger.LogInformation("Getting workspace tree for workspaceId: {WorkspaceId}, userId: {UserId}",
                    workspaceId, userId);

                // Get workspace
                var workspace = await _context.Workspaces
                    .Where(w => w.Id == workspaceId && w.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (workspace == null)
                {
                    _logger.LogWarning("Workspace not found: {WorkspaceId}", workspaceId);
                    return null;
                }

                // Get all workspace items
                var items = await _context.WorkspaceItems
                    .Where(i => i.WorkspaceId == workspaceId)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} items in workspace {WorkspaceId}", items.Count, workspaceId);

                // Get distinct IDs for each type
                var folderIds = items.Where(i => i.ItemType == 2).Select(i => i.ItemId).Distinct().ToList(); // item_type = 2 (folder)
                var noteIds = items.Where(i => i.ItemType == 3).Select(i => i.ItemId).Distinct().ToList();   // item_type = 3 (note)
                var fileIds = items.Where(i => i.ItemType == 4).Select(i => i.ItemId).Distinct().ToList();   // item_type = 4 (file)

                // Load entities
                var folders = await _context.Folders
                    .Where(f => folderIds.Contains(f.Id))
                    .ToDictionaryAsync(f => f.Id);

                var notes = await _context.Notes
                    .Where(n => noteIds.Contains(n.Id))
                    .ToDictionaryAsync(n => n.Id);

                var files = await _context.Files
                    .Where(f => fileIds.Contains(f.Id))
                    .ToDictionaryAsync(f => f.Id);

                // Build tree items
                var treeItems = new List<WorkspaceTreeItem>();

                foreach (var item in items)
                {
                    var treeItem = new WorkspaceTreeItem
                    {
                        ItemId = item.Id,
                        ChildId = item.ItemId,
                        ItemType = GetItemTypeName(item.ItemType), // Convert TINYINT to string
                        UserId = userId,
                        Name = "",
                        ParentId = item.FolderId, // folder_id from ws.folders
                        Level = 0,
                        Position = 0,
                        AccessType = item.IsOriginal ? "owner" : "shared",
                        IsOriginal = item.IsOriginal
                    };

                    // Populate name and metadata based on type
                    if (item.ItemType == 2 && folders.TryGetValue(item.ItemId, out var folder))
                    {
                        treeItem.Name = folder.Name;
                        treeItem.Color = folder.Color;
                        treeItem.Icon = folder.Icon;
                    }
                    else if (item.ItemType == 3 && notes.TryGetValue(item.ItemId, out var note))
                    {
                        treeItem.Name = note.Name;
                    }
                    else if (item.ItemType == 4 && files.TryGetValue(item.ItemId, out var file))
                    {
                        treeItem.Name = file.Name;
                    }

                    treeItems.Add(treeItem);
                }

                _logger.LogInformation("Built {Count} tree items for workspace {WorkspaceId}", treeItems.Count, workspaceId);

                return new WorkspaceWithTree
                {
                    WorkspaceId = workspace.Id,
                    Name = workspace.Name,
                    Description = workspace.Description,
                    UserId = workspace.UserId,
                    CreatedAt = workspace.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = workspace.UpdatedAt,
                    Items = treeItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace tree for workspaceId: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Gets workspace by ID
        /// </summary>
        public async Task<Workspace?> GetWorkspaceByIdAsync(int workspaceId, int userId)
        {
            try
            {
                return await _context.Workspaces
                    .Where(w => w.Id == workspaceId && w.DeletedAt == null)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace by ID: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Creates a new folder in a workspace
        /// </summary>
        public async Task<ResultOptions> CreateFolderAsync(int workspaceId, int userId, string name, string? description, string? color, string? icon, int? parentFolderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Creating folder '{Name}' in workspace {WorkspaceId} for user {UserId}",
                    name, workspaceId, userId);

                // 1. Create folder entity
                var folder = new Folder(name, userId)
                {
                    Description = description,
                    Color = color ?? "#F59E0B",  // Default amber color
                    Icon = icon ?? "📁"           // Default folder emoji
                };

                _context.Folders.Add(folder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Folder created with ID: {FolderId}", folder.Id);

                // 2. Create workspace_item link (ws.workspace_items table)
                var workspaceItem = new WorkspaceItem(workspaceId, 2, folder.Id, parentFolderId)
                {
                    IsOriginal = true  // This workspace owns the folder
                };

                _context.WorkspaceItems.Add(workspaceItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("WorkspaceItem created with ID: {ItemId} linking folder {FolderId} to workspace {WorkspaceId}",
                    workspaceItem.Id, folder.Id, workspaceId);

                await transaction.CommitAsync();

                return new ResultOptions
                {
                    Success = true,
                    Message = "Folder created successfully",
                    Reference = folder.Id.ToString(),
                    Object = folder,
                    Status = 201
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder '{Name}' in workspace {WorkspaceId}", name, workspaceId);
                await transaction.RollbackAsync();
                
                return new ResultOptions
                {
                    Success = false,
                    Message = $"Failed to create folder: {ex.Message}",
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Helper method to convert TINYINT item_type to string name
        /// </summary>
        private string GetItemTypeName(byte itemType)
        {
            return itemType switch
            {
                1 => "workspace",
                2 => "folder",
                3 => "note",
                4 => "file",
                _ => "unknown"
            };
        }
    }
}
