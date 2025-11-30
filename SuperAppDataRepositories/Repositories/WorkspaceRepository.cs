using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
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
                    .Where(w => w.WorkspaceId == workspaceId && w.DeletedAt == null)
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
                var folderIds = items.Where(i => i.ItemType == "2").Select(i => i.ChildId).Distinct().ToList(); // item_type = 2 (folder)
                var noteIds = items.Where(i => i.ItemType == "3").Select(i => i.ChildId).Distinct().ToList();   // item_type = 3 (note)
                var fileIds = items.Where(i => i.ItemType == "4").Select(i => i.ChildId).Distinct().ToList();   // item_type = 4 (file)

                // Load entities
                var folders = await _context.Folders
                    .Where(f => folderIds.Contains(f.FolderId))
                    .ToDictionaryAsync(f => f.FolderId);

                var notes = await _context.Notes
                    .Where(n => noteIds.Contains(n.NoteId))
                    .ToDictionaryAsync(n => n.NoteId);

                var files = await _context.Files
                    .Where(f => fileIds.Contains(f.FileId))
                    .ToDictionaryAsync(f => f.FileId);

                // Build tree items
                var treeItems = new List<WorkspaceTreeItem>();

                foreach (var item in items)
                {
                    var treeItem = new WorkspaceTreeItem
                    {
                        ItemId = item.ItemId,
                        ChildId = item.ChildId,
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
                    if (item.ItemType == "2" && folders.TryGetValue(item.ChildId, out var folder))
                    {
                        treeItem.Name = folder.Name;
                        treeItem.Slug = folder.Slug;
                        treeItem.Color = folder.Color;
                        treeItem.Icon = folder.Icon;
                    }
                    else if (item.ItemType == "3" && notes.TryGetValue(item.ChildId, out var note))
                    {
                        treeItem.Name = note.Name;
                        treeItem.Slug = note.Slug;
                    }
                    else if (item.ItemType == "4" && files.TryGetValue(item.ChildId, out var file))
                    {
                        treeItem.Name = file.Name;
                        treeItem.Slug = file.Slug;
                    }

                    treeItems.Add(treeItem);
                }

                _logger.LogInformation("Built {Count} tree items for workspace {WorkspaceId}", treeItems.Count, workspaceId);

                return new WorkspaceWithTree
                {
                    WorkspaceId = workspace.WorkspaceId,
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
                    .Where(w => w.WorkspaceId == workspaceId && w.DeletedAt == null)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace by ID: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Helper method to convert TINYINT item_type to string name
        /// </summary>
        private string GetItemTypeName(string itemType)
        {
            return itemType switch
            {
                "1" => "workspace",
                "2" => "folder",
                "3" => "note",
                "4" => "file",
                _ => "unknown"
            };
        }
    }
}
