using SuperAppServices.Services.K;
using Xunit;

namespace SuperAppServices.Tests
{
    /// <summary>
    /// Unit tests for the pure repo→DB reconciliation planner. No DB/git — every case is
    /// a repo folder tree + a DB snapshot, asserting the produced plan.
    /// Layout: Knowledge/&lt;K&gt;/&lt;Node&gt;/_.md ; folder names are clean (no "[id]"),
    /// identity comes from the id passed in (read from each _.md front-matter at runtime).
    /// </summary>
    public class KRepoSyncPlannerTests
    {
        // ── builders ──────────────────────────────────────────────────────────────
        private static ParsedQuestion Q(int? id, string q, string a = "", bool draft = false) => new(id, q, a, draft, null);
        private static RepoNodeFolder NF(string path, int? id, string name, params ParsedQuestion[] qs) => new(path, id, name, qs);
        private static RepoKnowledgeFolder KF(string key, int? id, string name) => new(key, id, name);
        private static DbKnowledgeRef K(int id, string name) => new(id, name);
        private static DbNodeRef N(int id, int kid, int? parent, string name) => new(id, kid, parent, name);
        private static DbQuestionRef DQ(int id, int nodeId, string name, string? desc = "", bool draft = false, int order = 1) => new(id, nodeId, name, desc, draft, order);

        private static ReconcilePlan Plan(
            IReadOnlyList<RepoKnowledgeFolder> kfs,
            IReadOnlyList<RepoNodeFolder> folders,
            IReadOnlyList<DbKnowledgeRef> ks,
            IReadOnlyList<DbNodeRef> ns,
            IReadOnlyList<DbQuestionRef>? qs = null)
            => KRepoSyncPlanner.Plan(kfs, folders, ks, ns, qs ?? new List<DbQuestionRef>());

        private static PlannedNode Node(ReconcilePlan p, string key) => p.Nodes.Single(n => n.FolderKey == key);
        private static PlannedKnowledge Kn(ReconcilePlan p, string key) => p.Knowledges.Single(k => k.FolderKey == key);

        // ── NODE: move across knowledge (the originally-reported bug) ───────────────
        [Fact]
        public void MoveNodeCrossKnowledge_keepsId_setsNewKnowledge()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT"), KF("B", 1142, "B") },
                folders: new[] { NF("Knowledge/IT/5", 2536, "5"), NF("Knowledge/B/6", 2540, "6") },
                ks: new[] { K(1141, "IT"), K(1142, "B") },
                ns: new[] { N(2536, 1142, null, "5"), N(2540, 1142, null, "6") });

            var n = Node(plan, "Knowledge/IT/5");
            Assert.Equal(2536, n.ExistingId);
            Assert.Equal("IT", n.KnowledgeFolderKey);
            Assert.Null(n.ParentFolderKey);
            Assert.DoesNotContain(1142, plan.KnowledgeIdsToDelete); // B still present
            Assert.Empty(plan.NodeIdsToDelete);
        }

        // ── NODE: move parent within a knowledge ────────────────────────────────────
        [Fact]
        public void MoveNodeParent_setsParentFolderKey()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[] { NF("Knowledge/IT/P", 100, "P"), NF("Knowledge/IT/P/C", 200, "C") },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "P"), N(200, 1141, null, "C") });

            var c = Node(plan, "Knowledge/IT/P/C");
            Assert.Equal(200, c.ExistingId);
            Assert.Equal("Knowledge/IT/P", c.ParentFolderKey);
        }

        // ── NODE: create (folder with no id in its _.md) ────────────────────────────
        [Fact]
        public void CreateNode_noId_isCreate()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[] { NF("Knowledge/IT/P", 100, "P"), NF("Knowledge/IT/Brand New", null, "Brand New") },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "P") });

            var n = Node(plan, "Knowledge/IT/Brand New");
            Assert.Null(n.ExistingId);
            Assert.Equal("Brand New", n.Name);
            Assert.Equal("IT", n.KnowledgeFolderKey);
            Assert.Null(n.ParentFolderKey);
        }

        // ── NODE: rename (folder renamed, id in _.md unchanged) ─────────────────────
        [Fact]
        public void RenameNode_keepsId_newName()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[] { NF("Knowledge/IT/New Name", 100, "New Name") },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "Old Name") });

            var n = Node(plan, "Knowledge/IT/New Name");
            Assert.Equal(100, n.ExistingId);
            Assert.Equal("New Name", n.Name);
        }

        // ── NODE: delete (gone from repo) ───────────────────────────────────────────
        [Fact]
        public void DeleteNode_whenAbsentFromRepo()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[] { NF("Knowledge/IT/A", 100, "A") },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "A"), N(200, 1141, null, "B") });

            Assert.Contains(200, plan.NodeIdsToDelete);
            Assert.DoesNotContain(100, plan.NodeIdsToDelete);
        }

        // ── NODE: copy (same id in two _.md) ⇒ first keeps id, second is new ────────
        [Fact]
        public void CopyNode_duplicateId_secondIsNew()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[] { NF("Knowledge/IT/A", 100, "A"), NF("Knowledge/IT/A copy", 100, "A copy") },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "A") });

            Assert.Equal(100, Node(plan, "Knowledge/IT/A").ExistingId);
            Assert.Null(Node(plan, "Knowledge/IT/A copy").ExistingId);
            Assert.Empty(plan.NodeIdsToDelete);
        }

        // ── NODE: folder without questions ⇒ still a node, zero questions ───────────
        [Fact]
        public void NodeFolderWithoutQuestions_isNode()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[] { NF("Knowledge/IT/A", 100, "A") },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "A") });

            Assert.Equal(100, Node(plan, "Knowledge/IT/A").ExistingId);
            Assert.Empty(plan.Questions);
            Assert.Empty(plan.QuestionIdsToDelete);
        }

        // ── KNOWLEDGE: create (folder _.md without id) + its node ───────────────────
        [Fact]
        public void CreateKnowledge_withNode()
        {
            var plan = Plan(
                kfs: new[] { KF("NewK", null, "NewK") },
                folders: new[] { NF("Knowledge/NewK/Topic", null, "Topic") },
                ks: new List<DbKnowledgeRef>(),
                ns: new List<DbNodeRef>());

            Assert.Null(Kn(plan, "NewK").ExistingId);
            var n = Node(plan, "Knowledge/NewK/Topic");
            Assert.Null(n.ExistingId);
            Assert.Equal("NewK", n.KnowledgeFolderKey);
            Assert.Null(n.ParentFolderKey);
        }

        // ── KNOWLEDGE: rename (folder renamed, id in _.md unchanged) ────────────────
        [Fact]
        public void RenameKnowledge_keepsId_newName()
        {
            var plan = Plan(
                kfs: new[] { KF("NewK", 1141, "NewK") },
                folders: new[] { NF("Knowledge/NewK/A", 100, "A") },
                ks: new[] { K(1141, "OldK") },
                ns: new[] { N(100, 1141, null, "A") });

            var k = Kn(plan, "NewK");
            Assert.Equal(1141, k.ExistingId);
            Assert.Equal("NewK", k.Name);
        }

        // ── KNOWLEDGE: deleted only when its folder is gone from the repo ───────────
        [Fact]
        public void DeleteKnowledge_whenFolderAbsent_keepWhenPresent()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },              // 1141 present, 1142 absent
                folders: new[] { NF("Knowledge/IT/A", 100, "A") },
                ks: new[] { K(1141, "IT"), K(1142, "Gone") },
                ns: new[] { N(100, 1141, null, "A") });

            Assert.DoesNotContain(1141, plan.KnowledgeIdsToDelete);
            Assert.Contains(1142, plan.KnowledgeIdsToDelete);
        }

        // ── KNOWLEDGE: empty knowledge kept alive by its placeholder folder ─────────
        [Fact]
        public void EmptyKnowledge_withPlaceholderFolder_notDeleted()
        {
            var plan = Plan(
                kfs: new[] { KF("Empty", 1142, "Empty") },        // present, but no node folders
                folders: new List<RepoNodeFolder>(),
                ks: new[] { K(1142, "Empty") },
                ns: new List<DbNodeRef>());

            Assert.DoesNotContain(1142, plan.KnowledgeIdsToDelete);
            Assert.Empty(plan.NodeIdsToDelete);
        }

        // ── KNOWLEDGE: create an empty knowledge from a brand-new placeholder folder ─
        [Fact]
        public void CreateEmptyKnowledge_fromPlaceholderFolder()
        {
            var plan = Plan(
                kfs: new[] { KF("NewEmpty", null, "NewEmpty") },
                folders: new List<RepoNodeFolder>(),
                ks: new List<DbKnowledgeRef>(),
                ns: new List<DbNodeRef>());

            Assert.Null(Kn(plan, "NewEmpty").ExistingId);
        }

        // ── KNOWLEDGE: copy (same id in two folder _.md) ⇒ second is new ────────────
        [Fact]
        public void CopyKnowledge_duplicateId_secondIsNew()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT"), KF("IT copy", 1141, "IT copy") },
                folders: new[] { NF("Knowledge/IT/A", 100, "A"), NF("Knowledge/IT copy/B", null, "B") },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "A") });

            Assert.Equal(1141, Kn(plan, "IT").ExistingId);
            Assert.Null(Kn(plan, "IT copy").ExistingId);
        }

        // ── QUESTION: create / update / copy / draft in one node ────────────────────
        [Fact]
        public void Questions_create_update_copy_draft()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[]
                {
                    NF("Knowledge/IT/A", 100, "A",
                        Q(10, "Q10 edited"),       // update
                        Q(10, "copy of 10"),       // duplicate id ⇒ create (copy)
                        Q(null, "brand new"),      // create
                        Q(11, "Q11", draft: true)) // existing, draft
                },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "A") },
                qs: new[] { DQ(10, 100, "Q10"), DQ(11, 100, "Q11", order: 2) });

            var qsForNode = plan.Questions.Where(q => q.NodeFolderKey == "Knowledge/IT/A").ToList();
            Assert.Single(qsForNode, q => q.ExistingId == 10);
            Assert.Equal(2, qsForNode.Count(q => q.ExistingId == null));
            Assert.Single(qsForNode, q => q.ExistingId == 11 && q.IsDraft);
            Assert.Empty(plan.QuestionIdsToDelete);
        }

        // ── QUESTION: move across nodes (same knowledge) ────────────────────────────
        [Fact]
        public void Question_moveAcrossNodes()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[]
                {
                    NF("Knowledge/IT/A", 100, "A", Q(20, "moved q")),
                    NF("Knowledge/IT/B", 200, "B"),
                },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "A"), N(200, 1141, null, "B") },
                qs: new[] { DQ(20, 200, "moved q") });

            var q = plan.Questions.Single(x => x.ExistingId == 20);
            Assert.Equal("Knowledge/IT/A", q.NodeFolderKey);
            Assert.DoesNotContain(20, plan.QuestionIdsToDelete);
        }

        // ── QUESTION: move across knowledges (file→file, different knowledge) ───────
        [Fact]
        public void Question_moveAcrossKnowledges()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT"), KF("B", 1142, "B") },
                folders: new[]
                {
                    NF("Knowledge/IT/A", 100, "A", Q(20, "moved")),
                    NF("Knowledge/B/X", 200, "X"),
                },
                ks: new[] { K(1141, "IT"), K(1142, "B") },
                ns: new[] { N(100, 1141, null, "A"), N(200, 1142, null, "X") },
                qs: new[] { DQ(20, 200, "moved") });

            var q = plan.Questions.Single(x => x.ExistingId == 20);
            Assert.Equal("Knowledge/IT/A", q.NodeFolderKey);
            Assert.DoesNotContain(20, plan.QuestionIdsToDelete);
        }

        // ── QUESTION: reorder ───────────────────────────────────────────────────────
        [Fact]
        public void Questions_reorder_setsSortOrderByPosition()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[] { NF("Knowledge/IT/A", 100, "A", Q(11, "Q11"), Q(10, "Q10")) },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "A") },
                qs: new[] { DQ(10, 100, "Q10", order: 1), DQ(11, 100, "Q11", order: 2) });

            Assert.Equal(1, plan.Questions.Single(q => q.ExistingId == 11).SortOrder);
            Assert.Equal(2, plan.Questions.Single(q => q.ExistingId == 10).SortOrder);
        }

        // ── QUESTION: delete all (node kept) ────────────────────────────────────────
        [Fact]
        public void DeleteAllQuestions_keepsNode()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[] { NF("Knowledge/IT/A", 100, "A") },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(100, 1141, null, "A") },
                qs: new[] { DQ(10, 100, "Q10"), DQ(11, 100, "Q11", order: 2) });

            Assert.Contains(10, plan.QuestionIdsToDelete);
            Assert.Contains(11, plan.QuestionIdsToDelete);
            Assert.DoesNotContain(100, plan.NodeIdsToDelete);
        }

        // ── COMBO: create new knowledge + move existing node into it ────────────────
        [Fact]
        public void Combo_createKnowledge_moveExistingNodeInto()
        {
            var plan = Plan(
                kfs: new[] { KF("NewK", null, "NewK") },
                folders: new[] { NF("Knowledge/NewK/5", 2536, "5") },
                ks: new[] { K(1142, "B") },
                ns: new[] { N(2536, 1142, null, "5") });

            Assert.Null(Kn(plan, "NewK").ExistingId);
            var n = Node(plan, "Knowledge/NewK/5");
            Assert.Equal(2536, n.ExistingId);
            Assert.Equal("NewK", n.KnowledgeFolderKey);
            Assert.Contains(1142, plan.KnowledgeIdsToDelete); // source folder absent ⇒ deleted
        }

        // ── COMBO: move node under a parent that is itself newly created ────────────
        [Fact]
        public void Combo_moveNodeUnderNewlyCreatedParent_ordersParentFirst()
        {
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[] { NF("Knowledge/IT/NewP", null, "NewP"), NF("Knowledge/IT/NewP/C", 200, "C") },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(200, 1141, null, "C") });

            var idxP = plan.Nodes.FindIndex(n => n.FolderKey == "Knowledge/IT/NewP");
            var idxC = plan.Nodes.FindIndex(n => n.FolderKey == "Knowledge/IT/NewP/C");
            Assert.True(idxP < idxC, "parent must be planned before child");

            Assert.Null(Node(plan, "Knowledge/IT/NewP").ExistingId);
            var c = Node(plan, "Knowledge/IT/NewP/C");
            Assert.Equal(200, c.ExistingId);
            Assert.Equal("Knowledge/IT/NewP", c.ParentFolderKey);
        }

        // ── COMBO: reverse a parent/child relationship (A↔B) ────────────────────────
        [Fact]
        public void ReverseParentChild_swapsHierarchy()
        {
            // Before: A(1) top-level, B(2) child of A. After: B(2) top-level, A(1) child of B.
            var plan = Plan(
                kfs: new[] { KF("IT", 1141, "IT") },
                folders: new[] { NF("Knowledge/IT/B", 2, "B"), NF("Knowledge/IT/B/A", 1, "A") },
                ks: new[] { K(1141, "IT") },
                ns: new[] { N(1, 1141, null, "A"), N(2, 1141, 1, "B") });

            var idxB = plan.Nodes.FindIndex(n => n.FolderKey == "Knowledge/IT/B");
            var idxA = plan.Nodes.FindIndex(n => n.FolderKey == "Knowledge/IT/B/A");
            Assert.True(idxB < idxA, "new top node B before its new child A");

            Assert.Null(Node(plan, "Knowledge/IT/B").ParentFolderKey);
            Assert.Equal(2, Node(plan, "Knowledge/IT/B").ExistingId);

            var a = Node(plan, "Knowledge/IT/B/A");
            Assert.Equal(1, a.ExistingId);
            Assert.Equal("Knowledge/IT/B", a.ParentFolderKey);
        }

        // ── LogicalPath: ".md" file → logical entity path (leaf vs self-file) ───────
        [Theory]
        [InlineData("Knowledge/IT/_.md", "Knowledge/IT")]                  // knowledge self-file (_.md)
        [InlineData("Knowledge/IT/Mạng.md", "Knowledge/IT/Mạng")]         // leaf node under knowledge
        [InlineData("Knowledge/IT/TCP/TCP.md", "Knowledge/IT/TCP")]        // non-leaf node self-file
        [InlineData("Knowledge/IT/TCP/Handshake.md", "Knowledge/IT/TCP/Handshake")] // leaf under non-leaf
        public void LogicalPath_mapsFileToEntityPath(string file, string expected)
        {
            var logical = KRepoSyncPlanner.LogicalPath(file);
            Assert.NotNull(logical);
            Assert.Equal(expected, string.Join("/", logical!));
        }

        [Fact]
        public void LogicalPath_returnsNull_forTooShallowOrNonMd()
        {
            Assert.Null(KRepoSyncPlanner.LogicalPath("Knowledge"));
            Assert.Null(KRepoSyncPlanner.LogicalPath("Knowledge/IT/note.txt"));
        }

        // ── CREATE: everything brand-new and nested ─────────────────────────────────
        [Fact]
        public void CreateAll_nested_isAllNew()
        {
            var plan = Plan(
                kfs: new[] { KF("NewK", null, "NewK") },
                folders: new[]
                {
                    NF("Knowledge/NewK/Parent", null, "Parent"),
                    NF("Knowledge/NewK/Parent/Child", null, "Child", Q(null, "new q")),
                },
                ks: new List<DbKnowledgeRef>(),
                ns: new List<DbNodeRef>());

            Assert.Null(Kn(plan, "NewK").ExistingId);
            Assert.Null(Node(plan, "Knowledge/NewK/Parent").ExistingId);
            Assert.Null(Node(plan, "Knowledge/NewK/Parent").ParentFolderKey);

            var child = Node(plan, "Knowledge/NewK/Parent/Child");
            Assert.Null(child.ExistingId);
            Assert.Equal("Knowledge/NewK/Parent", child.ParentFolderKey);

            var idxP = plan.Nodes.FindIndex(n => n.FolderKey == "Knowledge/NewK/Parent");
            var idxC = plan.Nodes.FindIndex(n => n.FolderKey == "Knowledge/NewK/Parent/Child");
            Assert.True(idxP < idxC, "parent created before child");

            Assert.Null(plan.Questions.Single(x => x.NodeFolderKey == "Knowledge/NewK/Parent/Child").ExistingId);
        }
    }
}
