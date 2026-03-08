using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using System.Text.RegularExpressions;

namespace SuperAppServices.Services
{
    public class KeywordServiceV2
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KeywordServiceV2> _logger;
        private readonly ITargetKeywordRepository _targetKeywordRepository;

        public KeywordServiceV2(
            ApplicationDbContext context,
            ILogger<KeywordServiceV2> logger,
            ITargetKeywordRepository targetKeywordRepository)
        {
            _context = context;
            _logger = logger;
            _targetKeywordRepository = targetKeywordRepository;
        }

        #region Get Keywords with LongLink

        public async Task<List<KeywordDto>> GetKeywordsAsync(int userId)
        {
            try
            {
                var keywords = await _context.Keywords
                    .Where(k => k.UserId == userId)
                    .ToListAsync();

                return await EnrichWithLongLinksAsync(keywords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting keywords for UserId: {UserId}", userId);
                throw;
            }
        }

        private async Task<List<KeywordDto>> EnrichWithLongLinksAsync(List<Keyword> keywords)
        {
            if (!keywords.Any())
                return new List<KeywordDto>();

            // Only folder/note/file keywords reference WorkspaceItems
            var wsItemTypes = new HashSet<string> { "folder", "note", "file" };

            var itemIds = keywords
                .Where(k => wsItemTypes.Contains(k.Type) && k.TargetItemId.HasValue)
                .Select(k => k.TargetItemId!.Value)
                .Distinct()
                .ToList();

            var items = await _context.Set<WorkspaceItemEntity>()
                .Where(i => itemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.PathIds, i.EntityType, i.EntityId, i.WorkspaceId })
                .ToListAsync();

            var itemMap = items.ToDictionary(
                i => i.Id,
                i => (i.Id, i.PathIds, i.EntityType, i.EntityId, i.WorkspaceId));

            var workspaceIds = items.Where(i => i.EntityType == 1).Select(i => i.EntityId)
                .Concat(items.Select(i => i.WorkspaceId))
                .Distinct().ToList();
            var folderIds = items.Where(i => i.EntityType == 2).Select(i => i.EntityId).ToList();
            var noteIds   = items.Where(i => i.EntityType == 3).Select(i => i.EntityId).ToList();

            var workspaceNames = (await _context.Workspaces
                .Where(w => workspaceIds.Contains(w.Id))
                .Select(w => new { w.Id, w.Name })
                .ToListAsync()).ToDictionary(w => w.Id, w => w.Name);
            var folderData = await _context.Folders
                .Where(f => folderIds.Contains(f.Id))
                .Select(f => new { f.Id, f.Name, f.Color, f.Icon })
                .ToListAsync();
            var folderNames  = folderData.ToDictionary(f => f.Id, f => f.Name);
            var folderColors = folderData.ToDictionary(f => f.Id, f => f.Color);
            var folderIcons  = folderData.ToDictionary(f => f.Id, f => f.Icon);

            var noteData = await _context.Notes
                .Where(n => noteIds.Contains(n.Id))
                .Select(n => new { n.Id, n.Name, n.Icon, n.Color })
                .ToListAsync();
            var noteNames  = noteData.ToDictionary(n => n.Id, n => n.Name);
            var noteIcons  = noteData.ToDictionary(n => n.Id, n => n.Icon);
            var noteColors = noteData.ToDictionary(n => n.Id, n => n.Color);

            var longLinkCache = new Dictionary<string, string>();

            // Load log data (type + trackId) for log keywords
            var logIds = keywords
                .Where(k => k.Type == "log" && k.TargetItemId.HasValue)
                .Select(k => k.TargetItemId!.Value)
                .Distinct().ToList();
            Dictionary<int, string> logTypeMap = new();
            Dictionary<int, int?> logTrackIdMap = new();
            if (logIds.Any())
            {
                var logData = await _context.LifeLogLogs
                    .Where(l => logIds.Contains(l.Id))
                    .Select(l => new { l.Id, l.Type, l.TrackId })
                    .ToListAsync();
                logTypeMap = logData.ToDictionary(l => l.Id, l => l.Type ?? "");
                logTrackIdMap = logData.ToDictionary(l => l.Id, l => (int?)l.TrackId);
            }

            // Collect all track IDs: from track keywords + from track-type log keywords
            var trackIdsFromKeywords = keywords
                .Where(k => k.Type == "track" && k.TargetItemId.HasValue)
                .Select(k => k.TargetItemId!.Value);
            var trackIdsFromLogs = logTrackIdMap.Values
                .Where(id => id.HasValue && id.Value > 0)
                .Select(id => id!.Value);
            var allTrackIds = trackIdsFromKeywords.Concat(trackIdsFromLogs).Distinct().ToList();

            Dictionary<int, string?> trackEmojiMap = new();
            Dictionary<int, string?> trackColorMap = new();
            if (allTrackIds.Any())
            {
                var trackData = await _context.LifeLogTracks
                    .Where(t => allTrackIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.Emoji, t.Color })
                    .ToListAsync();
                trackEmojiMap = trackData.ToDictionary(t => t.Id, t => (string?)t.Emoji);
                trackColorMap = trackData.ToDictionary(t => t.Id, t => (string?)t.Color);
            }

            return keywords.Select(k =>
            {
                int? workspaceItemId = null;
                int? entityId = null;
                string? color = null;
                string? icon = null;

                if (wsItemTypes.Contains(k.Type) &&
                    k.TargetItemId.HasValue &&
                    itemMap.TryGetValue(k.TargetItemId.Value, out var info))
                {
                    workspaceItemId = k.TargetItemId.Value;
                    entityId = info.EntityId;

                    if (info.EntityType == 2)
                    {
                        folderColors.TryGetValue(info.EntityId, out color);
                        folderIcons.TryGetValue(info.EntityId, out icon);
                    }
                    else if (info.EntityType == 3)
                    {
                        noteColors.TryGetValue(info.EntityId, out color);
                        noteIcons.TryGetValue(info.EntityId, out icon);
                    }
                }
                else if (k.Type == "log" && k.TargetItemId.HasValue)
                {
                    if (logTypeMap.TryGetValue(k.TargetItemId.Value, out var logType))
                    {
                        if (logType == "track" &&
                            logTrackIdMap.TryGetValue(k.TargetItemId.Value, out var tid) &&
                            tid.HasValue && tid.Value > 0)
                        {
                            // Track-type log: use the associated track's emoji and color
                            if (trackEmojiMap.TryGetValue(tid.Value, out var trackEmoji))
                                icon = trackEmoji;
                            if (trackColorMap.TryGetValue(tid.Value, out var trackColor))
                                color = trackColor;
                        }
                        else
                        {
                            // Regular log type (event, reflection, lesson, etc.)
                            icon = logType;
                        }
                    }
                }
                else if (k.Type == "track" && k.TargetItemId.HasValue)
                {
                    if (trackEmojiMap.TryGetValue(k.TargetItemId.Value, out var emoji))
                        icon = emoji;
                    if (trackColorMap.TryGetValue(k.TargetItemId.Value, out var trackColor))
                        color = trackColor;
                }

                return new KeywordDto
                {
                    Id = k.Id,
                    Name = k.Name,
                    Type = k.Type,
                    Link = k.Link,
                    LongLink = ComputeLongLink(k, itemMap, workspaceNames, folderNames, noteNames, longLinkCache),
                    Description = k.Description,
                    HardDeletedAt = k.HardDeletedAt,
                    WorkspaceItemId = workspaceItemId,
                    EntityId = entityId,
                    Color = color,
                    Icon = icon,
                };
            }).ToList();
        }

        private string ComputeLongLink(
            Keyword keyword,
            Dictionary<int, (int Id, string PathIds, byte EntityType, int EntityId, int WorkspaceId)> itemMap,
            Dictionary<int, string> workspaceNames,
            Dictionary<int, string> folderNames,
            Dictionary<int, string> noteNames,
            Dictionary<string, string> cache)
        {
            // Types whose LongLink is just the Name
            switch (keyword.Type)
            {
                case "workspace":
                case "external":
                case "project":
                case "task":
                case "log":
                case "track":
                    return keyword.Name;
            }

            // folder / note / file — build from PathIds
            if (!keyword.TargetItemId.HasValue || !itemMap.ContainsKey(keyword.TargetItemId.Value))
                return keyword.Name;

            var item = itemMap[keyword.TargetItemId.Value];
            return BuildLongLinkFromPathIds(item.PathIds, item.WorkspaceId, itemMap, workspaceNames, folderNames, noteNames, cache);
        }

        private string BuildLongLinkFromPathIds(
            string pathIds,
            int workspaceId,
            Dictionary<int, (int Id, string PathIds, byte EntityType, int EntityId, int WorkspaceId)> itemMap,
            Dictionary<int, string> workspaceNames,
            Dictionary<int, string> folderNames,
            Dictionary<int, string> noteNames,
            Dictionary<string, string> cache)
        {
            var cacheKey = $"{workspaceId}:{pathIds}";
            if (cache.TryGetValue(cacheKey, out var cached)) return cached;

            var parts = new List<string>
            {
                workspaceNames.TryGetValue(workspaceId, out var wsName) ? wsName : $"Workspace{workspaceId}"
            };

            var ids = pathIds.Trim('/').Split('/')
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(int.Parse)
                .ToList();

            foreach (var id in ids)
            {
                if (!itemMap.TryGetValue(id, out var item)) { parts.Add($"Unknown{id}"); continue; }

                string name = "Unknown";
                if (item.EntityType == 2 && folderNames.ContainsKey(item.EntityId))
                    name = folderNames[item.EntityId];
                else if (item.EntityType == 3 && noteNames.ContainsKey(item.EntityId))
                    name = noteNames[item.EntityId];

                parts.Add(name);
            }

            var longLink = string.Join("/", parts);
            cache[cacheKey] = longLink;
            return longLink;
        }

        #endregion

        #region Sync Keywords

        public async Task<Keyword> SyncWorkspaceKeywordAsync(int workspaceId, int userId)
        {
            try
            {
                var workspace = await _context.Workspaces.FindAsync(workspaceId);
                if (workspace == null)
                    throw new ArgumentException($"Workspace {workspaceId} not found");

                var link = $"sa/w{workspaceId}";

                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.TargetItemId == workspaceId && k.Type == "workspace");

                if (keyword == null)
                {
                    keyword = new Keyword(
                        name: workspace.Name,
                        link: link,
                        type: "workspace",
                        userId: userId,
                        targetItemId: workspaceId);
                    _context.Keywords.Add(keyword);
                }
                else
                {
                    keyword.Update(
                        name: workspace.Name,
                        link: link,
                        type: "workspace",
                        description: workspace.Description,
                        targetItemId: workspaceId);
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

                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.TargetItemId == workspaceItemId);

                if (keyword == null)
                {
                    keyword = new Keyword(
                        name: folder.Name,
                        link: link,
                        type: "folder",
                        userId: userId,
                        targetItemId: workspaceItemId);
                    _context.Keywords.Add(keyword);
                }
                else
                {
                    keyword.Update(
                        name: folder.Name,
                        link: link,
                        type: "folder",
                        targetItemId: workspaceItemId);
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

                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.TargetItemId == workspaceItemId);

                if (keyword == null)
                {
                    var existingWithSameLink = await _context.Keywords.FirstOrDefaultAsync(k => k.Link == link);
                    if (existingWithSameLink != null)
                    {
                        _logger.LogError("DUPLICATE LINK DETECTED! Link '{Link}' already exists for keyword ID {KeywordId}. Current item: {CurrentItemId}",
                            link, existingWithSameLink.Id, workspaceItemId);
                        throw new InvalidOperationException($"Duplicate keyword link detected: {link}");
                    }

                    keyword = new Keyword(
                        name: note.Name,
                        link: link,
                        type: "note",
                        userId: userId,
                        targetItemId: workspaceItemId);
                    _context.Keywords.Add(keyword);
                }
                else
                {
                    _logger.LogInformation("Updating keyword ID {KeywordId} for note '{NoteName}' with link '{Link}'", keyword.Id, note.Name, link);
                    keyword.Update(
                        name: note.Name,
                        link: link,
                        type: "note",
                        targetItemId: workspaceItemId);
                }

                await _context.SaveChangesAsync();
                return keyword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing note keyword for WorkspaceItemId: {WorkspaceItemId}", workspaceItemId);
                throw;
            }
        }

        public async Task<Keyword> SyncProjectKeywordAsync(int projectId, int userId)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                    throw new ArgumentException($"Project {projectId} not found");

                var link = $"sa/p{projectId}";

                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.TargetItemId == projectId && k.Type == "project");

                if (keyword == null)
                {
                    keyword = new Keyword(
                        name: project.Name,
                        link: link,
                        type: "project",
                        userId: userId,
                        targetItemId: projectId);
                    _context.Keywords.Add(keyword);
                }
                else
                {
                    keyword.Update(
                        name: project.Name,
                        link: link,
                        type: "project",
                        targetItemId: projectId);
                }

                await _context.SaveChangesAsync();
                return keyword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing project keyword for ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<Keyword> SyncTaskKeywordAsync(int taskId, int userId)
        {
            try
            {
                var task = await _context.ProTasks.FindAsync(taskId);
                if (task == null)
                    throw new ArgumentException($"Task {taskId} not found");

                var link = $"sa/p{task.ProjectId}/t{taskId}";

                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.TargetItemId == taskId && k.Type == "task");

                if (keyword == null)
                {
                    keyword = new Keyword(
                        name: task.Title,
                        link: link,
                        type: "task",
                        userId: userId,
                        targetItemId: taskId);
                    _context.Keywords.Add(keyword);
                }
                else
                {
                    keyword.Update(
                        name: task.Title,
                        link: link,
                        type: "task",
                        targetItemId: taskId);
                }

                await _context.SaveChangesAsync();
                return keyword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing task keyword for TaskId: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<Keyword> SyncLogKeywordAsync(int logId, int userId)
        {
            try
            {
                var log = await _context.LifeLogLogs.FindAsync(logId);
                if (log == null)
                    throw new ArgumentException($"Log {logId} not found");

                var name = log.Title ?? $"Log {logId}";
                var link = $"sa/l{logId}";

                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.TargetItemId == logId && k.Type == "log");

                if (keyword == null)
                {
                    keyword = new Keyword(
                        name: name,
                        link: link,
                        type: "log",
                        userId: userId,
                        targetItemId: logId);
                    _context.Keywords.Add(keyword);
                }
                else
                {
                    keyword.Update(name: name, link: link, type: "log", targetItemId: logId);
                }

                await _context.SaveChangesAsync();
                return keyword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing log keyword for LogId: {LogId}", logId);
                throw;
            }
        }

        public async Task<Keyword> SyncTrackKeywordAsync(int trackId, int userId)
        {
            try
            {
                var track = await _context.LifeLogTracks.FindAsync(trackId);
                if (track == null)
                    throw new ArgumentException($"Track {trackId} not found");

                var link = $"sa/tr{trackId}";

                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.TargetItemId == trackId && k.Type == "track");

                if (keyword == null)
                {
                    keyword = new Keyword(
                        name: track.Name,
                        link: link,
                        type: "track",
                        userId: userId,
                        targetItemId: trackId);
                    _context.Keywords.Add(keyword);
                }
                else
                {
                    keyword.Update(name: track.Name, link: link, type: "track", targetItemId: trackId);
                }

                await _context.SaveChangesAsync();
                return keyword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing track keyword for TrackId: {TrackId}", trackId);
                throw;
            }
        }

        #endregion

        #region External Keywords

        public async Task<ResultOptions> UpsertExternalKeywordsAsync(int userId, List<UpsertExternalKeywordRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                    return new ResultOptions { Success = false, Message = "No external keywords provided", Status = 400 };

                var successCount = 0;
                var failCount = 0;
                var errors = new List<string>();
                var results = new List<KeywordDto>();

                foreach (var request in requests)
                {
                    try
                    {
                        Keyword keyword;

                        if (request.Id.HasValue && request.Id.Value > 0)
                        {
                            keyword = await _context.Keywords.FindAsync(request.Id.Value);
                            if (keyword == null)
                            {
                                errors.Add($"Keyword ID {request.Id} not found");
                                failCount++;
                                continue;
                            }

                            keyword.Update(name: request.Name, link: request.Link, type: "external");
                            if (!string.IsNullOrEmpty(request.Description))
                                keyword.Description = request.Description;
                        }
                        else
                        {
                            keyword = new Keyword(
                                name: request.Name,
                                link: request.Link,
                                type: "external",
                                userId: userId);

                            if (!string.IsNullOrEmpty(request.Description))
                                keyword.Description = request.Description;

                            _context.Keywords.Add(keyword);
                        }

                        await _context.SaveChangesAsync();

                        results.Add(new KeywordDto
                        {
                            Id = keyword.Id,
                            Name = keyword.Name,
                            Type = keyword.Type,
                            Link = keyword.Link,
                            LongLink = keyword.Name,
                            Description = keyword.Description
                        });
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error upserting external keyword: {Name}", request.Name);
                        errors.Add($"Keyword '{request.Name}': {ex.Message}");
                        failCount++;
                    }
                }

                var message = successCount > 0
                    ? $"Successfully upserted {successCount}/{requests.Count} external keywords"
                    : "Failed to upsert all external keywords";
                if (failCount > 0) message += $". {failCount} failed.";

                return new ResultOptions
                {
                    Success = successCount > 0,
                    Message = message,
                    Object = new { SuccessCount = successCount, FailCount = failCount, Errors = errors, Keywords = results },
                    Status = successCount > 0 ? 200 : 400
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch upsert external keywords");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<string> ProcessExternalLinksInDescriptionAsync(string description, int userId)
        {
            try
            {
                if (string.IsNullOrEmpty(description)) return description;

                var pattern = new Regex(@"\(\(([^\|\)]+)\|([^\)]+)\)\)");
                var matches = pattern.Matches(description);
                if (matches.Count == 0) return description;

                _logger.LogInformation("Found {Count} external links in description", matches.Count);

                var updated = description;

                foreach (Match match in matches)
                {
                    var originalText = match.Value;
                    var name = match.Groups[1].Value.Trim();
                    var url  = match.Groups[2].Value.Trim();

                    try
                    {
                        // Dedup by Link (URL stored directly in Link for external keywords)
                        var existing = await _context.Keywords
                            .FirstOrDefaultAsync(k =>
                                k.Link == url &&
                                k.UserId == userId &&
                                k.Type == "external" &&
                                k.HardDeletedAt == null);

                        Keyword keyword;

                        if (existing != null)
                        {
                            keyword = existing;
                            _logger.LogInformation("Reusing existing external keyword ID {KeywordId} for URL: {Url}", keyword.Id, url);
                        }
                        else
                        {
                            keyword = new Keyword(name: name, link: url, type: "external", userId: userId);
                            _context.Keywords.Add(keyword);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Created external keyword ID {KeywordId} for '{Name}' → {Url}", keyword.Id, name, url);
                        }

                        updated = updated.Replace(originalText, $"[[{keyword.Id}]]");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing external link: {OriginalText}", originalText);
                    }
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing external links for UserId: {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region Move Operations

        /// <summary>
        /// Rebuild keyword links after workspace items are moved.
        /// Must be called AFTER WorkspaceItems.PathIds have been updated.
        /// </summary>
        public async Task RebuildLinksAfterMoveAsync(string oldPathIdsPrefix, string newPathIdsPrefix)
        {
            try
            {
                // Find workspace items now at the new path (already updated by caller)
                var movedItems = await _context.Set<WorkspaceItemEntity>()
                    .Where(i => i.PathIds != null && i.PathIds.StartsWith(newPathIdsPrefix))
                    .Select(i => new { i.Id, i.WorkspaceId, i.PathIds })
                    .ToListAsync();

                if (!movedItems.Any())
                {
                    _logger.LogInformation("No workspace items found at new path prefix: {Prefix}", newPathIdsPrefix);
                    return;
                }

                var movedItemIds = movedItems.Select(i => i.Id).ToList();
                var itemInfoMap = movedItems.ToDictionary(i => i.Id, i => (i.WorkspaceId, i.PathIds));

                var keywords = await _context.Keywords
                    .Where(k => k.TargetItemId.HasValue && movedItemIds.Contains(k.TargetItemId.Value))
                    .ToListAsync();

                _logger.LogInformation("Rebuilding {Count} keywords after move to '{NewPrefix}'", keywords.Count, newPathIdsPrefix);

                foreach (var keyword in keywords)
                {
                    if (!keyword.TargetItemId.HasValue) continue;
                    var (workspaceId, pathIds) = itemInfoMap[keyword.TargetItemId.Value];
                    keyword.Link = await BuildItemLinkAsync(pathIds, workspaceId, keyword.TargetItemId.Value);
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

        public async Task DeleteWorkspaceKeywordAsync(int workspaceId)
        {
            try
            {
                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.TargetItemId == workspaceId && k.Type == "workspace");

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

        //public async Task DeleteKeywordsByPathAsync(string pathIds)
        //{
        //    try
        //    {
        //        var itemIds = await _context.Set<WorkspaceItemEntity>()
        //            .Where(i => i.PathIds != null && i.PathIds.StartsWith(pathIds))
        //            .Select(i => i.Id)
        //            .ToListAsync();

        //        if (!itemIds.Any()) return;

        //        var keywords = await _context.Keywords
        //            .Where(k => k.TargetItemId.HasValue && itemIds.Contains(k.TargetItemId.Value))
        //            .ToListAsync();

        //        _context.Keywords.RemoveRange(keywords);
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error deleting keywords by path: {PathIds}", pathIds);
        //        throw;
        //    }
        //}

        public async Task HardDeleteNoteKeywordsAsync(List<int> noteIds)
        {
            try
            {
                if (noteIds == null || !noteIds.Any()) return;

                _logger.LogInformation("Hard deleting keywords for {Count} notes", noteIds.Count);

                var workspaceItemIds = await _context.WorkspaceItems
                    .Where(wi => wi.EntityType == 3 && noteIds.Contains(wi.EntityId))
                    .Select(wi => wi.Id)
                    .ToListAsync();

                if (!workspaceItemIds.Any())
                {
                    _logger.LogInformation("No workspace_items found for notes, skipping keyword hard delete");
                    return;
                }

                var keywords = await _context.Keywords
                    .Where(k => k.TargetItemId.HasValue &&
                                workspaceItemIds.Contains(k.TargetItemId.Value) &&
                                k.HardDeletedAt == null)
                    .ToListAsync();

                if (!keywords.Any())
                {
                    _logger.LogInformation("No keywords found for notes, skipping hard delete");
                    return;
                }

                var now = DateTime.UtcNow;
                foreach (var keyword in keywords)
                {
                    keyword.HardDeletedAt = now;
                    keyword.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Hard deleted {Count} keywords for {NoteCount} notes", keywords.Count, noteIds.Count);
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
        /// Build link from WorkspaceItem PathIds.
        /// Format: sa/w{workspaceId}/f{itemId}/n{itemId}
        /// PathIds already includes the current item as the last segment.
        /// </summary>
        private async Task<string> BuildItemLinkAsync(string pathIds, int workspaceId, int currentItemId)
        {
            if (string.IsNullOrEmpty(pathIds) || pathIds == "/")
            {
                _logger.LogWarning("BuildItemLinkAsync called with empty PathIds for item {ItemId}", currentItemId);
                var fallback = await _context.Set<WorkspaceItemEntity>().FindAsync(currentItemId);
                var fp = fallback?.EntityType switch { 2 => "f", 3 => "n", 4 => "file", _ => "?" };
                return $"sa/w{workspaceId}/{fp}{currentItemId}";
            }

            var ids = pathIds.Trim('/').Split('/')
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(int.Parse)
                .ToList();

            if (!ids.Any())
            {
                _logger.LogWarning("BuildItemLinkAsync: No valid IDs in PathIds '{PathIds}' for item {ItemId}", pathIds, currentItemId);
                var fallback = await _context.Set<WorkspaceItemEntity>().FindAsync(currentItemId);
                var fp = fallback?.EntityType switch { 2 => "f", 3 => "n", 4 => "file", _ => "?" };
                return $"sa/w{workspaceId}/{fp}{currentItemId}";
            }

            var dbItems = await _context.Set<WorkspaceItemEntity>()
                .Where(i => ids.Contains(i.Id))
                .Select(i => new { i.Id, i.EntityType })
                .ToListAsync();

            var entityTypes = dbItems.ToDictionary(i => i.Id, i => i.EntityType);
            var parts = new List<string> { $"sa/w{workspaceId}" };

            foreach (var id in ids)
            {
                if (!entityTypes.TryGetValue(id, out var et)) { parts.Add($"?{id}"); continue; }
                var prefix = et switch { 2 => "f", 3 => "n", 4 => "file", _ => "?" };
                parts.Add($"{prefix}{id}");
            }

            return string.Join("/", parts);
        }

        #endregion

        #region TargetKeywords

        public async Task<ResultOptions> GetTargetKeywordsAsync(int targetId, string targetType)
            => await _targetKeywordRepository.GetByTargetAsync(targetId, targetType);

        public async Task<ResultOptions> LinkTargetKeywordAsync(LinkTargetKeywordRequest request)
        {
            var item = new TargetKeyword
            {
                TargetId = request.TargetId,
                TargetType = request.TargetType,
                KeywordId = request.KeywordId
            };
            return await _targetKeywordRepository.CreateAsync(item);
        }

        public async Task<ResultOptions> UnlinkTargetKeywordAsync(int id)
            => await _targetKeywordRepository.DeleteAsync(id);

        #endregion

        #region Full Sync (compare + create missing)

        public async Task<KeywordSyncReportDto> SyncKeywordsAsync(int userId)
        {
            // 1. Load all existing keywords for the user
            var existingKeywords = await _context.Keywords
                .Where(k => k.UserId == userId)
                .ToListAsync();

            // Index by (type, targetItemId) for exact-match lookup.
            // UQ_Keywords_Link is unique on Link — we also build a link-index
            // to skip creating a keyword when the expected link already exists.
            var kwIndex = existingKeywords
                .Where(k => k.TargetItemId.HasValue)
                .GroupBy(k => (k.Type, k.TargetItemId!.Value))
                .ToDictionary(g => g.Key, g => g.First());

            // Secondary index: existing links → skip insert if link already taken
            var existingLinks = existingKeywords
                .Select(k => k.Link)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var updates = new List<KeywordSyncItemDto>();
            var created = new List<KeywordSyncItemDto>();
            var newKws  = new List<Keyword>();

            // 2. Load source entities
            var workspaces = await _context.Workspaces
                .Where(w => w.UserId == userId && w.DeletedAt == null)
                .Select(w => new { w.Id, w.Name })
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => p.UserId == userId && p.DeletedAt == null)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            // ProTask has no UserId — filter via project IDs owned by this user
            var userProjectIds = projects.Select(p => p.Id).ToList();
            var tasks = userProjectIds.Any()
                ? await _context.ProTasks
                    .Where(t => userProjectIds.Contains(t.ProjectId) && t.DeletedAt == null)
                    .Select(t => new { t.Id, t.Title, t.ProjectId })
                    .ToListAsync()
                : new List<(int Id, string Title, int ProjectId)>()
                    .Select(x => new { x.Id, x.Title, x.ProjectId }).ToList();

            // Load logs with description
            var logs = await _context.LifeLogLogs
                .Where(l => l.UserId == userId && l.DeletedAt == null)
                .Select(l => new { l.Id, l.Title, l.Description })
                .ToListAsync();

            // Load tracks with description
            var tracks = await _context.LifeLogTracks
                .Where(t => t.UserId == userId && t.DeletedAt == null)
                .Select(t => new { t.Id, t.Name, t.Description })
                .ToListAsync();

            // WorkspaceItemEntity has no UserId — filter via workspace IDs owned by this user
            var userWorkspaceIds = workspaces.Select(w => w.Id).ToList();
            var wsItems = userWorkspaceIds.Any()
                ? await _context.Set<WorkspaceItemEntity>()
                    .Where(i => userWorkspaceIds.Contains(i.WorkspaceId) && i.DeletedAt == null &&
                                (i.EntityType == 2 || i.EntityType == 3 || i.EntityType == 4))
                    .Select(i => new { i.Id, i.EntityType, i.EntityId, i.WorkspaceId, i.PathIds })
                    .ToListAsync()
                : new List<(int Id, byte EntityType, int EntityId, int WorkspaceId, string PathIds)>()
                    .Select(x => new { x.Id, x.EntityType, x.EntityId, x.WorkspaceId, x.PathIds }).ToList();

            // Build entity-type map for link building
            var allWsItemIds = wsItems.Select(i => i.Id).Distinct().ToList();
            var pathItemIds  = wsItems.SelectMany(i => SyncParsePathIds(i.PathIds)).Distinct().ToList();
            var allItemIds   = allWsItemIds.Concat(pathItemIds).Distinct().ToList();
            var entityTypeMap = (await _context.Set<WorkspaceItemEntity>()
                .Where(i => allItemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.EntityType })
                .ToListAsync())
                .ToDictionary(i => i.Id, i => i.EntityType);

            var folderEntityIds = wsItems.Where(i => i.EntityType == 2).Select(i => i.EntityId).Distinct().ToList();
            var noteEntityIds   = wsItems.Where(i => i.EntityType == 3).Select(i => i.EntityId).Distinct().ToList();

            var folderNameMap = folderEntityIds.Any()
                ? (await _context.Folders.Where(f => folderEntityIds.Contains(f.Id))
                    .Select(f => new { f.Id, f.Name }).ToListAsync())
                    .ToDictionary(f => f.Id, f => f.Name)
                : new Dictionary<int, string>();

            var noteNameMap = noteEntityIds.Any()
                ? (await _context.Notes.Where(n => noteEntityIds.Contains(n.Id))
                    .Select(n => new { n.Id, n.Name }).ToListAsync())
                    .ToDictionary(n => n.Id, n => n.Name)
                : new Dictionary<int, string>();

            // 3. Compare + queue changes
            foreach (var ws in workspaces)
                ProcessKeyword(kwIndex, existingLinks, updates, created, newKws, "workspace", ws.Id, userId, ws.Name, $"sa/w{ws.Id}");

            foreach (var p in projects)
                ProcessKeyword(kwIndex, existingLinks, updates, created, newKws, "project", p.Id, userId, p.Name, $"sa/p{p.Id}");

            foreach (var t in tasks)
                ProcessKeyword(kwIndex, existingLinks, updates, created, newKws, "task", t.Id, userId, t.Title ?? $"Task {t.Id}", $"sa/p{t.ProjectId}/t{t.Id}");

            foreach (var l in logs)
                ProcessKeyword(kwIndex, existingLinks, updates, created, newKws, "log", l.Id, userId, l.Title ?? $"Log {l.Id}", $"sa/l{l.Id}", l.Description);

            foreach (var t in tracks)
                ProcessKeyword(kwIndex, existingLinks, updates, created, newKws, "track", t.Id, userId, t.Name, $"sa/tr{t.Id}", t.Description);

            foreach (var item in wsItems)
            {
                string entityName = item.EntityType switch
                {
                    2 => folderNameMap.TryGetValue(item.EntityId, out var fn) ? fn : $"Folder{item.EntityId}",
                    3 => noteNameMap.TryGetValue(item.EntityId, out var nn)   ? nn : $"Note{item.EntityId}",
                    _ => $"File{item.EntityId}",
                };
                string kwType       = item.EntityType switch { 2 => "folder", 3 => "note", _ => "file" };
                string expectedLink = SyncBuildLink(item.PathIds, item.WorkspaceId, item.Id, entityTypeMap);
                ProcessKeyword(kwIndex, existingLinks, updates, created, newKws, kwType, item.Id, userId, entityName, expectedLink);
            }

            // 4. Persist
            if (newKws.Any())
                _context.Keywords.AddRange(newKws);

            if (updates.Any() || newKws.Any())
                await _context.SaveChangesAsync();

            for (int i = 0; i < newKws.Count; i++)
                created[i].Id = newKws[i].Id;

            // 5. Build report
            var allKeywords = await _context.Keywords
                .Where(k => k.UserId == userId)
                .ToListAsync();

            return new KeywordSyncReportDto
            {
                TotalKeywords     = allKeywords.Count,
                CountByType       = allKeywords
                    .GroupBy(k => k.Type)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count()),
                HardDeletedCount  = allKeywords.Count(k => k.HardDeletedAt != null),
                NameMismatchCount = updates.Count(u => u.NameChanged),
                LinkMismatchCount = updates.Count(u => u.LinkChanged),
                UpdatedCount      = updates.Count,
                CreatedCount      = created.Count,
                Updates           = updates,
                Created           = created,
            };
        }

        private static void ProcessKeyword(
            Dictionary<(string Type, int TargetItemId), Keyword> kwIndex,
            HashSet<string> existingLinks,
            List<KeywordSyncItemDto> updates,
            List<KeywordSyncItemDto> created,
            List<Keyword> newKws,
            string type, int targetItemId, int userId,
            string expectedName, string expectedLink,
            string? description = null)
        {
            if (kwIndex.TryGetValue((type, targetItemId), out var kw))
            {
                bool nameChanged = kw.Name != expectedName;
                bool linkChanged = kw.Link != expectedLink;
                if (!nameChanged && !linkChanged) return;

                // If link would change but the new link is already taken, skip link update
                if (linkChanged && existingLinks.Contains(expectedLink))
                    linkChanged = false;

                if (!nameChanged && !linkChanged) return;

                updates.Add(new KeywordSyncItemDto
                {
                    Id          = kw.Id,
                    Type        = type,
                    OldName     = kw.Name,
                    NewName     = nameChanged ? expectedName : kw.Name,
                    OldLink     = kw.Link,
                    NewLink     = linkChanged ? expectedLink : kw.Link,
                    NameChanged = nameChanged,
                    LinkChanged = linkChanged,
                });

                if (nameChanged) kw.Name = expectedName;
                if (linkChanged)
                {
                    existingLinks.Remove(kw.Link);
                    existingLinks.Add(expectedLink);
                    kw.Link = expectedLink;
                }
                kw.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Skip if the expected link is already taken by another keyword
                if (existingLinks.Contains(expectedLink)) return;

                var newKw = new Keyword(
                    name: expectedName,
                    link: expectedLink,
                    type: type,
                    userId: userId,
                    targetItemId: targetItemId);
                if (description != null) newKw.Description = description;
                newKws.Add(newKw);
                existingLinks.Add(expectedLink); // reserve immediately for subsequent iterations

                created.Add(new KeywordSyncItemDto
                {
                    Id          = 0,
                    Type        = type,
                    NewName     = expectedName,
                    NewLink     = expectedLink,
                    Description = description,
                    NameChanged = false,
                    LinkChanged = false,
                });
            }
        }

        private static List<int> SyncParsePathIds(string? pathIds)
        {
            if (string.IsNullOrEmpty(pathIds) || pathIds == "/") return new List<int>();
            return pathIds.Trim('/').Split('/')
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => int.TryParse(s, out var id) ? id : -1)
                .Where(id => id > 0)
                .ToList();
        }

        private static string SyncBuildLink(string? pathIds, int workspaceId, int currentItemId, Dictionary<int, byte> entityTypeMap)
        {
            var ids = SyncParsePathIds(pathIds);
            if (!ids.Any())
            {
                entityTypeMap.TryGetValue(currentItemId, out var fallbackEt);
                var fp = fallbackEt switch { 2 => "f", 3 => "n", 4 => "file", _ => "?" };
                return $"sa/w{workspaceId}/{fp}{currentItemId}";
            }

            var parts = new List<string> { $"sa/w{workspaceId}" };
            foreach (var id in ids)
            {
                if (!entityTypeMap.TryGetValue(id, out var et)) { parts.Add($"?{id}"); continue; }
                var prefix = et switch { 2 => "f", 3 => "n", 4 => "file", _ => "?" };
                parts.Add($"{prefix}{id}");
            }
            return string.Join("/", parts);
        }

        #endregion
    }
}
