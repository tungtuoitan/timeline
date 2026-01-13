using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using System.Text.RegularExpressions;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Keyword service with PathIds design
    /// LongLink computed runtime, not stored
    /// </summary>
    public class KeywordServiceV2
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KeywordServiceV2> _logger;

        public KeywordServiceV2(
            ApplicationDbContext context,
            ILogger<KeywordServiceV2> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Get Keywords with LongLink

        /// <summary>
        /// Get all keywords for user with LongLink computed runtime
        /// Includes: all keywords from Keywords table (workspace/folder/note/file/heading/external)
        /// </summary>
        public async Task<List<KeywordDto>> GetKeywordsAsync(int userId)
        {
            try
            {
                // Get all keywords from Keywords table (including workspace keywords)
                // Exclude hard deleted keywords
                var keywords = await _context.Keywords
                    .Where(k => k.UserId == userId)
                    .ToListAsync();

                var enrichedKeywords = await EnrichWithLongLinksAsync(keywords);

                return enrichedKeywords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting keywords for UserId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Enrich keywords with computed LongLink
        /// </summary>
        private async Task<List<KeywordDto>> EnrichWithLongLinksAsync(List<Keyword> keywords)
        {
            if (!keywords.Any())
                return new List<KeywordDto>();

            // Fetch all workspace items needed
            var itemIds = keywords
                .SelectMany(k => new[] { k.TargetItemId, k.NoteItemId })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var items = await _context.Set<WorkspaceItemEntity>()
                .Where(i => itemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.PathIds, i.EntityType, i.EntityId, i.WorkspaceId })
                .ToListAsync();

            var itemMap = items.ToDictionary(
                i => i.Id,
                i => (i.Id, i.PathIds, i.EntityType, i.EntityId, i.WorkspaceId)
            );

            // Fetch entity names (workspace/folder/note)
            var workspaceIds = items.Where(i => i.EntityType == 1).Select(i => i.EntityId)
                .Concat(items.Select(i => i.WorkspaceId))
                .Distinct()
                .ToList();
            var folderIds = items.Where(i => i.EntityType == 2).Select(i => i.EntityId).ToList();
            var noteIds = items.Where(i => i.EntityType == 3).Select(i => i.EntityId).ToList();

            var workspaceNamesList = await _context.Workspaces
                .Where(w => workspaceIds.Contains(w.Id))
                .Select(w => new { w.Id, w.Name })
                .ToListAsync();
            var workspaceNames = workspaceNamesList.ToDictionary(w => w.Id, w => w.Name);

            var folderNamesList = await _context.Folders
                .Where(f => folderIds.Contains(f.Id))
                .Select(f => new { f.Id, f.Name })
                .ToListAsync();
            var folderNames = folderNamesList.ToDictionary(f => f.Id, f => f.Name);

            var noteNamesList = await _context.Notes
                .Where(n => noteIds.Contains(n.Id))
                .Select(n => new { n.Id, n.Name })
                .ToListAsync();
            var noteNames = noteNamesList.ToDictionary(n => n.Id, n => n.Name);

            // Build nameIndex maps for each entity type
            // We need to calculate nameIndex based on all keywords with same name
            var allKeywords = await _context.Keywords
                .Where(k => k.TargetItemId.HasValue)
                .OrderBy(k => k.CreatedAt)
                .ToListAsync();

            // Build maps: (EntityType, EntityId) -> NameIndex
            var nameIndexMap = new Dictionary<(byte EntityType, int EntityId), int>();

            // Group workspaces by name to calculate nameIndex
            var workspacesByName = workspaceNamesList.GroupBy(w => w.Name);
            foreach (var group in workspacesByName)
            {
                var index = 1;
                foreach (var ws in group.OrderBy(w => w.Id))
                {
                    nameIndexMap[(1, ws.Id)] = index++;
                }
            }

            // For folders/notes, use existing keywords to get nameIndex
            var folderKeywords = allKeywords.Where(k => k.Type == "folder" && k.TargetItemId.HasValue).ToList();
            foreach (var kw in folderKeywords)
            {
                var item = items.FirstOrDefault(i => i.Id == kw.TargetItemId.Value);
                if (item != null)
                {
                    nameIndexMap[(item.EntityType, item.EntityId)] = kw.NameIndex;
                }
            }

            var noteKeywords = allKeywords.Where(k => k.Type == "note" && k.TargetItemId.HasValue).ToList();
            foreach (var kw in noteKeywords)
            {
                var item = items.FirstOrDefault(i => i.Id == kw.TargetItemId.Value);
                if (item != null)
                {
                    nameIndexMap[(item.EntityType, item.EntityId)] = kw.NameIndex;
                }
            }

            // Build PathIds → LongLink cache
            var pathLongLinkCache = new Dictionary<string, string>();

            // Map keywords to DTOs
            return keywords.Select(k => new KeywordDto
            {
                Id = k.Id,
                Name = k.Name,
                NameIndex = k.NameIndex,
                Type = k.Type,
                Link = k.Link,
                LongLink = ComputeLongLink(k, itemMap, workspaceNames, folderNames, noteNames, pathLongLinkCache, nameIndexMap),
                Description = k.Description,
                HardDeletedAt = k.HardDeletedAt,
            }).ToList();
        }

        /// <summary>
        /// Compute LongLink from keyword data
        /// </summary>
        private string ComputeLongLink(
            Keyword keyword,
            Dictionary<int, (int Id, string PathIds, byte EntityType, int EntityId, int WorkspaceId)> itemMap,
            Dictionary<int, string> workspaceNames,
            Dictionary<int, string> folderNames,
            Dictionary<int, string> noteNames,
            Dictionary<string, string> cache,
            Dictionary<(byte EntityType, int EntityId), int> nameIndexMap)
        {
            // Workspace keyword - simple LongLink (just the name)
            if (keyword.Type == "workspace")
                return $"{keyword.Name}";

            // External keyword
            if (keyword.Type == "external")
                return $"{keyword.Name}";

            // Heading keyword
            if (keyword.Type.StartsWith("h"))
            {
                if (!keyword.NoteItemId.HasValue || !itemMap.ContainsKey(keyword.NoteItemId.Value))
                    return $"{keyword.Name}";

                var noteItem = itemMap[keyword.NoteItemId.Value];
                var noteLongLink = BuildLongLinkFromPathIds(noteItem.PathIds, noteItem.WorkspaceId, itemMap, workspaceNames, folderNames, noteNames, cache);
                return $"{noteLongLink}/{keyword.Name}";
            }

            // Folder/Note/File keyword
            if (!keyword.TargetItemId.HasValue || !itemMap.ContainsKey(keyword.TargetItemId.Value))
                return $"{keyword.Name}";

            var item = itemMap[keyword.TargetItemId.Value];
            return BuildLongLinkFromPathIds(item.PathIds, item.WorkspaceId, itemMap, workspaceNames, folderNames, noteNames, cache);
        }

        /// <summary>
        /// Build LongLink from PathIds
        /// Example: workspaceId=10, PathIds='/1/5/23/' → 'MyWorkspace/Folder1/Note1'
        /// </summary>
        private string BuildLongLinkFromPathIds(
            string pathIds,
            int workspaceId,
            Dictionary<int, (int Id, string PathIds, byte EntityType, int EntityId, int WorkspaceId)> itemMap,
            Dictionary<int, string> workspaceNames,
            Dictionary<int, string> folderNames,
            Dictionary<int, string> noteNames,
            Dictionary<string, string> cache)
        {
            // Create cache key with workspaceId
            var cacheKey = $"{workspaceId}:{pathIds}";

            // Check cache
            if (cache.TryGetValue(cacheKey, out var cachedLink))
                return cachedLink;

            var parts = new List<string>();

            // Add workspace name at the beginning
            if (workspaceNames.ContainsKey(workspaceId))
            {
                parts.Add(workspaceNames[workspaceId]);
            }
            else
            {
                parts.Add($"Workspace{workspaceId}");
            }

            // Parse PathIds: '/1/5/23/' → [1, 5, 23]
            var ids = pathIds.Trim('/').Split('/')
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(int.Parse)
                .ToList();

            foreach (var id in ids)
            {
                if (!itemMap.TryGetValue(id, out var item))
                {
                    parts.Add($"Unknown{id}");
                    continue;
                }

                string name = "Unknown";

                // Get name based on entity type (no nameIndex anymore)
                if (item.EntityType == 2 && folderNames.ContainsKey(item.EntityId))
                {
                    name = folderNames[item.EntityId];
                }
                else if (item.EntityType == 3 && noteNames.ContainsKey(item.EntityId))
                {
                    name = noteNames[item.EntityId];
                }

                // Format: just name (no nameIndex)
                parts.Add(name);
            }

            var longLink = string.Join("/", parts);
            cache[cacheKey] = longLink;
            return longLink;
        }

        #endregion

        #region Create/Update Keywords

        /// <summary>
        /// Create or update workspace keyword
        /// </summary>
        public async Task<Keyword> SyncWorkspaceKeywordAsync(int workspaceId, int userId)
        {
            try
            {
                var workspace = await _context.Workspaces.FindAsync(workspaceId);
                if (workspace == null)
                    throw new ArgumentException($"Workspace {workspaceId} not found");

                var link = $"w{workspaceId}";

                // Find existing keyword by WorkspaceId
                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.WorkspaceId == workspaceId && k.Type == "workspace");

                if (keyword == null)
                {
                    // Create new keyword
                    var nameIndex = await GetNextNameIndexAsync(workspace.Name);
                    keyword = new Keyword(
                        name: workspace.Name,
                        nameIndex: nameIndex,
                        link: link,
                        type: "workspace",
                        userId: userId,
                        workspaceId: workspaceId
                    );
                    _context.Keywords.Add(keyword);
                }
                else
                {
                    // Check if updating will cause duplicate (Name + NameIndex already exists for another keyword)
                    var duplicateKeyword = await _context.Keywords
                        .FirstOrDefaultAsync(k => k.Name == workspace.Name &&
                                                  k.NameIndex == keyword.NameIndex &&
                                                  k.Id != keyword.Id &&
                                                  k.HardDeletedAt == null);

                    int nameIndexToUse = keyword.NameIndex;
                    if (duplicateKeyword != null)
                    {
                        // Generate new nameIndex to avoid duplicate
                        nameIndexToUse = await GetNextNameIndexAsync(workspace.Name);
                        _logger.LogWarning(
                            "Duplicate detected when updating workspace keyword ID {KeywordId}. Name '{Name}' + NameIndex {OldIndex} already exists (Keyword ID {DuplicateId}). Generating new NameIndex: {NewIndex}",
                            keyword.Id, workspace.Name, keyword.NameIndex, duplicateKeyword.Id, nameIndexToUse);
                    }

                    // Update existing keyword
                    keyword.Update(
                        name: workspace.Name,
                        nameIndex: nameIndexToUse,
                        link: link,
                        type: "workspace",
                        description: workspace.Description,
                        workspaceId: workspaceId
                    );
                }

                await _context.SaveChangesAsync();
                return keyword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing workspace keyword for WorkspaceId: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Create or update folder keyword
        /// </summary>
        public async Task<Keyword> SyncFolderKeywordAsync(int workspaceItemId, int userId)
        {
            try
            {
                var item = await _context.Set<WorkspaceItemEntity>().FindAsync(workspaceItemId);
                if (item == null || item.EntityType != 2)
                    throw new ArgumentException($"Folder workspace item {workspaceItemId} not found");

                var folder = await _context.Folders.FindAsync(item.EntityId);
                if (folder == null)
                    throw new ArgumentException($"Folder {item.EntityId} not found");

                var link = await BuildItemLinkAsync(item.PathIds, item.WorkspaceId, workspaceItemId);

                // Find existing keyword by TargetItemId (stable identifier - doesn't change on move)
                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.TargetItemId == workspaceItemId);

                if (keyword == null)
                {
                    // Create new keyword
                    var nameIndex = await GetNextNameIndexAsync(folder.Name);
                    keyword = new Keyword(
                        name: folder.Name,
                        nameIndex: nameIndex,
                        link: link,
                        type: "folder",
                        userId: userId,
                        targetItemId: workspaceItemId,
                        pathIds: item.PathIds
                    );
                    _context.Keywords.Add(keyword);
                }
                else
                {
                    // Check if updating will cause duplicate (Name + NameIndex already exists for another keyword)
                    var duplicateKeyword = await _context.Keywords
                        .FirstOrDefaultAsync(k => k.Name == folder.Name &&
                                                  k.NameIndex == keyword.NameIndex &&
                                                  k.Id != keyword.Id &&
                                                  k.HardDeletedAt == null);

                    int nameIndexToUse = keyword.NameIndex;
                    if (duplicateKeyword != null)
                    {
                        // Generate new nameIndex to avoid duplicate
                        nameIndexToUse = await GetNextNameIndexAsync(folder.Name);
                        _logger.LogWarning(
                            "Duplicate detected when updating folder keyword ID {KeywordId}. Name '{Name}' + NameIndex {OldIndex} already exists (Keyword ID {DuplicateId}). Generating new NameIndex: {NewIndex}",
                            keyword.Id, folder.Name, keyword.NameIndex, duplicateKeyword.Id, nameIndexToUse);
                    }

                    // Update existing keyword
                    keyword.Update(
                        name: folder.Name,
                        nameIndex: nameIndexToUse,
                        link: link,
                        type: "folder",
                        targetItemId: workspaceItemId,
                        pathIds: item.PathIds
                    );
                }

                await _context.SaveChangesAsync();
                return keyword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing folder keyword for WorkspaceItemId: {WorkspaceItemId}", workspaceItemId);
                throw;
            }
        }

        /// <summary>
        /// Create or update note keyword + sync headings
        /// </summary>
        public async Task<Keyword> SyncNoteKeywordAsync(int workspaceItemId, int userId)
        {
            try
            {
                var item = await _context.Set<WorkspaceItemEntity>().FindAsync(workspaceItemId);
                if (item == null || item.EntityType != 3)
                    throw new ArgumentException($"Note workspace item {workspaceItemId} not found");

                var note = await _context.Notes.FindAsync(item.EntityId);
                if (note == null)
                    throw new ArgumentException($"Note {item.EntityId} not found");

                _logger.LogInformation("SyncNoteKeywordAsync - WorkspaceItemId: {WorkspaceItemId}, PathIds: '{PathIds}', WorkspaceId: {WorkspaceId}",
                    workspaceItemId, item.PathIds, item.WorkspaceId);

                var link = await BuildItemLinkAsync(item.PathIds, item.WorkspaceId, workspaceItemId);

                _logger.LogInformation("SyncNoteKeywordAsync - Built link: '{Link}' for note '{NoteName}'", link, note.Name);

                // Find existing keyword by TargetItemId (stable identifier - doesn't change on move)
                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.TargetItemId == workspaceItemId);

                if (keyword == null)
                {
                    _logger.LogInformation("Creating new keyword for note '{NoteName}' with link '{Link}'", note.Name, link);

                    // Check if link already exists (safety check)
                    var existingKeywordWithSameLink = await _context.Keywords
                        .FirstOrDefaultAsync(k => k.Link == link);

                    if (existingKeywordWithSameLink != null)
                    {
                        _logger.LogError("DUPLICATE LINK DETECTED! Link '{Link}' already exists for keyword ID {KeywordId} (TargetItemId: {ExistingTargetItemId}). Current item: {CurrentItemId}",
                            link, existingKeywordWithSameLink.Id, existingKeywordWithSameLink.TargetItemId, workspaceItemId);
                        throw new InvalidOperationException($"Duplicate keyword link detected: {link}");
                    }

                    // Create new keyword
                    var nameIndex = await GetNextNameIndexAsync(note.Name);
                    keyword = new Keyword(
                        name: note.Name,
                        nameIndex: nameIndex,
                        link: link,
                        type: "note",
                        userId: userId,
                        targetItemId: workspaceItemId,
                        pathIds: item.PathIds
                    );
                    _context.Keywords.Add(keyword);
                }
                else
                {
                    _logger.LogInformation("Updating existing keyword ID {KeywordId} for note '{NoteName}' with new link '{Link}'", keyword.Id, note.Name, link);

                    // Check if updating will cause duplicate (Name + NameIndex already exists for another keyword)
                    var duplicateKeyword = await _context.Keywords
                        .FirstOrDefaultAsync(k => k.Name == note.Name &&
                                                  k.NameIndex == keyword.NameIndex &&
                                                  k.Id != keyword.Id &&
                                                  k.HardDeletedAt == null);

                    int nameIndexToUse = keyword.NameIndex;
                    if (duplicateKeyword != null)
                    {
                        // Generate new nameIndex to avoid duplicate
                        nameIndexToUse = await GetNextNameIndexAsync(note.Name);
                        _logger.LogWarning(
                            "Duplicate detected when updating note keyword ID {KeywordId}. Name '{Name}' + NameIndex {OldIndex} already exists (Keyword ID {DuplicateId}). Generating new NameIndex: {NewIndex}",
                            keyword.Id, note.Name, keyword.NameIndex, duplicateKeyword.Id, nameIndexToUse);
                    }

                    // Update existing keyword
                    keyword.Update(
                        name: note.Name,
                        nameIndex: nameIndexToUse,
                        link: link,
                        type: "note",
                        targetItemId: workspaceItemId,
                        pathIds: item.PathIds
                    );
                }

                await _context.SaveChangesAsync();

                // Sync headings
                await SyncNoteHeadingsAsync(note, workspaceItemId, userId);

                return keyword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing note keyword for WorkspaceItemId: {WorkspaceItemId}", workspaceItemId);
                throw;
            }
        }

        /// <summary>
        /// Sync headings for a note (soft delete old + insert/restore new)
        /// Strategy: Mark old headings as deleted, then insert or restore matching headings
        /// </summary>
        private async Task SyncNoteHeadingsAsync(Note note, int noteWorkspaceItemId, int userId)
        {
            // Get all existing headings for this note (including soft deleted ones)
            var existingHeadings = await _context.Keywords
                .Where(k => k.NoteItemId == noteWorkspaceItemId &&
                           (k.Type == "h1" || k.Type == "h2" || k.Type == "h3" ||
                            k.Type == "h4" || k.Type == "h5" || k.Type == "h6"))
                .ToListAsync();

            // Extract new headings from markdown
            if (string.IsNullOrEmpty(note.Description))
            {
                // No headings in markdown - delete all existing headings
                foreach (var heading in existingHeadings.Where(h => h.HardDeletedAt == null))
                {
                    heading.HardDeletedAt = DateTime.UtcNow;
                    heading.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return;
            }

            var headings = ExtractHeadingsFromMarkdown(note.Description);
            var headingStack = new Stack<(int Level, string Title, int NameIndex)>();

            var noteItem = await _context.Set<WorkspaceItemEntity>().FindAsync(noteWorkspaceItemId);
            var noteLink = await BuildItemLinkAsync(noteItem!.PathIds, noteItem.WorkspaceId, noteWorkspaceItemId);

            // Track which existing headings are still valid (to avoid soft deleting them)
            var processedHeadingPaths = new HashSet<string>();

            // CRITICAL: Track nameIndex assignments in this batch to avoid duplicates
            var batchNameIndexTracker = new Dictionary<string, int>();

            foreach (var (level, title) in headings)
            {
                // Pop headings with same or higher level
                while (headingStack.Count > 0 && headingStack.Peek().Level >= level)
                {
                    headingStack.Pop();
                }

                // Build heading path for nameIndex calculation (full path to ensure uniqueness)
                var headingPath = string.Join("/", headingStack.Reverse().Select(h => h.Title));
                if (headingStack.Count > 0)
                    headingPath += "/";
                headingPath += title;

                // Build links (include current title)
                var headingPathForLink = headingStack.Count > 0
                    ? string.Join("/", headingStack.Reverse().Select(h => h.Title)) + "/" + title
                    : title;
                var fullLink = $"{noteLink}/{headingPathForLink}";

                // Check if heading already exists (by HeadingPath + NoteItemId + Type)
                var existingHeading = existingHeadings.FirstOrDefault(h =>
                    h.HeadingPath == headingPathForLink &&
                    h.Type == $"h{level}");

                if (existingHeading != null)
                {
                    // Restore if deleted, update if changed
                    if (existingHeading.HardDeletedAt.HasValue)
                    {
                        existingHeading.HardDeletedAt = null;
                        existingHeading.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Restored heading keyword {KeywordId} for note {NoteItemId}",
                            existingHeading.Id, noteWorkspaceItemId);
                    }

                    // Update name/link if changed
                    if (existingHeading.Name != title || existingHeading.Link != fullLink)
                    {
                        existingHeading.Name = title;
                        existingHeading.Link = fullLink;
                        existingHeading.UpdatedAt = DateTime.UtcNow;
                    }

                    processedHeadingPaths.Add(headingPathForLink);
                    var nameIndex = existingHeading.NameIndex;
                    headingStack.Push((level, title, nameIndex));
                }
                else
                {
                    // Create new heading - pass batch tracker to avoid duplicate nameIndex
                    var nameIndex = await GetNextNameIndexAsync(title, batchNameIndexTracker);
                    headingStack.Push((level, title, nameIndex));

                    var heading = new Keyword(
                        name: title, // Only the last heading title, not the full path
                        nameIndex: nameIndex,
                        link: fullLink,
                        type: $"h{level}",
                        userId: userId,
                        noteItemId: noteWorkspaceItemId,
                        headingPath: headingPathForLink
                    );

                    _context.Keywords.Add(heading);
                    processedHeadingPaths.Add(headingPathForLink);
                }
            }

            // delete headings that no longer exist in markdown
            foreach (var heading in existingHeadings)
            {
                if (!processedHeadingPaths.Contains(heading.HeadingPath ?? "") &&
                    heading.HardDeletedAt == null)
                {
                    heading.HardDeletedAt = DateTime.UtcNow;
                    heading.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation("deleted heading keyword {KeywordId} (HeadingPath: {HeadingPath}) for note {NoteItemId}",
                        heading.Id, heading.HeadingPath, noteWorkspaceItemId);
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Batch upsert external keywords
        /// </summary>
        public async Task<ResultOptions> UpsertExternalKeywordsAsync(int userId, List<UpsertExternalKeywordRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "No external keywords provided",
                        Status = 400
                    };
                }

                var successCount = 0;
                var failCount = 0;
                var errors = new List<string>();
                var results = new List<KeywordDto>();

                // CRITICAL: Track nameIndex assignments in this batch to avoid duplicates
                var batchNameIndexTracker = new Dictionary<string, int>();

                foreach (var request in requests)
                {
                    try
                    {
                        Keyword keyword;

                        if (request.Id.HasValue && request.Id.Value > 0)
                        {
                            // UPDATE existing keyword
                            keyword = await _context.Keywords.FindAsync(request.Id.Value);
                            if (keyword == null)
                            {
                                errors.Add($"Keyword ID {request.Id} not found");
                                failCount++;
                                continue;
                            }

                            // Update fields
                            keyword.Update(
                                name: request.Name,
                                nameIndex: keyword.NameIndex, // Keep existing nameIndex
                                link: request.Link,
                                type: "external",
                                externalUrl: request.Link
                            );

                            if (!string.IsNullOrEmpty(request.Description))
                                keyword.Description = request.Description;
                        }
                        else
                        {
                            // CREATE new external keyword - pass batch tracker to avoid duplicate nameIndex
                            var nameIndex = await GetNextNameIndexAsync(request.Name, batchNameIndexTracker);

                            keyword = new Keyword(
                                name: request.Name,
                                nameIndex: nameIndex,
                                link: request.Link,
                                type: "external",
                                userId: userId,
                                externalUrl: request.Link
                            );

                            if (!string.IsNullOrEmpty(request.Description))
                                keyword.Description = request.Description;

                            _context.Keywords.Add(keyword);
                        }

                        await _context.SaveChangesAsync();

                        // Build response DTO
                        var dto = new KeywordDto
                        {
                            Id = keyword.Id,
                            Name = keyword.Name,
                            NameIndex = keyword.NameIndex,
                            Type = keyword.Type,
                            Link = keyword.Link,
                            LongLink = $"{keyword.Name}", // External keywords have simple LongLink
                            Description = keyword.Description
                        };

                        results.Add(dto);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error upserting external keyword with Name: {Name}", request.Name);
                        errors.Add($"Keyword '{request.Name}': {ex.Message}");
                        failCount++;
                    }
                }

                var message = successCount > 0
                    ? $"Successfully upserted {successCount}/{requests.Count} external keywords"
                    : "Failed to upsert all external keywords";

                if (failCount > 0)
                {
                    message += $". {failCount} failed.";
                }

                return new ResultOptions
                {
                    Success = successCount > 0,
                    Message = message,
                    Object = new
                    {
                        SuccessCount = successCount,
                        FailCount = failCount,
                        Errors = errors,
                        Keywords = results
                    },
                    Status = successCount > 0 ? 200 : 400
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during batch upsert external keywords");
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Process external links in note description
        /// Extracts all ((name|url)) patterns, creates keywords, and replaces with [[id]]
        /// Does NOT call SaveChangesAsync - caller manages transaction
        /// </summary>
        /// <param name="description">Note description with external links</param>
        /// <param name="userId">User ID for keyword ownership</param>
        /// <returns>Updated description with [[id]] format</returns>
        public async Task<string> ProcessExternalLinksInDescriptionAsync(string description, int userId)
        {
            try
            {
                if (string.IsNullOrEmpty(description))
                    return description;

                // Match ((name|url)) where url can be any string (no need http/https)
                var externalLinkPattern = new Regex(@"\(\(([^\|\)]+)\|([^\)]+)\)\)");

                var matches = externalLinkPattern.Matches(description);

                if (matches.Count == 0)
                    return description; // No external links found

                _logger.LogInformation("Found {Count} external links in description", matches.Count);

                var updatedDescription = description;

                // CRITICAL: Track nameIndex assignments in this batch to avoid duplicates
                var batchNameIndexTracker = new Dictionary<string, int>();

                // Process each external link
                foreach (Match match in matches)
                {
                    var originalText = match.Value; // ((name|url))
                    var name = match.Groups[1].Value.Trim();
                    var url = match.Groups[2].Value.Trim();

                    try
                    {
                        // Check if keyword with this URL already exists for this user
                        var existingKeyword = await _context.Keywords
                            .FirstOrDefaultAsync(k =>
                                k.ExternalUrl == url &&
                                k.UserId == userId &&
                                k.Type == "external" &&
                                k.HardDeletedAt == null);

                        Keyword keyword;

                        if (existingKeyword != null)
                        {
                            // Reuse existing keyword
                            keyword = existingKeyword;
                            _logger.LogInformation("Reusing existing external keyword ID {KeywordId} for URL: {Url}",
                                keyword.Id, url);
                        }
                        else
                        {
                            // Create new external keyword - pass batch tracker to avoid duplicate nameIndex
                            var nameIndex = await GetNextNameIndexAsync(name, batchNameIndexTracker);

                            keyword = new Keyword(
                                name: name,
                                nameIndex: nameIndex,
                                link: url,
                                type: "external",
                                userId: userId,
                                externalUrl: url
                            );

                            _context.Keywords.Add(keyword);

                            // Save to get the ID (within the same transaction context)
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("Created new external keyword ID {KeywordId} for name '{Name}' and URL: {Url}",
                                keyword.Id, name, url);
                        }

                        // Replace ((name|url)) with [[id]]
                        var replacement = $"[[{keyword.Id}]]";
                        updatedDescription = updatedDescription.Replace(originalText, replacement);

                        _logger.LogInformation("Replaced '{Original}' with '{Replacement}'", originalText, replacement);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing external link: {OriginalText}", originalText);
                        // Continue processing other links - don't fail the whole operation
                    }
                }

                return updatedDescription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing external links in description for UserId: {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region Move Operations

        /// <summary>
        /// Rebuild links for keywords after workspace item move
        /// </summary>
        public async Task RebuildLinksAfterMoveAsync(string oldPathIdsPrefix, string newPathIdsPrefix)
        {
            try
            {
                // Find all keywords with PathIds starting with old prefix
                var keywords = await _context.Keywords
                    .Where(k => k.PathIds != null && k.PathIds.StartsWith(oldPathIdsPrefix))
                    .ToListAsync();

                _logger.LogInformation("Rebuilding {Count} keywords after move", keywords.Count);

                // Fetch workspace IDs for all keywords
                var targetItemIds = keywords
                    .Where(k => k.TargetItemId.HasValue)
                    .Select(k => k.TargetItemId!.Value)
                    .Distinct()
                    .ToList();

                var itemWorkspaces = await _context.Set<WorkspaceItemEntity>()
                    .Where(i => targetItemIds.Contains(i.Id))
                    .Select(i => new { i.Id, i.WorkspaceId })
                    .ToListAsync();

                var workspaceMap = itemWorkspaces.ToDictionary(i => i.Id, i => i.WorkspaceId);

                foreach (var keyword in keywords)
                {
                    // Update PathIds
                    keyword.PathIds = keyword.PathIds!.Replace(oldPathIdsPrefix, newPathIdsPrefix);

                    // Rebuild Link
                    if (keyword.TargetItemId.HasValue && workspaceMap.ContainsKey(keyword.TargetItemId.Value))
                    {
                        var workspaceId = workspaceMap[keyword.TargetItemId.Value];
                        keyword.Link = await BuildItemLinkAsync(keyword.PathIds, workspaceId, keyword.TargetItemId.Value);
                    }
                    else
                    {
                        // Fallback: keep old link format or log warning
                        _logger.LogWarning("Cannot rebuild link for keyword {Id}: missing TargetItemId or WorkspaceId", keyword.Id);
                    }

                    keyword.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding links after move from '{OldPrefix}' to '{NewPrefix}'", oldPathIdsPrefix, newPathIdsPrefix);
                throw;
            }
        }

        #endregion

        #region Delete Operations


        /// <summary>
        /// Soft delete workspace keyword (when workspace is soft deleted - set HardDeletedAt)
        /// </summary>
        public async Task DeleteWorkspaceKeywordAsync(int workspaceId)
        {
            try
            {
                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.WorkspaceId == workspaceId && k.Type == "workspace");

                if (keyword != null && keyword.HardDeletedAt == null)
                {
                    keyword.HardDeletedAt = DateTime.UtcNow;
                    keyword.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Soft deleted workspace keyword for WorkspaceId: {WorkspaceId}", workspaceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workspace keyword for WorkspaceId: {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Delete all keywords for a workspace item and its descendants
        /// </summary>
        public async Task DeleteKeywordsByPathAsync(string pathIds)
        {
            try
            {
                var keywords = await _context.Keywords
                    .Where(k => k.PathIds != null && k.PathIds.StartsWith(pathIds))
                    .ToListAsync();

                _context.Keywords.RemoveRange(keywords);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting keywords by path: {PathIds}", pathIds);
                throw;
            }
        }

        /// <summary>
        /// Hard delete keywords for deleted notes (set HardDeletedAt)
        /// Marks both note keyword and its heading keywords
        /// </summary>
        public async Task HardDeleteNoteKeywordsAsync(List<int> noteIds)
        {
            try
            {
                if (noteIds == null || !noteIds.Any())
                    return;

                _logger.LogInformation("Hard deleting keywords for {Count} notes", noteIds.Count);

                // Get all workspace_items for these notes (EntityType=3)
                var workspaceItems = await _context.WorkspaceItems
                    .Where(wi => wi.EntityType == 3 && noteIds.Contains(wi.EntityId))
                    .Select(wi => wi.Id)
                    .ToListAsync();

                if (!workspaceItems.Any())
                {
                    _logger.LogInformation("No workspace_items found for notes, skipping keyword hard delete");
                    return;
                }

                // Find all keywords for these notes:
                // 1. Note keywords (TargetItemId matches workspace_item)
                // 2. Heading keywords (NoteItemId matches workspace_item)
                var keywords = await _context.Keywords
                    .Where(k => (k.TargetItemId.HasValue && workspaceItems.Contains(k.TargetItemId.Value)) ||
                               (k.NoteItemId.HasValue && workspaceItems.Contains(k.NoteItemId.Value)))
                    .Where(k => k.HardDeletedAt == null) // Only mark non-deleted keywords
                    .ToListAsync();

                if (!keywords.Any())
                {
                    _logger.LogInformation("No keywords found for notes, skipping hard delete");
                    return;
                }

                // Set HardDeletedAt for all keywords
                var now = DateTime.UtcNow;
                foreach (var keyword in keywords)
                {
                    keyword.HardDeletedAt = now;
                    keyword.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Hard deleted {Count} keywords (notes + headings) for {NoteCount} notes",
                    keywords.Count, noteIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting keywords for notes: {NoteIds}", string.Join(",", noteIds ?? new List<int>()));
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Build Link from PathIds + WorkspaceId
        /// Example: PathIds="/171/25/" + WorkspaceId=10 → "w10/f171/n25"
        /// PathIds already includes the current item, so no need to append currentItemId
        /// </summary>
        private async Task<string> BuildItemLinkAsync(string pathIds, int workspaceId, int currentItemId)
        {
            // PathIds should always contain the current item
            // If empty/root, this is an error condition (items should have PathIds)
            if (string.IsNullOrEmpty(pathIds) || pathIds == "/")
            {
                _logger.LogWarning("BuildItemLinkAsync called with empty PathIds for item {ItemId}", currentItemId);
                // Fallback: determine prefix from current item's EntityType
                var currentItem = await _context.Set<WorkspaceItemEntity>().FindAsync(currentItemId);
                var prefix = currentItem?.EntityType switch
                {
                    2 => "f", // folder
                    3 => "n", // note
                    4 => "file", // file
                    _ => "?"
                };
                return $"w{workspaceId}/{prefix}{currentItemId}";
            }

            var ids = pathIds.Trim('/').Split('/')
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(int.Parse)
                .ToList();

            if (!ids.Any())
            {
                // No valid IDs in path, fallback
                _logger.LogWarning("BuildItemLinkAsync: No valid IDs in PathIds '{PathIds}' for item {ItemId}", pathIds, currentItemId);
                var currentItem = await _context.Set<WorkspaceItemEntity>().FindAsync(currentItemId);
                var prefix = currentItem?.EntityType switch
                {
                    2 => "f", // folder
                    3 => "n", // note
                    4 => "file", // file
                    _ => "?"
                };
                return $"w{workspaceId}/{prefix}{currentItemId}";
            }

            // Fetch EntityTypes for all items in PathIds
            var items = await _context.Set<WorkspaceItemEntity>()
                .Where(i => ids.Contains(i.Id))
                .Select(i => new { i.Id, i.EntityType })
                .ToListAsync();

            var itemMap = items.ToDictionary(i => i.Id, i => i.EntityType);

            // Build link parts
            var parts = new List<string> { $"w{workspaceId}" };

            foreach (var id in ids)
            {
                if (!itemMap.ContainsKey(id))
                {
                    parts.Add($"?{id}"); // Unknown item
                    continue;
                }

                var entityType = itemMap[id];
                var prefix = entityType switch
                {
                    2 => "f", // folder
                    3 => "n", // note
                    4 => "file", // file
                    _ => "?"
                };
                parts.Add($"{prefix}{id}");
            }

            // PathIds already includes current item, so NO append needed
            return string.Join("/", parts);
        }

        /// <summary>
        /// Get next nameIndex for a name
        /// Supports batch operation by tracking assigned nameIndex values
        /// </summary>
        /// <param name="name">Keyword name</param>
        /// <param name="batchTracker">Optional dictionary tracking nameIndex already assigned in current batch (key: name, value: max nameIndex used)</param>
        /// <returns>Next available nameIndex</returns>
        private async Task<int> GetNextNameIndexAsync(string name, Dictionary<string, int>? batchTracker = null)
        {
            // Get max nameIndex from database
            var maxIndexDb = await _context.Keywords
                .Where(k => k.Name == name)
                .MaxAsync(k => (int?)k.NameIndex) ?? 0;

            // If batch tracker provided, also check in-memory assigned values
            var maxIndexBatch = 0;
            if (batchTracker != null && batchTracker.TryGetValue(name, out var trackedIndex))
            {
                maxIndexBatch = trackedIndex;
            }

            // Use the higher value between DB and batch
            var maxIndex = Math.Max(maxIndexDb, maxIndexBatch);
            var nextIndex = maxIndex + 1;

            // Update batch tracker if provided
            if (batchTracker != null)
            {
                batchTracker[name] = nextIndex;
            }

            return nextIndex;
        }

        /// <summary>
        /// Extract headings from markdown
        /// </summary>
        private List<(int Level, string Title)> ExtractHeadingsFromMarkdown(string markdown)
        {
            var headings = new List<(int, string)>();
            var lines = markdown.Split('\n');

            foreach (var line in lines)
            {
                var match = Regex.Match(line.Trim(), @"^(#{1,6})\s+(.+)$");
                if (match.Success)
                {
                    var level = match.Groups[1].Value.Length;
                    var title = match.Groups[2].Value.Trim();
                    headings.Add((level, title));
                }
            }

            return headings;
        }

        #endregion
    }
}
