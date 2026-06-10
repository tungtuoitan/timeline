using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SuperAppServices.Services.K
{
    public class KRepoSyncService : IKRepoSyncService
    {
        private readonly IUserProfileRepository _profileRepo;
        private readonly IKKnowledgeRepository _knowledgeRepo;
        private readonly IKQuestionRepository _questionRepo;
        private readonly IKNodeHelperService _nodeHelper;
        private readonly IKSyncNotifier _notifier;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<KRepoSyncService> _logger;
        private readonly string _reposBasePath;

        public KRepoSyncService(
            IUserProfileRepository profileRepo,
            IKKnowledgeRepository knowledgeRepo,
            IKQuestionRepository questionRepo,
            IKNodeHelperService nodeHelper,
            IKSyncNotifier notifier,
            ApplicationDbContext db,
            IConfiguration config,
            ILogger<KRepoSyncService> logger)
        {
            _profileRepo   = profileRepo   ?? throw new ArgumentNullException(nameof(profileRepo));
            _knowledgeRepo = knowledgeRepo ?? throw new ArgumentNullException(nameof(knowledgeRepo));
            _questionRepo  = questionRepo  ?? throw new ArgumentNullException(nameof(questionRepo));
            _nodeHelper    = nodeHelper    ?? throw new ArgumentNullException(nameof(nodeHelper));
            _notifier      = notifier      ?? throw new ArgumentNullException(nameof(notifier));
            _db            = db            ?? throw new ArgumentNullException(nameof(db));
            _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
            _reposBasePath = config["KRepoSync:LocalReposBasePath"] ?? Path.Combine(AppContext.BaseDirectory, "k-repos");
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public async Task<KRepoSyncStatusResponse> GetStatusAsync(int userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (profile == null)
                return new KRepoSyncStatusResponse { StatusCode = "idle", IsConfigured = false };

            return new KRepoSyncStatusResponse
            {
                RepoUrl       = profile.KRepoUrl,
                Branch        = profile.KRepoBranch ?? "main",
                StatusCode    = profile.KRepoStatusCode ?? "idle",
                LastPushAt    = profile.KRepoLastPushAt,
                LastCheckAt   = profile.KRepoLastCheckAt,
                IsConfigured  = !string.IsNullOrEmpty(profile.KRepoUrl),
            };
        }

        public async Task<ResultOptions> SaveConfigAsync(int userId, string repoUrl, string branch, string pat)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId)
                ?? new UserProfile(userId);

            profile.KRepoUrl        = repoUrl;
            profile.KRepoBranch     = branch;
            profile.KRepoPat        = pat;
            profile.KRepoStatusCode = "idle";

            await _profileRepo.UpsertUserProfileAsync(profile);
            return new ResultOptions { Success = true };
        }

        public async Task<ResultOptions> PushToRepoAsync(int userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (string.IsNullOrEmpty(profile?.KRepoUrl))
                return ResultOptions.Fail("No repo configured. Set up repo URL in Settings.", 400);

            // Skip push if status is conflict — user must resolve manually
            if (profile.KRepoStatusCode == "conflict")
                return ResultOptions.Fail("Sync paused due to conflict. Resolve in git and click Retry.", 409);

            await PushStatusAsync(userId, "pushing", "Pushing DB → repo...", "push");
            try
            {
                // Explicit push = force DB → repo even when the content hash is unchanged
                // (the remote may have been emptied/edited out-of-band; restore it from DB).
                var result = await DoPushAsync(profile, force: true);
                if (!result.Success)
                {
                    await PushStatusAsync(userId, result.Status == 409 ? "conflict" : "error", result.Message, "push");
                    await SaveStatusAsync(profile, result.Status == 409 ? "conflict" : "error");
                }
                else
                {
                    await PushStatusAsync(userId, "synced", null, "");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PushToRepo failed for user {UserId}", userId);
                await PushStatusAsync(userId, "error", ex.Message, "push");
                await SaveStatusAsync(profile, "error");
                return ResultOptions.Fail("Push failed: " + ex.Message, 500);
            }
        }

        public async Task<ResultOptions> PullFromRepoAsync(int userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (string.IsNullOrEmpty(profile?.KRepoUrl))
                return ResultOptions.Fail("No repo configured.", 400);

            if (profile.KRepoStatusCode == "conflict")
                return ResultOptions.Fail("Sync paused due to conflict. Resolve in git and click Retry.", 409);

            // NOTE: DoPushAsync (push DB → repo first) is intentionally disabled.
            // Manual workflow: user edits repo directly and pulls → DB wins repo content.
            // Re-enable when daemon/auto-sync is needed.
            // await PushStatusAsync(userId, "pushing", "Pushing DB → repo before pull...", "push");
            // var pushResult = await DoPushAsync(profile);
            // if (!pushResult.Success)
            // {
            //     var isConflict = pushResult.Status == 409;
            //     await PushStatusAsync(userId, isConflict ? "conflict" : "error", pushResult.Message, "push");
            //     await SaveStatusAsync(profile, isConflict ? "conflict" : "error");
            //     return pushResult;
            // }

            // Parse remote changes and apply to DB
            await PushStatusAsync(userId, "pulling", "Pulling repo → DB...", "pull");
            try
            {
                var result = await DoApplyRemoteChangesAsync(profile, userId);
                if (!result.Success)
                {
                    await PushStatusAsync(userId, "error", result.Message, "pull");
                    await SaveStatusAsync(profile, "error");
                }
                else
                {
                    await PushStatusAsync(userId, "synced", null, "");
                    await SaveStatusAsync(profile, "synced");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PullFromRepo failed for user {UserId}", userId);
                await PushStatusAsync(userId, "error", ex.Message, "pull");
                await SaveStatusAsync(profile, "error");
                return ResultOptions.Fail("Pull failed: " + ex.Message, 500);
            }
        }

        public async Task<KRepoSyncDiffResponse> GetDiffAsync(int userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (string.IsNullOrEmpty(profile?.KRepoUrl) || string.IsNullOrEmpty(profile.KRepoLastPushSha))
                return new KRepoSyncDiffResponse();

            var localPath = GetLocalPath(userId);
            if (!LibGit2Sharp.Repository.IsValid(localPath))
                return new KRepoSyncDiffResponse();

            try
            {
                using var repo = new LibGit2Sharp.Repository(localPath);
                var oldCommit = repo.Lookup<Commit>(profile.KRepoLastPushSha);
                var headCommit = repo.Head.Tip;
                if (oldCommit == null || headCommit == null || oldCommit.Sha == headCommit.Sha)
                    return new KRepoSyncDiffResponse();

                var diff = repo.Diff.Compare<Patch>(oldCommit.Tree, headCommit.Tree);
                var items = new List<KRepoSyncDiffItem>();
                var knowledges = await _knowledgeRepo.GetAllKnowledgesByUserIdAsync(userId);

                foreach (var change in diff)
                {
                    if (!change.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;

                    // Node = the folder containing _.md; its name is that folder's name.
                    var pathSegs = change.Path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var nodeName = pathSegs.Length >= 2 ? pathSegs[^2] : change.Path;
                    var oldContent = change.OldMode == Mode.Nonexistent ? "" : GetBlobContent(repo, oldCommit, change.OldPath);
                    var newContent = change.Mode == Mode.Nonexistent ? "" : GetBlobContent(repo, headCommit, change.Path);

                    var (newId, _, newBody) = ParseFrontMatter(newContent);
                    var (oldId, _, oldBody) = ParseFrontMatter(oldContent);
                    var oldQuestions = ParseRepoMarkdownQuestions(oldBody);
                    var newQuestions = ParseRepoMarkdownQuestions(newBody);
                    var nodeId = newId ?? oldId ?? 0;

                    // Build diff per question
                    var allIds = oldQuestions.Keys.Union(newQuestions.Keys).ToHashSet();
                    foreach (var qId in allIds)
                    {
                        oldQuestions.TryGetValue(qId, out var oldText);
                        newQuestions.TryGetValue(qId, out var newText);
                        var changeType = oldText == null ? "added" : newText == null ? "removed" : "modified";
                        if (changeType == "modified" && oldText == newText) continue;

                        items.Add(new KRepoSyncDiffItem
                        {
                            NodeId     = nodeId,
                            NodeName   = nodeName,
                            QuestionId = qId > 0 ? qId : null,
                            OldText    = oldText,
                            NewText    = newText,
                            ChangeType = changeType,
                        });
                    }
                }

                return new KRepoSyncDiffResponse { Items = items };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDiff failed for user {UserId}", userId);
                return new KRepoSyncDiffResponse();
            }
        }

        /// <summary>
        /// Compares current remote repo state against DB — returns a structured diff
        /// without modifying anything. Fetches remote first to get latest.
        /// </summary>
        public async Task<KRepoCompareDiffResponse> GetCompareDiffAsync(int userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (string.IsNullOrEmpty(profile?.KRepoUrl) || string.IsNullOrEmpty(profile.KRepoPat))
                return new KRepoCompareDiffResponse();

            var localPath = GetLocalPath(userId);
            EnsureCloned(localPath, profile.KRepoUrl, profile.KRepoPat, profile.KRepoBranch ?? "main");

            var entries = new List<KRepoCompareEntry>();

            try
            {
                using var repo   = new LibGit2Sharp.Repository(localPath);
                var creds        = BuildCredentials(profile.KRepoPat!);
                var branch       = profile.KRepoBranch ?? "main";

                // Fetch to get latest remote state
                Remote remote = repo.Network.Remotes["origin"];
                repo.Network.Fetch(remote.Name, remote.FetchRefSpecs.Select(r => r.Specification),
                    new FetchOptions { CredentialsProvider = creds });

                var remoteBranch = repo.Branches[$"refs/remotes/origin/{branch}"];
                if (remoteBranch == null)
                    return new KRepoCompareDiffResponse { Error = "Remote branch not found" };

                // ── 1. Read repo files ────────────────────────────────────────────
                var mdFiles = new Dictionary<string, string>(StringComparer.Ordinal);
                WalkMdFiles(remoteBranch.Tip.Tree, "", mdFiles);

                var knowledgeFolders = new List<RepoKnowledgeFolder>();
                var nodeFolders      = new List<RepoNodeFolder>();
                foreach (var (path, content) in mdFiles)
                {
                    var logical = KRepoSyncPlanner.LogicalPath(path);
                    if (logical == null) continue;
                    var (id, _, body) = ParseFrontMatter(content);
                    if (logical.Length == 2)
                        knowledgeFolders.Add(new RepoKnowledgeFolder(logical[1], id, logical[1]));
                    else if (logical.Length >= 3)
                        nodeFolders.Add(new RepoNodeFolder(string.Join("/", logical), id, logical[^1], ParseQuestions(body)));
                }

                // ── 2. DB snapshot ────────────────────────────────────────────────
                var dbKnowledges = await _db.KKnowledges
                    .Where(k => k.UserId == userId && k.DeletedAt == null).ToListAsync();
                var dbKnowledgeMap = dbKnowledges.ToDictionary(k => k.Id);
                var userKnowledgeIds = dbKnowledges.Select(k => k.Id).ToList();

                var dbNodes = await _db.KNodes
                    .Where(n => userKnowledgeIds.Contains(n.KnowledgeId) && n.DeletedAt == null).ToListAsync();
                var dbNodeMap = dbNodes.ToDictionary(n => n.Id);
                var dbNodeIds = dbNodes.Select(n => n.Id).ToList();

                var dbQuestions = await _db.KQuestions
                    .Where(q => q.NodeId != null && dbNodeIds.Contains(q.NodeId.Value) && q.DeletedAt == null)
                    .ToListAsync();

                // ── 3. Plan (pure comparison) ─────────────────────────────────────
                var plan = KRepoSyncPlanner.Plan(
                    knowledgeFolders,
                    nodeFolders,
                    dbKnowledges.Select(k => new DbKnowledgeRef(k.Id, k.Name)).ToList(),
                    dbNodes.Select(n => new DbNodeRef(n.Id, n.KnowledgeId, n.ParentId, n.Name)).ToList(),
                    dbQuestions.Select(q => new DbQuestionRef(q.Id, q.NodeId!.Value, q.Name, q.Description, q.StatusCode == "draft", q.SortOrder)).ToList());

                // ── 4. Translate plan → compare entries ───────────────────────────

                // Knowledges only in repo (will be created on Push to DB)
                foreach (var pk in plan.Knowledges.Where(k => k.ExistingId == null))
                    entries.Add(new KRepoCompareEntry
                    {
                        EntityType  = "knowledge",
                        ChangeType  = "repo_only",
                        Name        = pk.Name,
                        RepoPath    = $"Knowledge/{pk.FolderKey}",
                    });

                // Knowledges only in DB (will be pushed on Push to R)
                var claimedKnowledgeIds = plan.Knowledges.Where(k => k.ExistingId != null).Select(k => k.ExistingId!.Value).ToHashSet();
                foreach (var k in dbKnowledges.Where(k => !claimedKnowledgeIds.Contains(k.Id)))
                    entries.Add(new KRepoCompareEntry
                    {
                        EntityType = "knowledge",
                        ChangeType = "db_only",
                        DbId       = k.Id,
                        Name       = k.Name,
                    });

                // Knowledges in both but renamed
                foreach (var pk in plan.Knowledges.Where(k => k.ExistingId != null))
                {
                    if (!dbKnowledgeMap.TryGetValue(pk.ExistingId!.Value, out var dbK)) continue;
                    if (dbK.Name != pk.Name)
                        entries.Add(new KRepoCompareEntry
                        {
                            EntityType    = "knowledge",
                            ChangeType    = "modified",
                            DbId          = dbK.Id,
                            Name          = pk.Name,
                            OldText       = dbK.Name,
                            NewText       = pk.Name,
                        });
                }

                // Nodes only in repo
                foreach (var pn in plan.Nodes.Where(n => n.ExistingId == null))
                    entries.Add(new KRepoCompareEntry
                    {
                        EntityType    = "node",
                        ChangeType    = "repo_only",
                        Name          = pn.Name,
                        KnowledgeName = pn.KnowledgeFolderKey,
                        RepoPath      = pn.FolderKey,
                    });

                // Nodes only in DB
                var claimedNodeIds = plan.Nodes.Where(n => n.ExistingId != null).Select(n => n.ExistingId!.Value).ToHashSet();
                foreach (var n in dbNodes.Where(n => !claimedNodeIds.Contains(n.Id)))
                {
                    var kName = dbKnowledgeMap.TryGetValue(n.KnowledgeId, out var kk) ? kk.Name : "";
                    entries.Add(new KRepoCompareEntry
                    {
                        EntityType    = "node",
                        ChangeType    = "db_only",
                        DbId          = n.Id,
                        Name          = n.Name,
                        KnowledgeName = kName,
                    });
                }

                // Nodes in both but renamed/moved
                foreach (var pn in plan.Nodes.Where(n => n.ExistingId != null))
                {
                    if (!dbNodeMap.TryGetValue(pn.ExistingId!.Value, out var dbN)) continue;
                    var repoKName = pn.KnowledgeFolderKey;
                    var dbKName   = dbKnowledgeMap.TryGetValue(dbN.KnowledgeId, out var dbKK) ? dbKK.Name : "";
                    var nameChanged   = dbN.Name != pn.Name;
                    var parentChanged = pn.ParentFolderKey != null; // non-null means it has a parent in repo
                    // only flag if name actually changed
                    if (nameChanged)
                        entries.Add(new KRepoCompareEntry
                        {
                            EntityType    = "node",
                            ChangeType    = "modified",
                            DbId          = dbN.Id,
                            Name          = pn.Name,
                            KnowledgeName = dbKName,
                            OldText       = dbN.Name,
                            NewText       = pn.Name,
                        });
                }

                // Questions: new in repo
                var dbQuestionMap = dbQuestions.ToDictionary(q => q.Id);
                var claimedQIds   = new HashSet<int>();
                foreach (var pq in plan.Questions.Where(q => q.ExistingId == null))
                {
                    // resolve node name for display
                    var nodeFolderName = pq.NodeFolderKey.Split('/').LastOrDefault() ?? pq.NodeFolderKey;
                    entries.Add(new KRepoCompareEntry
                    {
                        EntityType = "question",
                        ChangeType = "repo_only",
                        Name       = pq.Question,
                        NodeName   = nodeFolderName,
                        NewText    = pq.Answer,
                    });
                }

                // Questions: modified (content or draft-status changed)
                foreach (var pq in plan.Questions.Where(q => q.ExistingId != null))
                {
                    claimedQIds.Add(pq.ExistingId!.Value);
                    if (!dbQuestionMap.TryGetValue(pq.ExistingId.Value, out var dbQ)) continue;
                    var dbDraft   = dbQ.StatusCode == "draft";
                    var repoDraft = pq.IsDraft;
                    if (QuestionsEqual(dbQ.Name, dbQ.Description, dbDraft,
                                       pq.Question, pq.Answer, repoDraft)) continue;

                    var dbBody   = FlattenLine(dbQ.Name) + (string.IsNullOrEmpty(dbQ.Description) ? "" : "\n" + NormalizeDescription(dbQ.Description));
                    var repoBody = pq.Question.Trim() + (string.IsNullOrEmpty(pq.Answer) ? "" : "\n" + NormalizeDescription(pq.Answer));
                    // Tag draft status into the diff text so the UI shows *why* it's marked modified
                    // even when the body is identical (only the active/draft flag differs).
                    var dbText   = (dbDraft   ? "[draft] "  : "[active] ") + dbBody;
                    var repoText = (repoDraft ? "[draft] "  : "[active] ") + repoBody;
                    var nodeFolderName = pq.NodeFolderKey.Split('/').LastOrDefault() ?? pq.NodeFolderKey;
                    entries.Add(new KRepoCompareEntry
                    {
                        EntityType = "question",
                        ChangeType = "modified",
                        DbId       = dbQ.Id,
                        Name       = pq.Question,
                        NodeName   = nodeFolderName,
                        OldText    = dbText,
                        NewText    = repoText,
                    });
                }

                // Questions: only in DB (deleted in repo)
                foreach (var qId in plan.QuestionIdsToDelete)
                {
                    if (!dbQuestionMap.TryGetValue(qId, out var dbQ)) continue;
                    var nodeForQ = dbNodeMap.TryGetValue(dbQ.NodeId!.Value, out var nn) ? nn.Name : "";
                    entries.Add(new KRepoCompareEntry
                    {
                        EntityType = "question",
                        ChangeType = "db_only",
                        DbId       = dbQ.Id,
                        Name       = dbQ.Name,
                        NodeName   = nodeForQ,
                        OldText    = dbQ.Name + (string.IsNullOrEmpty(dbQ.Description) ? "" : "\n" + dbQ.Description),
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCompareDiff failed for user {UserId}", userId);
                return new KRepoCompareDiffResponse { Error = ex.Message };
            }

            return new KRepoCompareDiffResponse
            {
                Entries       = entries,
                RepoOnlyCount = entries.Count(e => e.ChangeType == "repo_only"),
                DbOnlyCount   = entries.Count(e => e.ChangeType == "db_only"),
                ModifiedCount = entries.Count(e => e.ChangeType == "modified"),
            };
        }

        public async Task CheckAndUpdateStatusAsync(int userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (string.IsNullOrEmpty(profile?.KRepoUrl) || string.IsNullOrEmpty(profile.KRepoPat))
                return;

            // Do not disturb a conflict state
            if (profile.KRepoStatusCode == "conflict") return;

            try
            {
                var localPath = GetLocalPath(userId);
                EnsureCloned(localPath, profile.KRepoUrl, profile.KRepoPat, profile.KRepoBranch ?? "main");

                using var repo = new LibGit2Sharp.Repository(localPath);
                var creds = BuildCredentials(profile.KRepoPat);

                Remote remote = repo.Network.Remotes["origin"];
                repo.Network.Fetch(remote.Name, remote.FetchRefSpecs.Select(r => r.Specification), new FetchOptions { CredentialsProvider = creds });

                var remoteBranch = repo.Branches[$"refs/remotes/origin/{profile.KRepoBranch ?? "main"}"];
                var remoteHeadSha = remoteBranch?.Tip?.Sha;

                profile.KRepoLastRemoteSha = remoteHeadSha;
                profile.KRepoLastCheckAt   = DateTime.UtcNow;

                var isBehind = remoteHeadSha != null && remoteHeadSha != profile.KRepoLastPushSha;
                profile.KRepoStatusCode = isBehind ? "behind" : "synced";

                await _profileRepo.UpsertUserProfileAsync(profile);

                if (isBehind)
                    await PushStatusAsync(userId, "behind", "Remote changes available", "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckAndUpdateStatus failed for user {UserId}", userId);
            }
        }

        public async Task PushAllUsersAsync()
        {
            var userIds = await _db.UserProfiles
                .Where(p => p.KRepoUrl != null && p.KRepoUrl != "" && p.KRepoStatusCode != "conflict")
                .Select(p => p.UserId)
                .ToListAsync();

            foreach (var userId in userIds)
            {
                try { await PushToRepoAsync(userId); }
                catch (Exception ex) { _logger.LogError(ex, "PushAllUsers: error for user {UserId}", userId); }
            }
        }

        public async Task<ResultOptions> ResetConflictAndRetryAsync(int userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (profile == null) return ResultOptions.Fail("Profile not found", 404);

            // Clear conflict status so push is allowed again
            profile.KRepoStatusCode = "idle";
            await _profileRepo.UpsertUserProfileAsync(profile);

            return await PushToRepoAsync(userId);
        }

        public async Task CheckAllUsersAsync()
        {
            var userIds = await _db.UserProfiles
                .Where(p => p.KRepoUrl != null && p.KRepoUrl != "" && p.KRepoStatusCode != "conflict")
                .Select(p => p.UserId)
                .ToListAsync();

            foreach (var userId in userIds)
            {
                try { await CheckAndUpdateStatusAsync(userId); }
                catch (Exception ex) { _logger.LogError(ex, "CheckAllUsers: error for user {UserId}", userId); }
            }
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private async Task<ResultOptions> DoPushAsync(UserProfile profile, bool force = false)
        {
            var userId    = profile.UserId;
            var branch    = profile.KRepoBranch ?? "main";
            var localPath = GetLocalPath(userId);
            EnsureCloned(localPath, profile.KRepoUrl!, profile.KRepoPat!, branch);

            // Load all knowledges + nodes + questions
            var knowledges = (await _knowledgeRepo.GetAllKnowledgesByUserIdAsync(userId))
                .Where(k => k.DeletedAt == null).ToList();
            var fileMap    = new Dictionary<string, string>(); // repo-relative path → content

            // Folder/file names are clean (no "[id]"); the id lives in the .md front-matter.
            // Sibling names are made unique so two entities sharing a name never collide.
            var knowledgeName = AssignUniqueNames(knowledges.Select(k => (k.Id, k.Name)));

            foreach (var k in knowledges)
            {
                var kUniq = knowledgeName[k.Id];
                var kDir  = $"Knowledge/{kUniq}";
                // A knowledge is always a folder; its own file is "<K>/_.md".
                fileMap[$"{kDir}/_.md"] = BuildFrontMatter(k.Id, k.Name);

                var tree = await _knowledgeRepo.GetKnowledgeTreeAsync(k.Id, userId);
                if (tree == null) continue;

                var activeNodes = tree.Nodes.Where(n => n.DeletedAt == null).ToList();
                // Key by ParentId, using 0 for roots (Dictionary<int?,> rejects a null key).
                var childrenByParent = activeNodes.GroupBy(n => n.ParentId ?? 0)
                    .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Id).ToList());
                // A node is non-leaf iff its id is some other node's ParentId.
                var hasChildren = childrenByParent.Keys.Where(id => id != 0).ToHashSet();

                var uniqueName   = new Dictionary<int, string>();
                var containerDir = new Dictionary<int, string>(); // dir the node's file sits in
                var childDir     = new Dictionary<int, string>(); // dir holding a non-leaf node's children

                // Assign unique names top-down. In each directory reserve "_" (the
                // knowledge self-file marker) and the parent node's own file name
                // ("<P>.md") so a child can never collide with a self-file.
                void Assign(int parentId, string container, string? reservedSelf)
                {
                    if (!childrenByParent.TryGetValue(parentId, out var kids)) return;
                    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "_" };
                    if (reservedSelf != null) used.Add(reservedSelf);
                    foreach (var n in kids)
                    {
                        var name = MakeUnique(Sanitize(n.Name), used);
                        uniqueName[n.Id]   = name;
                        containerDir[n.Id] = container;
                        if (hasChildren.Contains(n.Id))
                        {
                            var dir = $"{container}/{name}";
                            childDir[n.Id] = dir;
                            Assign(n.Id, dir, name);
                        }
                    }
                }
                Assign(0, kDir, null); // root group: only "_" (knowledge self) is reserved

                foreach (var node in activeNodes)
                {
                    var questions = await _questionRepo.GetQuestionsByNodeAsync(node.Id);
                    var active    = questions.Where(q => q.DeletedAt == null).ToList();
                    var content   = BuildFrontMatter(node.Id, node.Name) + BuildRepoMarkdown(active);
                    var name      = uniqueName[node.Id];
                    // Non-leaf → folder "<name>/<name>.md"; leaf → file "<name>.md".
                    var filePath  = hasChildren.Contains(node.Id)
                        ? $"{childDir[node.Id]}/{name}.md"
                        : $"{containerDir[node.Id]}/{name}.md";
                    fileMap[filePath] = content;
                }
            }

            // Compute content hash. The short-circuit is an optimisation for the
            // push-before-pull step (skip when DB is unchanged so remote edits survive
            // to be applied). It is bypassed for an explicit force push, because the
            // remote may have diverged from DB even when the DB content hash matches.
            var newHash = ComputeHash(fileMap);
            if (!force && newHash == profile.KRepoContentHash)
                return new ResultOptions { Success = true, Message = "No changes" };

            var creds = BuildCredentials(profile.KRepoPat!);
            string repoWorkDir;
            List<string>? conflictedPaths = null;

            // ── Pull latest (close repo after so handles are released on Windows) ──
            using (var repo = new LibGit2Sharp.Repository(localPath))
            {
                repoWorkDir = repo.Info.WorkingDirectory.TrimEnd('/', '\\');
                var pullSig = new Signature("superapp", "superapp@local", DateTimeOffset.UtcNow);

                MergeResult pullResult;
                try
                {
                    pullResult = Commands.Pull(repo, pullSig,
                        new PullOptions { FetchOptions = new FetchOptions { CredentialsProvider = creds } });
                }
                catch (Exception ex)
                {
                    return ResultOptions.Fail("Pull failed: " + ex.Message, 409);
                }

                if (pullResult.Status == MergeStatus.Conflicts)
                {
                    // ── DB-wins conflict resolution ──────────────────────────────
                    // 1. Collect conflicted file paths for the commit message
                    conflictedPaths = repo.Index.Conflicts
                        .Select(c => c.Ours?.Path ?? c.Theirs?.Path ?? c.Ancestor?.Path ?? "?")
                        .ToList();

                    _logger.LogWarning("DoPush: {Count} merge conflict(s) — resolving with DB-wins strategy: {Paths}",
                        conflictedPaths.Count, string.Join(", ", conflictedPaths));

                    // 2. Abort the conflicted merge
                    repo.Reset(ResetMode.Hard);

                    // 3. Re-pull with MergeFileFavor.Ours (auto-resolves content conflicts)
                    try
                    {
                        pullResult = Commands.Pull(repo, pullSig, new PullOptions
                        {
                            FetchOptions = new FetchOptions { CredentialsProvider = creds },
                            MergeOptions = new MergeOptions { MergeFileFavor = MergeFileFavor.Ours }
                        });
                    }
                    catch (Exception ex)
                    {
                        return ResultOptions.Fail("Pull (DB-wins retry) failed: " + ex.Message, 409);
                    }

                    // Structural conflict (e.g. delete-vs-modify, file-vs-directory)
                    if (pullResult.Status == MergeStatus.Conflicts)
                        return ResultOptions.Fail("Structural merge conflict — use Force Update to overwrite remote.", 409);
                }
            }
            // repo disposed here — all file handles released before we touch the FS

            // ── Write new files, remove stale .md files (never delete dirs) ─────────
            // Deleting a dir that git just removed via Pull leaves it in Windows
            // "pending deletion" state; an immediate CreateDirectory on the same path
            // then fails with ERROR_INVALID_NAME. Avoid the problem by only touching
            // individual files and letting git Stage("*") detect the deletions.
            var knowledgeRoot = Path.Combine(repoWorkDir, "Knowledge");
            if (!Directory.Exists(knowledgeRoot))
                Directory.CreateDirectory(knowledgeRoot);

            // Existing .md paths (repo-relative, forward slashes)
            var existingMd = Directory
                .GetFiles(knowledgeRoot, "*.md", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(repoWorkDir, f).Replace('\\', '/'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Write / overwrite files from DB
            var newPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (relPath, content) in fileMap)
            {
                newPaths.Add(relPath);
                var absPath = Path.Combine(repoWorkDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                _logger.LogDebug("DoPush: writing {Path}", relPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);
                await System.IO.File.WriteAllTextAsync(absPath, content, Encoding.UTF8);
            }

            // Delete .md files no longer present in DB
            foreach (var old in existingMd.Where(p => !newPaths.Contains(p)))
            {
                var absOld = Path.Combine(repoWorkDir, old.Replace('/', Path.DirectorySeparatorChar));
                _logger.LogDebug("DoPush: removing stale {Path}", old);
                if (System.IO.File.Exists(absOld))
                    System.IO.File.Delete(absOld);
            }

            // ── Stage, commit, push (reopen repo after FS writes are done) ────────
            using (var repo = new LibGit2Sharp.Repository(localPath))
            {
                // Ensure we're on the configured branch (clone may default to "main")
                var localBranch = repo.Branches[branch];
                if (localBranch == null)
                {
                    _logger.LogInformation("DoPush: creating local branch '{Branch}' from HEAD", branch);
                    localBranch = repo.CreateBranch(branch);
                }
                if (!repo.Head.FriendlyName.Equals(branch, StringComparison.Ordinal))
                {
                    _logger.LogInformation("DoPush: switching from '{Current}' to '{Target}'",
                        repo.Head.FriendlyName, branch);
                    Commands.Checkout(repo, localBranch);
                }

                // Set up tracking so ahead/behind detection works
                var remoteBranchRef = $"refs/remotes/origin/{branch}";
                if (repo.Branches[remoteBranchRef] != null)
                    repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranchRef);

                Commands.Stage(repo, "*");

                var localTip   = repo.Head.Tip;
                var tracking   = repo.Head.TrackedBranch;
                var remoteTip  = tracking?.Tip;
                var isDirty    = repo.RetrieveStatus().IsDirty;
                var localAhead = localTip != null && (remoteTip == null || remoteTip.Sha != localTip.Sha);

                if (!isDirty && !localAhead)
                {
                    _logger.LogInformation("DoPush: no file changes and local == remote — nothing to do");
                    profile.KRepoContentHash = newHash;
                    await _profileRepo.UpsertUserProfileAsync(profile);
                    return new ResultOptions { Success = true, Message = "No changes after write" };
                }

                // Create a new commit if there are staged changes
                Commit? commit = localTip;
                if (isDirty)
                {
                    var sig        = new Signature("SuperApp Sync", "sync@superapp.local", DateTimeOffset.UtcNow);
                    var commitMsg  = $"DB sync: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                    if (conflictedPaths is { Count: > 0 })
                        commitMsg += $"\n\nResolved {conflictedPaths.Count} conflict(s) — DB wins:\n"
                                   + string.Join("\n", conflictedPaths.Select(p => $"  - {p}"));
                    commit = repo.Commit(commitMsg, sig, sig);
                    _logger.LogInformation("DoPush: committed {Sha}", commit.Sha[..8]);
                }
                else
                {
                    _logger.LogInformation("DoPush: no file changes but local is ahead of remote — pushing pending commit(s)");
                }

                // Push to remote
                _logger.LogInformation("DoPush: pushing branch '{Branch}', HEAD={Sha}", branch, repo.Head.Tip.Sha[..8]);
                repo.Network.Push(repo.Branches[branch], new PushOptions { CredentialsProvider = creds });
                _logger.LogInformation("DoPush: pushed to remote successfully");

                profile.KRepoLastPushSha  = commit!.Sha;
                profile.KRepoLastPushAt   = DateTime.UtcNow;
                profile.KRepoContentHash  = newHash;
                profile.KRepoStatusCode   = "synced";
                await _profileRepo.UpsertUserProfileAsync(profile);
            }

            return new ResultOptions { Success = true };
        }

        private async Task<ResultOptions> DoApplyRemoteChangesAsync(UserProfile profile, int userId)
        {
            var localPath = GetLocalPath(userId);
            if (!LibGit2Sharp.Repository.IsValid(localPath))
                return ResultOptions.Fail("Local repo not found, trigger a push first.", 400);

            using var repo = new LibGit2Sharp.Repository(localPath);
            var creds  = BuildCredentials(profile.KRepoPat!);
            var branch = profile.KRepoBranch ?? "main";

            Remote remote = repo.Network.Remotes["origin"];
            repo.Network.Fetch(remote.Name, remote.FetchRefSpecs.Select(r => r.Specification),
                new FetchOptions { CredentialsProvider = creds });

            var remoteBranch = repo.Branches[$"refs/remotes/origin/{branch}"];
            if (remoteBranch == null)
                return ResultOptions.Fail("Remote branch not found.", 400);

            var remoteHead = remoteBranch.Tip;
            var now = DateTime.UtcNow;

            // ── 1. Read repo: every ".md" file → a logical knowledge/node ──────────
            // Layout: leaf node = "<Name>.md"; non-leaf node = folder "<Name>/" whose own
            // "<Name>.md" holds its questions; knowledge = folder "<K>/" with "<K>.md".
            // A file whose base name equals its containing folder is that folder's "self"
            // file (the entity = the folder); any other ".md" is a leaf node.
            var mdFiles = new Dictionary<string, string>(StringComparer.Ordinal);
            WalkMdFiles(remoteHead.Tree, "", mdFiles);

            var knowledgeFolders = new List<RepoKnowledgeFolder>();
            var nodeFolders      = new List<RepoNodeFolder>();
            foreach (var (path, content) in mdFiles)
            {
                var logical = KRepoSyncPlanner.LogicalPath(path);
                if (logical == null) continue;

                var (id, _, body) = ParseFrontMatter(content);
                if (logical.Length == 2)                               // Knowledge/<K> ⇒ knowledge
                    knowledgeFolders.Add(new RepoKnowledgeFolder(logical[1], id, logical[1]));
                else if (logical.Length >= 3)                          // node
                    nodeFolders.Add(new RepoNodeFolder(string.Join("/", logical), id, logical[^1], ParseQuestions(body)));
            }

            // ── 2. DB snapshot for this user ──────────────────────────────────────
            var dbKnowledges   = await _db.KKnowledges
                .Where(k => k.UserId == userId && k.DeletedAt == null)
                .ToListAsync();
            var dbKnowledgeMap = dbKnowledges.ToDictionary(k => k.Id);
            var userKnowledgeIds = dbKnowledges.Select(k => k.Id).ToList();

            var dbNodes   = await _db.KNodes
                .Where(n => userKnowledgeIds.Contains(n.KnowledgeId) && n.DeletedAt == null)
                .ToListAsync();
            var dbNodeMap = dbNodes.ToDictionary(n => n.Id);
            var dbNodeIds = dbNodes.Select(n => n.Id).ToList();

            var dbQuestions = await _db.KQuestions
                .Where(q => q.NodeId != null && dbNodeIds.Contains(q.NodeId.Value) && q.DeletedAt == null)
                .ToListAsync();

            // ── 3. Compute the reconciliation plan (pure) ─────────────────────────
            var plan = KRepoSyncPlanner.Plan(
                knowledgeFolders,
                nodeFolders,
                dbKnowledges.Select(k => new DbKnowledgeRef(k.Id, k.Name)).ToList(),
                dbNodes.Select(n => new DbNodeRef(n.Id, n.KnowledgeId, n.ParentId, n.Name)).ToList(),
                dbQuestions.Select(q => new DbQuestionRef(q.Id, q.NodeId!.Value, q.Name, q.Description, q.StatusCode == "draft", q.SortOrder)).ToList());

            // ── 4. Knowledges: create / rename ────────────────────────────────────
            var folderToKnowledgeId = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pk in plan.Knowledges)
            {
                if (pk.ExistingId == null)
                {
                    var k = new KKnowledge(pk.Name, userId);
                    _db.KKnowledges.Add(k);
                    await _db.SaveChangesAsync();
                    folderToKnowledgeId[pk.FolderKey] = k.Id;
                    _logger.LogInformation("Reconcile: created knowledge '{Name}' → id {Id}", pk.Name, k.Id);
                }
                else
                {
                    folderToKnowledgeId[pk.FolderKey] = pk.ExistingId.Value;
                    if (dbKnowledgeMap.TryGetValue(pk.ExistingId.Value, out var k) && k.Name != pk.Name)
                    {
                        _logger.LogInformation("Reconcile: renamed knowledge {Id} '{Old}' → '{New}'", k.Id, k.Name, pk.Name);
                        k.Name = pk.Name;
                        k.UpdatedAt = now;
                    }
                }
            }
            await _db.SaveChangesAsync();

            // ── 5. Nodes: create / rename / move (parents first, ids resolve top-down) ─
            var folderToNodeId = new Dictionary<string, int>(StringComparer.Ordinal);
            var touched        = new List<KNodeEntity>();
            foreach (var pn in plan.Nodes)
            {
                if (!folderToKnowledgeId.TryGetValue(pn.KnowledgeFolderKey, out var knowledgeId))
                {
                    _logger.LogWarning("Reconcile: node '{Folder}' has unresolved knowledge '{K}', skipped",
                        pn.FolderKey, pn.KnowledgeFolderKey);
                    continue;
                }

                int? parentId = null;
                if (pn.ParentFolderKey != null && folderToNodeId.TryGetValue(pn.ParentFolderKey, out var pid))
                    parentId = pid;

                if (pn.ExistingId == null)
                {
                    var node = new KNodeEntity
                    {
                        KnowledgeId = knowledgeId,
                        ParentId    = parentId,
                        Name        = pn.Name,
                        CreatedAt   = now,
                    };
                    _db.KNodes.Add(node);
                    await _db.SaveChangesAsync();
                    _db.KNodeStatusHistory.Add(new KNodeStatusHistoryEntity
                    {
                        NodeId     = node.Id,
                        StatusCode = node.StatusCode ?? "learning",
                        ChangedAt  = now,
                        UserId     = userId,
                    });
                    await _db.SaveChangesAsync();
                    folderToNodeId[pn.FolderKey] = node.Id;
                    touched.Add(node);
                    _logger.LogInformation("Reconcile: created node '{Name}' → id {Id} (knowledge {K}, parent {P})",
                        pn.Name, node.Id, knowledgeId, parentId);
                }
                else
                {
                    if (!dbNodeMap.TryGetValue(pn.ExistingId.Value, out var node)) continue;
                    var changed = false;
                    if (node.Name        != pn.Name)      { node.Name = pn.Name;               changed = true; }
                    if (node.KnowledgeId != knowledgeId)  { node.KnowledgeId = knowledgeId;     changed = true; }
                    if (node.ParentId    != parentId)     { node.ParentId = parentId;           changed = true; }
                    if (changed)
                    {
                        node.UpdatedAt = now;
                        _logger.LogInformation("Reconcile: updated node {Id} ('{Name}') → knowledge {K}, parent {P}",
                            node.Id, node.Name, knowledgeId, parentId);
                    }
                    folderToNodeId[pn.FolderKey] = node.Id;
                    touched.Add(node);
                }
            }
            await _db.SaveChangesAsync();

            // Rebuild PathIds/PathDepth via the canonical helper (convention "/{id}/…").
            if (touched.Any()) await _nodeHelper.SyncPathIdsAsync(touched);

            // ── 6. Questions: create / update / move / draft-toggle ───────────────
            foreach (var group in plan.Questions.GroupBy(q => q.NodeFolderKey))
            {
                if (!folderToNodeId.TryGetValue(group.Key, out var nodeId)) continue;

                var toAdd       = new List<(KNewQuestionItem item, bool isDraft)>();
                var toUpdate    = new List<KUpdateQuestionItem>();
                var toggleDraft = new List<int>();

                foreach (var pq in group)
                {
                    if (pq.ExistingId == null)
                    {
                        toAdd.Add((new KNewQuestionItem { Name = pq.Question, Description = pq.Answer, SortOrder = pq.SortOrder }, pq.IsDraft));
                        continue;
                    }

                    var q = await _db.KQuestions.FirstOrDefaultAsync(x => x.Id == pq.ExistingId.Value && x.DeletedAt == null);
                    if (q == null) continue;

                    if (q.NodeId != nodeId) { q.NodeId = nodeId; q.UpdatedAt = now; } // moved to this node
                    var textChanged  = q.Name != pq.Question || (q.Description ?? "") != (pq.Answer ?? "");
                    var orderChanged = q.SortOrder != pq.SortOrder;
                    if (textChanged || orderChanged)
                        toUpdate.Add(new KUpdateQuestionItem { Id = q.Id, Name = pq.Question, Description = pq.Answer, SortOrder = pq.SortOrder });
                    if ((q.StatusCode == "draft") != pq.IsDraft) toggleDraft.Add(q.Id);
                }

                await _db.SaveChangesAsync(); // persist any cross-node moves first

                if (toUpdate.Any())    await _questionRepo.UpdateQuestionsDataAsync(toUpdate);
                if (toggleDraft.Any()) await _questionRepo.ToggleQuestionsDraftAsync(toggleDraft);
                if (toAdd.Any())
                {
                    var newIds = await _questionRepo.AddQuestionsAsync(nodeId, toAdd.Select(x => x.item).ToList());
                    var newDraftIds = toAdd.Zip(newIds, (x, id) => (x.isDraft, id)).Where(x => x.isDraft).Select(x => x.id).ToList();
                    if (newDraftIds.Any()) await _questionRepo.ToggleQuestionsDraftAsync(newDraftIds);
                }
            }
            if (plan.QuestionIdsToDelete.Any())
                await _questionRepo.DeleteQuestionsAsync(plan.QuestionIdsToDelete);

            // ── 7. DB-wins: unclaimed nodes/knowledges are NOT deleted ─────────
            // They will be pushed back to the repo by DoPushAsync below.
            // Only log for diagnostics.
            var unclaimedNodeCount = dbNodes.Count(n => !plan.Nodes.Any(p => p.ExistingId == n.Id));
            var unclaimedKnowledgeCount = dbKnowledges.Count(k => !plan.Knowledges.Any(p => p.ExistingId == k.Id));
            if (unclaimedNodeCount > 0 || unclaimedKnowledgeCount > 0)
                _logger.LogInformation(
                    "Reconcile: {Nodes} node(s) and {Knowledges} knowledge(s) in DB but not in repo — will push back",
                    unclaimedNodeCount, unclaimedKnowledgeCount);

            // Push back so newly-created ids / detail.md files land in the repo.
            return await DoPushAsync(profile);
        }

        // Collects every ".md" blob in the tree: path → text content.
        private static void WalkMdFiles(Tree tree, string prefix, Dictionary<string, string> mdFiles)
        {
            foreach (var entry in tree)
            {
                var fullPath = string.IsNullOrEmpty(prefix) ? entry.Name : $"{prefix}/{entry.Name}";
                if (entry.TargetType == TreeEntryTargetType.Tree)
                    WalkMdFiles((Tree)entry.Target, fullPath, mdFiles);
                else if (entry.TargetType == TreeEntryTargetType.Blob
                         && entry.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    mdFiles[fullPath] = ((Blob)entry.Target).GetContentText(Encoding.UTF8);
            }
        }

        // ── Markdown builder (C# port of kMarkdownEditor.utils.ts) ───────────────

        /// <summary>
        /// Flattens a string to a single line — collapses CRLF/LF and any run of
        /// whitespace into one space. Used when a value goes onto the heading line
        /// of a markdown question (newlines there would break the parser, since a
        /// new line starting with "# " is read as a new question).
        /// </summary>
        internal static string FlattenLine(string? s) =>
            string.IsNullOrEmpty(s) ? "" : Regex.Replace(s.Replace("\r\n", "\n").Replace('\n', ' '), @"\s+", " ").Trim();

        /// <summary>
        /// Per-line right-trim plus overall trim. Used by the diff comparer because
        /// the parser reads each repo line via <c>line.TrimEnd()</c>, so trailing
        /// whitespace per line is lost on round-trip; we strip it from the DB side
        /// before comparing to avoid false "modified" diffs.
        /// </summary>
        internal static string NormalizeDescription(string? s) => string.IsNullOrEmpty(s)
            ? ""
            : string.Join("\n", s.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd())).Trim();

        /// <summary>
        /// Pure compare for a question round-trip. Returns true when DB and repo carry
        /// the same content + draft flag (ignoring incidental whitespace lost during
        /// the round-trip). Mirrors the logic in <c>GetCompareDiffAsync</c>.
        /// </summary>
        internal static bool QuestionsEqual(
            string dbName, string? dbDescription, bool dbDraft,
            string repoName, string repoAnswer, bool repoDraft)
        {
            var dbBody   = FlattenLine(dbName)
                         + (string.IsNullOrEmpty(dbDescription) ? "" : "\n" + NormalizeDescription(dbDescription));
            var repoBody = (repoName ?? "").Trim()
                         + (string.IsNullOrEmpty(repoAnswer) ? "" : "\n" + NormalizeDescription(repoAnswer));
            return dbBody == repoBody && dbDraft == repoDraft;
        }

        internal static string BuildRepoMarkdown(List<KQuestionEntity> questions)
        {
            if (!questions.Any()) return string.Empty;

            var sb = new StringBuilder();
            var sorted = questions.OrderBy(q => q.SortOrder).ToList();

            foreach (var q in sorted)
            {
                var tag      = $"[id:{q.Id} order:{q.SortOrder}]";
                var nameLine = FlattenLine(q.Name);
                if (q.StatusCode == "draft")
                {
                    var answer = q.Description?.Trim();
                    if (!string.IsNullOrEmpty(answer))
                    {
                        sb.AppendLine($"<!--# {nameLine} {tag}");
                        sb.AppendLine($"{answer} -->");
                    }
                    else
                    {
                        sb.AppendLine($"<!--# {nameLine} {tag} -->");
                    }
                }
                else
                {
                    sb.AppendLine($"# {nameLine} {tag}");
                    if (!string.IsNullOrEmpty(q.Description?.Trim()))
                        sb.AppendLine(q.Description!.Trim());
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        // Assigns each (id, name) a unique folder name, in id order for stability.
        private static Dictionary<int, string> AssignUniqueNames(IEnumerable<(int Id, string Name)> items)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var map  = new Dictionary<int, string>();
            foreach (var (id, name) in items.OrderBy(x => x.Id))
                map[id] = MakeUnique(Sanitize(name), used);
            return map;
        }

        // Returns baseName, or "baseName (2)", "baseName (3)", … if already taken.
        private static string MakeUnique(string baseName, HashSet<string> used)
        {
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "untitled";
            var name = baseName;
            var k = 2;
            while (used.Contains(name)) name = $"{baseName} ({k++})";
            used.Add(name);
            return name;
        }

        private static string Sanitize(string name) =>
            Regex.Replace(name, @"[\\/:*?""<>|]", "_").Trim();

        // YAML front-matter: id (identity link to DB) + name (display). Regenerated from
        // DB on every push; on pull only `id` matters — the node/knowledge name comes
        // from the clean folder name.
        private static string BuildFrontMatter(int id, string name)
        {
            var escaped = (name ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"---\nid: {id}\nname: \"{escaped}\"\n---\n\n";
        }

        // Parses a leading YAML front-matter block ("---" … "---"): returns the declared
        // id/name and the markdown body after it. If absent, id/name are null and body is
        // the original content (so question parsing never sees front-matter lines).
        private static (int? id, string? name, string body) ParseFrontMatter(string content)
        {
            if (string.IsNullOrEmpty(content)) return (null, null, content ?? "");
            var lines = content.TrimStart('﻿').Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0 || lines[0].Trim() != "---") return (null, null, content);

            int? id = null; string? name = null;
            for (var i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "---")
                    return (id, name, string.Join("\n", lines.Skip(i + 1)).TrimStart('\n'));

                var m = Regex.Match(lines[i], @"^\s*(\w+)\s*:\s*(.*?)\s*$");
                if (!m.Success) continue;
                var key = m.Groups[1].Value;
                var val = m.Groups[2].Value.Trim();
                if      (key == "id"   && int.TryParse(val, out var idv)) id = idv;
                else if (key == "name") name = Unquote(val);
            }
            return (null, null, content); // no closing fence → not front-matter
        }

        private static string Unquote(string v) =>
            v.Length >= 2 && v[0] == '"' && v[^1] == '"'
                ? v[1..^1].Replace("\\\"", "\"").Replace("\\\\", "\\")
                : v;

        // ── Markdown parser (C# port) ─────────────────────────────────────────────

        internal static List<ParsedQuestion> ParseQuestions(string body)
        {
            var result = new List<ParsedQuestion>();
            string? question = null, answer = null;
            bool isDraft = false;
            int? id = null, order = null;
            bool inDraft = false;

            // Tolerate the editor putting "<!--" on its own line with the heading on
            // the next line — collapse that into the canonical single-line opener so
            // the regex below can recognise it as a draft block.
            body = (body ?? "").Replace("\r\n", "\n");
            body = Regex.Replace(body, @"<!--\s*\n\s*#\s", "<!--# ");

            void Flush()
            {
                if (question == null) return;
                result.Add(new ParsedQuestion(id, question, answer?.Trim() ?? "", isDraft, order));
                question = answer = null; isDraft = false; id = order = null;
            }

            foreach (var raw in body.Split('\n'))
            {
                var line = raw.TrimEnd();

                // Draft block start
                if (Regex.IsMatch(line, @"^<!--\s*#\s"))
                {
                    Flush();
                    var isSingle = line.TrimEnd().EndsWith("-->");
                    var inner    = Regex.Replace(line, @"^<!--\s*#\s+", "").TrimEnd();
                    if (isSingle) inner = Regex.Replace(inner, @"\s*-->\s*$", "");
                    var (q, metaId, metaOrder) = ExtractMeta(inner);
                    if (q.Length > 0 && metaId.HasValue)
                    {
                        question = q; id = metaId; order = metaOrder; isDraft = true;
                        if (isSingle) Flush();
                        else inDraft = true;
                    }
                    continue;
                }

                if (inDraft)
                {
                    var isClose = line.TrimEnd().EndsWith("-->");
                    var content = isClose ? Regex.Replace(line, @"\s*-->\s*$", "") : line;
                    if (!string.IsNullOrEmpty(content))
                        answer = answer == null ? content : answer + "\n" + content;
                    if (isClose) { Flush(); inDraft = false; }
                    continue;
                }

                // Active question
                if (Regex.IsMatch(line, @"^#\s"))
                {
                    Flush();
                    var (q, metaId, metaOrder) = ExtractMeta(line[2..].Trim());
                    if (q.Length > 0) { question = q; id = metaId; order = metaOrder; }
                    continue;
                }

                // Answer lines
                if (question != null && !isDraft)
                    answer = answer == null ? line : answer + "\n" + line;
            }
            Flush();
            return result;
        }

        private static (string question, int? id, int? order) ExtractMeta(string text)
        {
            var match = Regex.Match(text, @"\[([^\]]+)\]");
            if (!match.Success) return (text.Trim(), null, null);

            var bracket = match.Value;
            var clean   = text.Replace(bracket, "").Trim();
            int? id = null, order = null;

            foreach (Match m in Regex.Matches(bracket, @"(\w+):(\S+)"))
            {
                if (m.Groups[1].Value == "id"    && int.TryParse(m.Groups[2].Value, out var i)) id    = i;
                if (m.Groups[1].Value == "order" && int.TryParse(m.Groups[2].Value, out var o)) order = o;
            }
            return (clean, id, order);
        }

        // Quick helper used by GetDiffAsync
        private static Dictionary<int, string> ParseRepoMarkdownQuestions(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return new();
            return ParseQuestions(content)
                .Where(p => p.Id.HasValue)
                .ToDictionary(p => p.Id!.Value, p => p.Question);
        }

        private static string GetBlobContent(LibGit2Sharp.Repository repo, Commit commit, string path)
        {
            var entry = commit[path];
            if (entry?.Target is not Blob blob) return "";
            return blob.GetContentText(Encoding.UTF8);
        }

        // ── Utilities ─────────────────────────────────────────────────────────────

        private string GetLocalPath(int userId) =>
            Path.Combine(_reposBasePath, userId.ToString());

        private static void EnsureCloned(string localPath, string repoUrl, string pat, string branch)
        {
            if (LibGit2Sharp.Repository.IsValid(localPath)) return;

            Directory.CreateDirectory(localPath);
            var co = new CloneOptions
            {
                BranchName = branch,
                FetchOptions = { CredentialsProvider = BuildCredentials(pat) },
            };
            LibGit2Sharp.Repository.Clone(repoUrl, localPath, co);
        }

        private static CredentialsHandler BuildCredentials(string pat) =>
            (_, _, _) => new UsernamePasswordCredentials { Username = "x-access-token", Password = pat };

        private static string ComputeHash(Dictionary<string, string> fileMap)
        {
            var sorted  = string.Concat(fileMap.OrderBy(kv => kv.Key).Select(kv => kv.Key + kv.Value));
            var bytes   = SHA256.HashData(Encoding.UTF8.GetBytes(sorted));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private async Task PushStatusAsync(int userId, string status, string? message, string direction)
        {
            await _notifier.NotifyAsync(userId, status, message, direction);
        }

        private async Task SaveStatusAsync(UserProfile profile, string statusCode)
        {
            profile.KRepoStatusCode = statusCode;
            await _profileRepo.UpsertUserProfileAsync(profile);
        }

        // ── Force Update Remote ──────────────────────────────────────────────────

        public async Task<ResultOptions> ForceUpdateRemoteAsync(int userId)
            => await ForceUpdateRemoteAsync(userId, CancellationToken.None);

        public async Task<ResultOptions> ForceUpdateRemoteAsync(int userId, CancellationToken ct)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (profile == null)
                return ResultOptions.Fail("User profile not found", 404);
            if (string.IsNullOrEmpty(profile.KRepoUrl) || string.IsNullOrEmpty(profile.KRepoPat))
                return ResultOptions.Fail("Repo not configured", 400);

            try
            {
                await PushStatusAsync(userId, "syncing", "Force updating remote…", "push");
                ct.ThrowIfCancellationRequested();
                var result = await DoForceUpdateRemoteAsync(profile, ct);
                await PushStatusAsync(userId, result.Success ? "synced" : "error",
                    result.Message, "push");
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ForceUpdateRemote cancelled for user {UserId}", userId);
                return ResultOptions.Fail("Cancelled", 499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForceUpdateRemote failed for user {UserId}", userId);
                await PushStatusAsync(userId, "error", ex.Message, "push");
                return ResultOptions.Fail("Force update failed: " + ex.Message, 500);
            }
        }

        // ── Resolve Conflicts ────────────────────────────────────────────────────

        /// <summary>
        /// For each conflict, applies "keep_db" (no-op — DB already has it) or
        /// "keep_repo" (overwrite DB entity with repo's value), then force-pushes
        /// the resulting DB state to remote so both sides match.
        /// </summary>
        public async Task<ResultOptions> ResolveConflictsAsync(int userId, List<KRepoResolveConflictItem> items)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (profile == null)
                return ResultOptions.Fail("User profile not found", 404);
            if (string.IsNullOrEmpty(profile.KRepoUrl) || string.IsNullOrEmpty(profile.KRepoPat))
                return ResultOptions.Fail("Repo not configured", 400);
            if (items == null || items.Count == 0)
                return ResultOptions.Fail("No items to resolve", 400);

            try
            {
                await PushStatusAsync(userId, "syncing", "Resolving conflicts…", "push");

                var keepRepo = items.Where(i => i.Action == "keep_repo").ToList();
                if (keepRepo.Count > 0)
                {
                    // Need repo content to apply "keep_repo" — fetch & parse
                    var localPath = GetLocalPath(userId);
                    EnsureCloned(localPath, profile.KRepoUrl, profile.KRepoPat, profile.KRepoBranch ?? "main");

                    var repoNodes      = new Dictionary<int, ParsedQuestion[]>();
                    var repoNodeNames  = new Dictionary<int, string>();
                    var repoKnowledges = new Dictionary<int, string>();
                    using (var repo = new LibGit2Sharp.Repository(localPath))
                    {
                        var creds  = BuildCredentials(profile.KRepoPat);
                        var branch = profile.KRepoBranch ?? "main";
                        Remote remote = repo.Network.Remotes["origin"];
                        repo.Network.Fetch(remote.Name, remote.FetchRefSpecs.Select(r => r.Specification),
                            new FetchOptions { CredentialsProvider = creds });
                        var remoteBranch = repo.Branches[$"refs/remotes/origin/{branch}"];
                        if (remoteBranch == null)
                            return ResultOptions.Fail("Remote branch not found", 400);

                        var mdFiles = new Dictionary<string, string>(StringComparer.Ordinal);
                        WalkMdFiles(remoteBranch.Tip.Tree, "", mdFiles);
                        foreach (var (path, content) in mdFiles)
                        {
                            var logical = KRepoSyncPlanner.LogicalPath(path);
                            if (logical == null) continue;
                            var (id, _, body) = ParseFrontMatter(content);
                            if (id == null) continue;
                            if (logical.Length == 2)
                                repoKnowledges[id.Value] = logical[1];
                            else if (logical.Length >= 3)
                            {
                                repoNodeNames[id.Value] = logical[^1];
                                repoNodes[id.Value]     = ParseQuestions(body).ToArray();
                            }
                        }
                    }

                    var now = DateTime.UtcNow;
                    foreach (var it in keepRepo)
                    {
                        if (it.EntityType == "knowledge")
                        {
                            var k = await _db.KKnowledges.FirstOrDefaultAsync(x => x.Id == it.DbId && x.DeletedAt == null);
                            if (k != null && repoKnowledges.TryGetValue(it.DbId, out var newName) && k.Name != newName)
                            {
                                k.Name = newName; k.UpdatedAt = now;
                                _logger.LogInformation("ResolveConflict: knowledge {Id} → '{Name}' (keep_repo)", k.Id, newName);
                            }
                        }
                        else if (it.EntityType == "node")
                        {
                            var n = await _db.KNodes.FirstOrDefaultAsync(x => x.Id == it.DbId && x.DeletedAt == null);
                            if (n != null && repoNodeNames.TryGetValue(it.DbId, out var newName) && n.Name != newName)
                            {
                                n.Name = newName; n.UpdatedAt = now;
                                _logger.LogInformation("ResolveConflict: node {Id} → '{Name}' (keep_repo)", n.Id, newName);
                            }
                        }
                        else if (it.EntityType == "question")
                        {
                            var q = await _db.KQuestions.FirstOrDefaultAsync(x => x.Id == it.DbId && x.DeletedAt == null);
                            if (q == null) continue;
                            // Find the parsed question with matching id across all repo nodes
                            ParsedQuestion? match = null;
                            foreach (var arr in repoNodes.Values)
                            {
                                match = arr.FirstOrDefault(p => p.Id == it.DbId);
                                if (match != null) break;
                            }
                            if (match != null)
                            {
                                q.Name        = match.Question;
                                q.Description = match.Answer;
                                // Flip draft/active to match repo. The reconcile that follows
                                // would also toggle this, but doing it here keeps the
                                // keep_repo intent explicit for the audit log.
                                q.StatusCode  = match.IsDraft ? "draft" : "active";
                                q.UpdatedAt   = now;
                                _logger.LogInformation("ResolveConflict: question {Id} → '{Name}' draft={Draft} (keep_repo)",
                                    q.Id, match.Question, match.IsDraft);
                            }
                        }
                    }
                    await _db.SaveChangesAsync();
                }

                // Apply chosen "keep_repo" overrides to DB. Then run the full
                // repo→DB reconcile so that repo_only items get created in DB,
                // db_only stays (DB-wins by design), and modified items —
                // already aligned by the keep_repo loop above — pass through clean.
                // Daemon will force-push DB → remote afterwards so both sides match.
                var pullResult = await DoApplyRemoteChangesAsync(profile, userId);
                await PushStatusAsync(userId, pullResult.Success ? "synced" : "error", pullResult.Message,
                    pullResult.Success ? "" : "pull");
                return pullResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResolveConflicts failed for user {UserId}", userId);
                await PushStatusAsync(userId, "error", ex.Message, "push");
                return ResultOptions.Fail("Resolve conflicts failed: " + ex.Message, 500);
            }
        }

        /// <summary>
        /// Overwrites remote repo with DB content. No pull, no merge — just write files,
        /// commit, and force-push. The remote branch is completely replaced.
        ///
        /// <para><paramref name="ct"/> is checked at well-defined checkpoints (between DB load,
        /// fetch, file writes, commit, and push). Mid-libgit2sharp call cannot be cancelled —
        /// the next debounce will retry with the latest DB state.</para>
        /// </summary>
        private async Task<ResultOptions> DoForceUpdateRemoteAsync(UserProfile profile, CancellationToken ct = default)
        {
            var userId    = profile.UserId;
            var branch    = profile.KRepoBranch ?? "main";
            var localPath = GetLocalPath(userId);
            EnsureCloned(localPath, profile.KRepoUrl!, profile.KRepoPat!, branch);
            ct.ThrowIfCancellationRequested();

            // ── Build file map from DB (same as DoPushAsync) ─────────────────────
            var knowledges = (await _knowledgeRepo.GetAllKnowledgesByUserIdAsync(userId))
                .Where(k => k.DeletedAt == null).ToList();
            var fileMap = new Dictionary<string, string>();

            var knowledgeName = AssignUniqueNames(knowledges.Select(k => (k.Id, k.Name)));

            foreach (var k in knowledges)
            {
                var kUniq = knowledgeName[k.Id];
                var kDir  = $"Knowledge/{kUniq}";
                fileMap[$"{kDir}/_.md"] = BuildFrontMatter(k.Id, k.Name);

                var tree = await _knowledgeRepo.GetKnowledgeTreeAsync(k.Id, userId);
                if (tree == null) continue;

                var activeNodes = tree.Nodes.Where(n => n.DeletedAt == null).ToList();
                var childrenByParent = activeNodes.GroupBy(n => n.ParentId ?? 0)
                    .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Id).ToList());
                var hasChildren = childrenByParent.Keys.Where(id => id != 0).ToHashSet();

                var uniqueName   = new Dictionary<int, string>();
                var containerDir = new Dictionary<int, string>();
                var childDir     = new Dictionary<int, string>();

                void Assign(int parentId, string container, string? reservedSelf)
                {
                    if (!childrenByParent.TryGetValue(parentId, out var kids)) return;
                    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "_" };
                    if (reservedSelf != null) used.Add(reservedSelf);
                    foreach (var n in kids)
                    {
                        var name = MakeUnique(Sanitize(n.Name), used);
                        uniqueName[n.Id]   = name;
                        containerDir[n.Id] = container;
                        if (hasChildren.Contains(n.Id))
                        {
                            var dir = $"{container}/{name}";
                            childDir[n.Id] = dir;
                            Assign(n.Id, dir, name);
                        }
                    }
                }
                Assign(0, kDir, null);

                foreach (var node in activeNodes)
                {
                    var questions = await _questionRepo.GetQuestionsByNodeAsync(node.Id);
                    var active    = questions.Where(q => q.DeletedAt == null).ToList();
                    var content   = BuildFrontMatter(node.Id, node.Name) + BuildRepoMarkdown(active);
                    var name      = uniqueName[node.Id];
                    var filePath  = hasChildren.Contains(node.Id)
                        ? $"{childDir[node.Id]}/{name}.md"
                        : $"{containerDir[node.Id]}/{name}.md";
                    fileMap[filePath] = content;
                }
            }

            if (fileMap.Count == 0)
                return ResultOptions.Fail("No data in DB to push", 400);
            ct.ThrowIfCancellationRequested();

            _logger.LogInformation("ForceUpdate: writing {Count} files for user {UserId}", fileMap.Count, userId);

            var creds = BuildCredentials(profile.KRepoPat!);
            string repoWorkDir;

            // ── Fetch remote (no merge) so we have the tracking ref ──────────────
            using (var repo = new LibGit2Sharp.Repository(localPath))
            {
                repoWorkDir = repo.Info.WorkingDirectory.TrimEnd('/', '\\');
                var remote = repo.Network.Remotes["origin"];
                try
                {
                    repo.Network.Fetch(remote.Name,
                        remote.FetchRefSpecs.Select(r => r.Specification),
                        new FetchOptions { CredentialsProvider = creds });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ForceUpdate: fetch failed (will force-push anyway)");
                }
            }
            ct.ThrowIfCancellationRequested();

            // ── Clear Knowledge dir and write all DB files ───────────────────────
            var knowledgeRoot = Path.Combine(repoWorkDir, "Knowledge");
            if (Directory.Exists(knowledgeRoot))
                Directory.Delete(knowledgeRoot, recursive: true);
            Directory.CreateDirectory(knowledgeRoot);

            foreach (var (relPath, content) in fileMap)
            {
                var absPath = Path.Combine(repoWorkDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);
                await System.IO.File.WriteAllTextAsync(absPath, content, Encoding.UTF8, ct);
            }
            ct.ThrowIfCancellationRequested();

            // ── Stage, commit, force-push ────────────────────────────────────────
            using (var repo = new LibGit2Sharp.Repository(localPath))
            {
                // Ensure we're on the configured branch (clone may default to "main")
                var localBranch = repo.Branches[branch];
                if (localBranch == null)
                {
                    _logger.LogInformation("ForceUpdate: creating local branch '{Branch}' from HEAD", branch);
                    localBranch = repo.CreateBranch(branch);
                }
                if (!repo.Head.FriendlyName.Equals(branch, StringComparison.Ordinal))
                {
                    _logger.LogInformation("ForceUpdate: switching from '{Current}' to '{Target}'",
                        repo.Head.FriendlyName, branch);
                    Commands.Checkout(repo, localBranch);
                }

                // Set up tracking so push knows the upstream
                var remoteBranchRef = $"refs/remotes/origin/{branch}";
                if (repo.Branches[remoteBranchRef] != null)
                    repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranchRef);

                Commands.Stage(repo, "*");
                if (!repo.RetrieveStatus().IsDirty)
                {
                    _logger.LogInformation("ForceUpdate: files match HEAD — force-pushing existing commits");
                }
                else
                {
                    var sig = new Signature("SuperApp Sync", "sync@superapp.local", DateTimeOffset.UtcNow);
                    var commit = repo.Commit($"Force sync: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}", sig, sig);
                    _logger.LogInformation("ForceUpdate: committed {Sha}", commit.Sha[..8]);
                }

                // Force-push: "+" prefix on refspec forces the remote ref to match local
                _logger.LogInformation("ForceUpdate: pushing branch '{Branch}', HEAD={Sha}",
                    branch, repo.Head.Tip.Sha[..8]);

                var localSha = repo.Head.Tip.Sha;
                var remoteUrl = repo.Network.Remotes["origin"].Url;
                _logger.LogInformation("ForceUpdate: remote URL = {Url}", remoteUrl);

                repo.Network.Push(
                    repo.Network.Remotes["origin"],
                    $"+refs/heads/{branch}:refs/heads/{branch}",
                    new PushOptions { CredentialsProvider = creds });

                // ── Verify push actually landed ──────────────────────────────────
                repo.Network.Fetch(repo.Network.Remotes["origin"].Name,
                    repo.Network.Remotes["origin"].FetchRefSpecs.Select(r => r.Specification),
                    new FetchOptions { CredentialsProvider = creds });

                var remoteAfterPush = repo.Branches[$"refs/remotes/origin/{branch}"];
                var remoteSha = remoteAfterPush?.Tip?.Sha;

                _logger.LogInformation(
                    "ForceUpdate: local={LocalSha}, remote after push={RemoteSha}, match={Match}",
                    localSha[..8], remoteSha?[..8] ?? "NULL", localSha == remoteSha);

                if (remoteSha != localSha)
                    return ResultOptions.Fail(
                        $"Push did not update remote. Local={localSha[..8]}, remote={remoteSha?[..8] ?? "NULL"}. " +
                        "Check PAT permissions (needs write/push access) and repo URL.", 500);

                _logger.LogInformation("ForceUpdate: verified — remote matches local");

                var newHash = ComputeHash(fileMap);
                profile.KRepoLastPushSha  = localSha;
                profile.KRepoLastPushAt   = DateTime.UtcNow;
                profile.KRepoContentHash  = newHash;
                profile.KRepoStatusCode   = "synced";
                await _profileRepo.UpsertUserProfileAsync(profile);
            }

            return new ResultOptions { Success = true, Message = $"Force updated remote with {fileMap.Count} files" };
        }
    }
}
