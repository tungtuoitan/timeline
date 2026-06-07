namespace SuperAppServices.Services.K
{
    // ── Inputs ───────────────────────────────────────────────────────────────────

    /// <summary>A single question parsed from a node's _.md body.</summary>
    public sealed record ParsedQuestion(int? Id, string Question, string Answer, bool IsDraft, int? Order);

    /// <summary>
    /// A node as seen in the repo. <see cref="Path"/> is the node FOLDER path
    /// (e.g. "Knowledge/IT/Mạng"); <see cref="Id"/> comes from the _.md front-matter
    /// (null ⇒ new node); <see cref="Name"/> is the clean folder name.
    /// </summary>
    public sealed record RepoNodeFolder(string Path, int? Id, string Name, IReadOnlyList<ParsedQuestion> Questions);

    /// <summary>A knowledge folder. FolderKey = the folder name under "Knowledge/"; Id from _.md front-matter.</summary>
    public sealed record RepoKnowledgeFolder(string FolderKey, int? Id, string Name);

    public sealed record DbKnowledgeRef(int Id, string Name);
    public sealed record DbNodeRef(int Id, int KnowledgeId, int? ParentId, string Name);
    public sealed record DbQuestionRef(int Id, int NodeId, string Name, string? Description, bool IsDraft, int SortOrder);

    // ── Output ───────────────────────────────────────────────────────────────────

    public sealed record PlannedKnowledge(string FolderKey, string Name, int? ExistingId);

    /// <summary>
    /// FolderKey = the node folder path. KnowledgeFolderKey = owning knowledge folder
    /// name. ParentFolderKey = parent node's folder path (null = top-level). ExistingId
    /// null ⇒ create.
    /// </summary>
    public sealed record PlannedNode(
        string FolderKey, string Name, string KnowledgeFolderKey,
        string? ParentFolderKey, int? ExistingId);

    public sealed record PlannedQuestion(
        string NodeFolderKey, int? ExistingId, string Question,
        string Answer, bool IsDraft, int SortOrder);

    public sealed class ReconcilePlan
    {
        public List<PlannedKnowledge> Knowledges          { get; } = new();
        public List<int>              KnowledgeIdsToDelete { get; } = new();
        public List<PlannedNode>      Nodes               { get; } = new(); // parents first
        public List<int>              NodeIdsToDelete      { get; } = new();
        public List<PlannedQuestion>  Questions           { get; } = new();
        public List<int>              QuestionIdsToDelete  { get; } = new();
    }

    /// <summary>
    /// Pure, side-effect-free reconciliation planner for the K repo → DB sync.
    /// Identity no longer lives in folder names — it comes from the id stored in each
    /// folder's _.md front-matter (knowledge &amp; node) and the "[id:N]" tag of each
    /// question. Folder names are just clean display names; parent/child and ownership
    /// are read from the folder tree. Each id matches at most one DB entity (first
    /// occurrence in ordinal order wins); a duplicate id (a copy) becomes a new entity.
    /// </summary>
    public static class KRepoSyncPlanner
    {
        public static ReconcilePlan Plan(
            IReadOnlyList<RepoKnowledgeFolder> knowledgeFolders,
            IReadOnlyList<RepoNodeFolder> nodeFolders,
            IReadOnlyList<DbKnowledgeRef> knowledges,
            IReadOnlyList<DbNodeRef> nodes,
            IReadOnlyList<DbQuestionRef> questions)
        {
            var plan = new ReconcilePlan();

            var dbKnowledgeIds = knowledges.Select(k => k.Id).ToHashSet();
            var dbNodeIds      = nodes.Select(n => n.Id).ToHashSet();
            var dbQuestionIds  = questions.Select(q => q.Id).ToHashSet();

            // ── 1. Knowledges ─────────────────────────────────────────────────────
            var kfByKey = knowledgeFolders.ToDictionary(k => k.FolderKey, StringComparer.Ordinal);
            // Union folder entries with any knowledge referenced only by a node path.
            var knowledgeKeys = knowledgeFolders.Select(k => k.FolderKey)
                .Concat(nodeFolders.Select(f => Segments(f.Path)).Where(s => s.Length >= 2).Select(s => s[1]))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();

            var claimedKnowledgeIds = new HashSet<int>();
            foreach (var key in knowledgeKeys)
            {
                kfByKey.TryGetValue(key, out var kf);
                var id   = kf?.Id;
                var name = kf?.Name ?? key;
                if (id.HasValue && dbKnowledgeIds.Contains(id.Value) && claimedKnowledgeIds.Add(id.Value))
                    plan.Knowledges.Add(new PlannedKnowledge(key, name, id.Value)); // existing
                else
                    plan.Knowledges.Add(new PlannedKnowledge(key, name, null));     // create (incl. copy)
            }

            // ── 2. Nodes (parents first: shallow folders before deep) ─────────────
            var ordered = nodeFolders
                .Where(f => Segments(f.Path).Length >= 3) // Knowledge / <K> / <Node> …
                .OrderBy(f => Segments(f.Path).Length)
                .ThenBy(f => f.Path, StringComparer.Ordinal)
                .ToList();

            var claimedNodeIds = new HashSet<int>();
            foreach (var f in ordered)
            {
                var segs         = Segments(f.Path);
                var knowledgeKey = segs[1];
                var parentKey    = segs.Length == 3 ? null : string.Join("/", segs[..^1]);

                int? existingId = null;
                if (f.Id.HasValue && dbNodeIds.Contains(f.Id.Value) && claimedNodeIds.Add(f.Id.Value))
                    existingId = f.Id.Value;

                plan.Nodes.Add(new PlannedNode(f.Path, f.Name, knowledgeKey, parentKey, existingId));
            }

            // ── 3. Questions (dedup id globally; copies → create) ─────────────────
            var claimedQuestionIds = new HashSet<int>();
            foreach (var f in ordered)
            {
                for (var idx = 0; idx < f.Questions.Count; idx++)
                {
                    var pq = f.Questions[idx];
                    int? existingId = null;
                    if (pq.Id.HasValue && dbQuestionIds.Contains(pq.Id.Value) && claimedQuestionIds.Add(pq.Id.Value))
                        existingId = pq.Id.Value;

                    plan.Questions.Add(new PlannedQuestion(
                        f.Path, existingId, pq.Question, pq.Answer ?? "", pq.IsDraft, idx + 1));
                }
            }

            // ── 4. Deletes ────────────────────────────────────────────────────────
            foreach (var n in nodes)
                if (!claimedNodeIds.Contains(n.Id))
                    plan.NodeIdsToDelete.Add(n.Id);

            // Every knowledge folder keeps a placeholder _.md, so a present-but-empty
            // knowledge is still listed (claimed). Absence ⇒ real deletion.
            foreach (var k in knowledges)
                if (!claimedKnowledgeIds.Contains(k.Id))
                    plan.KnowledgeIdsToDelete.Add(k.Id);

            var deletedNodeIds = plan.NodeIdsToDelete.ToHashSet();
            foreach (var q in questions)
                if (!claimedQuestionIds.Contains(q.Id) && !deletedNodeIds.Contains(q.NodeId))
                    plan.QuestionIdsToDelete.Add(q.Id);

            return plan;
        }

        private static string[] Segments(string path) =>
            path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    }
}
