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

        // Single questions/placeholder file inside every knowledge & node folder.
        private const string DetailFile = "_.md";

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
                Branch        = profile.KRepoBranch ?? "K",
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

            // Step 1: push DB → repo first (DB is source of truth)
            await PushStatusAsync(userId, "pushing", "Pushing DB → repo before pull...", "push");
            var pushResult = await DoPushAsync(profile);
            if (!pushResult.Success)
            {
                var isConflict = pushResult.Status == 409;
                await PushStatusAsync(userId, isConflict ? "conflict" : "error", pushResult.Message, "push");
                await SaveStatusAsync(profile, isConflict ? "conflict" : "error");
                return pushResult;
            }

            // Step 2: parse remote changes and apply to DB
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
                EnsureCloned(localPath, profile.KRepoUrl, profile.KRepoPat, profile.KRepoBranch ?? "K");

                using var repo = new LibGit2Sharp.Repository(localPath);
                var creds = BuildCredentials(profile.KRepoPat);

                Remote remote = repo.Network.Remotes["origin"];
                repo.Network.Fetch(remote.Name, remote.FetchRefSpecs.Select(r => r.Specification), new FetchOptions { CredentialsProvider = creds });

                var remoteBranch = repo.Branches[$"refs/remotes/origin/{profile.KRepoBranch ?? "K"}"];
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
            var branch    = profile.KRepoBranch ?? "K";
            var localPath = GetLocalPath(userId);
            EnsureCloned(localPath, profile.KRepoUrl!, profile.KRepoPat!, branch);

            // Load all knowledges + nodes + questions
            var knowledges = (await _knowledgeRepo.GetAllKnowledgesByUserIdAsync(userId))
                .Where(k => k.DeletedAt == null).ToList();
            var fileMap    = new Dictionary<string, string>(); // repo-relative path → content

            // Folder names are clean (no "[id]"); the id lives in the _.md front-matter.
            // Give sibling folders unique names so two entities sharing a name never
            // collide on disk: "Misc", "Misc (2)", "Misc (3)", …
            var knowledgeFolder = AssignUniqueNames(knowledges.Select(k => (k.Id, k.Name)));

            foreach (var k in knowledges)
            {
                var kDir = $"Knowledge/{knowledgeFolder[k.Id]}";
                // Placeholder _.md keeps the knowledge folder tracked even when empty.
                fileMap[$"{kDir}/{DetailFile}"] = BuildFrontMatter(k.Id, k.Name);

                var tree = await _knowledgeRepo.GetKnowledgeTreeAsync(k.Id, userId);
                if (tree == null) continue;

                var activeNodes = tree.Nodes.Where(n => n.DeletedAt == null).ToList();
                var nodeIndex   = activeNodes.ToDictionary(n => n.Id);

                // Unique folder names per sibling group (same parent within this knowledge).
                var nodeFolder = new Dictionary<int, string>();
                foreach (var group in activeNodes.GroupBy(n => n.ParentId))
                {
                    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var n in group.OrderBy(n => n.Id))
                        nodeFolder[n.Id] = MakeUnique(Sanitize(n.Name), used);
                }

                foreach (var node in activeNodes)
                {
                    var questions = await _questionRepo.GetQuestionsByNodeAsync(node.Id);
                    var active    = questions.Where(q => q.DeletedAt == null).ToList();
                    // Front-matter (id + name) then the questions markdown.
                    var content   = BuildFrontMatter(node.Id, node.Name) + BuildRepoMarkdown(active);
                    var dir       = BuildNodeDir(kDir, node, nodeIndex, nodeFolder);
                    fileMap[$"{dir}/{DetailFile}"] = content;
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

            // ── Pull latest (close repo after so handles are released on Windows) ──
            using (var repo = new LibGit2Sharp.Repository(localPath))
            {
                repoWorkDir = repo.Info.WorkingDirectory.TrimEnd('/', '\\');

                MergeResult pullResult;
                try
                {
                    pullResult = Commands.Pull(repo,
                        new Signature("superapp", "superapp@local", DateTimeOffset.UtcNow),
                        new PullOptions { FetchOptions = new FetchOptions { CredentialsProvider = creds } });
                }
                catch (Exception ex)
                {
                    return ResultOptions.Fail("Pull failed: " + ex.Message, 409);
                }

                if (pullResult.Status == MergeStatus.Conflicts)
                    return ResultOptions.Fail("Conflict: remote has diverged. Resolve in git and retry.", 409);
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
                Commands.Stage(repo, "*");
                if (!repo.RetrieveStatus().IsDirty)
                {
                    profile.KRepoContentHash = newHash;
                    await _profileRepo.UpsertUserProfileAsync(profile);
                    return new ResultOptions { Success = true, Message = "No changes after write" };
                }

                var sig    = new Signature("SuperApp Sync", "sync@superapp.local", DateTimeOffset.UtcNow);
                var commit = repo.Commit($"DB sync: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}", sig, sig);

                repo.Network.Push(repo.Branches[branch], new PushOptions { CredentialsProvider = creds });

                profile.KRepoLastPushSha  = commit.Sha;
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
            var branch = profile.KRepoBranch ?? "K";

            Remote remote = repo.Network.Remotes["origin"];
            repo.Network.Fetch(remote.Name, remote.FetchRefSpecs.Select(r => r.Specification),
                new FetchOptions { CredentialsProvider = creds });

            var remoteBranch = repo.Branches[$"refs/remotes/origin/{branch}"];
            if (remoteBranch == null)
                return ResultOptions.Fail("Remote branch not found.", 400);

            var remoteHead = remoteBranch.Tip;
            var now = DateTime.UtcNow;

            // ── 1. Read repo: every node FOLDER + the questions in its detail.md ──
            var dirDetail = new Dictionary<string, string?>(StringComparer.Ordinal);
            WalkTree(remoteHead.Tree, "", dirDetail);

            // Knowledge folder = directory exactly 2 segments deep (Knowledge/<K>); it
            // carries a placeholder _.md (with the id) so even empty knowledges are tracked.
            var knowledgeFolders = dirDetail
                .Where(kv => kv.Key.Split('/', StringSplitOptions.RemoveEmptyEntries).Length == 2)
                .Select(kv =>
                {
                    var seg = kv.Key.Split('/', StringSplitOptions.RemoveEmptyEntries)[1];
                    var (id, _, _) = ParseFrontMatter(kv.Value ?? "");
                    return new RepoKnowledgeFolder(seg, id, seg);
                })
                .ToList();

            // Node folder = any directory ≥3 segments deep: Knowledge/<K>/<Node>… Identity
            // (id) comes from its _.md front-matter; the display name is the folder name.
            var nodeFolders = dirDetail
                .Where(kv => kv.Key.Split('/', StringSplitOptions.RemoveEmptyEntries).Length >= 3)
                .Select(kv =>
                {
                    var segs = kv.Key.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var (id, _, body) = ParseFrontMatter(kv.Value ?? "");
                    return new RepoNodeFolder(kv.Key, id, segs[^1], ParseQuestions(body));
                })
                .ToList();

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

            // ── 7. Soft-delete nodes & knowledges removed from the repo ───────────
            foreach (var nodeId in plan.NodeIdsToDelete)
            {
                if (!dbNodeMap.TryGetValue(nodeId, out var node)) continue;
                var qIds = await _db.KQuestions
                    .Where(q => q.NodeId == nodeId && q.DeletedAt == null)
                    .Select(q => q.Id).ToListAsync();
                if (qIds.Any()) await _questionRepo.DeleteQuestionsAsync(qIds);
                node.DeletedAt = now;
                node.UpdatedAt = now;
                _logger.LogInformation("Reconcile: soft-deleted node {Id}", nodeId);
            }
            await _db.SaveChangesAsync();

            foreach (var kid in plan.KnowledgeIdsToDelete)
            {
                if (!dbKnowledgeMap.TryGetValue(kid, out var k)) continue;
                var knodes = await _db.KNodes.Where(n => n.KnowledgeId == kid && n.DeletedAt == null).ToListAsync();
                foreach (var n in knodes)
                {
                    var qIds = await _db.KQuestions
                        .Where(q => q.NodeId == n.Id && q.DeletedAt == null)
                        .Select(q => q.Id).ToListAsync();
                    if (qIds.Any()) await _questionRepo.DeleteQuestionsAsync(qIds);
                    n.DeletedAt = now;
                    n.UpdatedAt = now;
                }
                k.DeletedAt = now;
                k.UpdatedAt = now;
                _logger.LogInformation("Reconcile: soft-deleted knowledge {Id} (+{Count} nodes)", kid, knodes.Count);
            }
            await _db.SaveChangesAsync();

            // Push back so newly-created ids / detail.md files land in the repo.
            return await DoPushAsync(profile);
        }

        // Walks the whole tree, recording every directory and the text of any detail.md
        // it directly contains. dirDetail[dirPath] = detail.md content (null if none).
        private static void WalkTree(Tree tree, string prefix, Dictionary<string, string?> dirDetail)
        {
            foreach (var entry in tree)
            {
                var fullPath = string.IsNullOrEmpty(prefix) ? entry.Name : $"{prefix}/{entry.Name}";
                if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    if (!dirDetail.ContainsKey(fullPath)) dirDetail[fullPath] = null;
                    WalkTree((Tree)entry.Target, fullPath, dirDetail);
                }
                else if (entry.TargetType == TreeEntryTargetType.Blob
                         && entry.Name.Equals(DetailFile, StringComparison.OrdinalIgnoreCase)
                         && !string.IsNullOrEmpty(prefix))
                {
                    dirDetail[prefix] = ((Blob)entry.Target).GetContentText(Encoding.UTF8);
                }
            }
        }

        // ── Markdown builder (C# port of kMarkdownEditor.utils.ts) ───────────────

        private static string BuildRepoMarkdown(List<KQuestionEntity> questions)
        {
            if (!questions.Any()) return string.Empty;

            var sb = new StringBuilder();
            var sorted = questions.OrderBy(q => q.SortOrder).ToList();

            foreach (var q in sorted)
            {
                var tag = $"[id:{q.Id} order:{q.SortOrder}]";
                if (q.StatusCode == "draft")
                {
                    var answer = q.Description?.Trim();
                    if (!string.IsNullOrEmpty(answer))
                    {
                        sb.AppendLine($"<!--# {q.Name} {tag}");
                        sb.AppendLine($"{answer} -->");
                    }
                    else
                    {
                        sb.AppendLine($"<!--# {q.Name} {tag} -->");
                    }
                }
                else
                {
                    sb.AppendLine($"# {q.Name} {tag}");
                    if (!string.IsNullOrEmpty(q.Description?.Trim()))
                        sb.AppendLine(q.Description!.Trim());
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        // Builds a node's folder path (no trailing file): "<knowledgeDir>/<anc>/…/<node>".
        // Uses the precomputed unique sibling folder names so two nodes sharing a name
        // never collide on disk.
        private static string BuildNodeDir(
            string knowledgeDir, KNodeEntity node,
            Dictionary<int, KNodeEntity> nodeIndex, Dictionary<int, string> nodeFolder)
        {
            var parts = new List<string>();
            var current = node;
            while (current != null)
            {
                parts.Add(nodeFolder[current.Id]);
                current = current.ParentId.HasValue && nodeIndex.TryGetValue(current.ParentId.Value, out var p) ? p : null;
            }
            parts.Reverse();
            return string.Join("/", new[] { knowledgeDir }.Concat(parts));
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

        private static List<ParsedQuestion> ParseQuestions(string body)
        {
            var result = new List<ParsedQuestion>();
            string? question = null, answer = null;
            bool isDraft = false;
            int? id = null, order = null;
            bool inDraft = false;

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
    }
}
