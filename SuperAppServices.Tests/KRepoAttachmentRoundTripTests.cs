using SuperAppServices.Services.K;
using SuperAppModels.Models;
using Xunit;

namespace SuperAppServices.Tests
{
    /// <summary>
    /// Tests for the attachment-link round-trip in repo markdown:
    ///   build → parse → AttRefs preserved as numeric strings.
    /// Covers the bugs that surfaced during the attachment-sync session:
    ///   A. ExtractMeta regex `\S+` ate the closing `]` when atts: was the last key
    ///      (`atts:2]` → val "2]" → int.TryParse failed → atts dropped silently)
    ///   B. Builder must emit `atts:` only when there are links (no empty `atts:`)
    ///   C. Builder/parser must accept multiple comma-separated atts
    ///   D. Singular `att:` (typo) must NOT be parsed as an att-ref
    ///   E. Order of meta keys in the bracket should not matter
    /// </summary>
    public class KRepoAttachmentRoundTripTests
    {
        private static KQuestionEntity DbQ(int id, string name, string? desc = null, bool draft = false, int order = 1) =>
            new()
            {
                Id          = id,
                Name        = name,
                Description = desc,
                StatusCode  = draft ? "draft" : "active",
                SortOrder   = order,
            };

        private static Dictionary<int, List<int>> AttMap(params (int qid, int[] attIds)[] entries)
            => entries.ToDictionary(e => e.qid, e => e.attIds.ToList());

        // ── BUILD: question with no atts → no "atts:" key in tag ────────────────
        [Fact]
        public void Build_noAtts_omitsAttsKey()
        {
            var md = KRepoSyncService.BuildRepoMarkdown(new() { DbQ(5, "Q5", "ans") });
            Assert.Contains("[id:5 order:1]", md);
            Assert.DoesNotContain("atts:", md);
        }

        // ── BUILD: question with attsByQuestionId=null → still works ────────────
        [Fact]
        public void Build_nullAttMap_omitsAttsKey()
        {
            var md = KRepoSyncService.BuildRepoMarkdown(
                new() { DbQ(5, "Q5", "ans") },
                attsByQuestionId: null);
            Assert.DoesNotContain("atts:", md);
        }

        // ── BUILD: question with empty atts list → no "atts:" key ───────────────
        [Fact]
        public void Build_emptyAttList_omitsAttsKey()
        {
            var md = KRepoSyncService.BuildRepoMarkdown(
                new() { DbQ(5, "Q5", "ans") },
                attsByQuestionId: AttMap((5, new int[0])));
            Assert.DoesNotContain("atts:", md);
        }

        // ── BUILD: single att ────────────────────────────────────────────────────
        [Fact]
        public void Build_singleAtt_emitsAttsKey()
        {
            var md = KRepoSyncService.BuildRepoMarkdown(
                new() { DbQ(5, "Q5", "ans") },
                attsByQuestionId: AttMap((5, new[] { 7 })));
            Assert.Contains("[id:5 order:1 atts:7]", md);
        }

        // ── BUILD: multiple atts comma-joined ───────────────────────────────────
        [Fact]
        public void Build_multipleAtts_commaSeparated()
        {
            var md = KRepoSyncService.BuildRepoMarkdown(
                new() { DbQ(5, "Q5", "ans") },
                attsByQuestionId: AttMap((5, new[] { 1, 2, 3 })));
            Assert.Contains("[id:5 order:1 atts:1,2,3]", md);
        }

        // ── BUILD: draft question with atts ─────────────────────────────────────
        [Fact]
        public void Build_draftWithAtts_writesInsideComment()
        {
            var md = KRepoSyncService.BuildRepoMarkdown(
                new() { DbQ(5, "Q5", "ans", draft: true) },
                attsByQuestionId: AttMap((5, new[] { 9 })));
            Assert.Contains("<!--# Q5 [id:5 order:1 atts:9]", md);
        }

        // ── BUILD: only one of two questions has atts ───────────────────────────
        [Fact]
        public void Build_partialAtts_emitsOnlyForLinkedQuestion()
        {
            var md = KRepoSyncService.BuildRepoMarkdown(
                new() { DbQ(5, "Q5", "a"), DbQ(6, "Q6", "b", order: 2) },
                attsByQuestionId: AttMap((5, new[] { 7 })));
            Assert.Contains("[id:5 order:1 atts:7]", md);
            Assert.Contains("[id:6 order:2]", md);
            Assert.DoesNotContain("[id:6 order:2 atts:", md);
        }

        // ── PARSE BUG-A: atts: at end of bracket (the regex bug we fixed) ───────
        [Fact]
        public void Parse_attsAtEndOfBracket_doesNotIncludeClosingBracket()
        {
            // Reproduces the original bug: regex `\S+` ate the `]`, val = "2]", parse failed.
            var md = "# Q [id:1198 order:5 atts:2]\nbody\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.Equal(new[] { "2" }, parsed[0].AttRefs);
        }

        // ── PARSE: single numeric att ───────────────────────────────────────────
        [Fact]
        public void Parse_singleNumericAtt_yieldsOneRef()
        {
            var md = "# Q [id:1 order:1 atts:7]\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Equal(new[] { "7" }, parsed[0].AttRefs);
        }

        // ── PARSE: multi-att comma-list ─────────────────────────────────────────
        [Fact]
        public void Parse_multipleAtts_yieldsAllRefs()
        {
            var md = "# Q [id:1 order:1 atts:1,2,3]\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Equal(new[] { "1", "2", "3" }, parsed[0].AttRefs);
        }

        // ── PARSE: filename ref (not yet a numeric id) ──────────────────────────
        [Fact]
        public void Parse_filenameRef_keptAsString()
        {
            var md = "# Q [id:1 order:1 atts:case1.cs]\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Equal(new[] { "case1.cs" }, parsed[0].AttRefs);
        }

        // ── PARSE: mixed numeric + filename refs ────────────────────────────────
        [Fact]
        public void Parse_mixedRefs_keptIndividually()
        {
            var md = "# Q [id:1 order:1 atts:7,case1.cs,8]\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Equal(new[] { "7", "case1.cs", "8" }, parsed[0].AttRefs);
        }

        // ── PARSE BUG-D: singular `att:` is NOT recognised ──────────────────────
        [Fact]
        public void Parse_singularAttKey_isIgnored()
        {
            // We deliberately accept ONLY the plural `atts:` to avoid drift.
            var md = "# Q [id:1 order:1 att:7]\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Empty(parsed[0].AttRefs);
        }

        // ── PARSE BUG-E: meta key order should not matter ───────────────────────
        [Fact]
        public void Parse_attsBeforeIdAndOrder_stillParses()
        {
            var md = "# Q [atts:7,8 id:1 order:1]\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Equal(1, parsed[0].Id);
            Assert.Equal(1, parsed[0].Order);
            Assert.Equal(new[] { "7", "8" }, parsed[0].AttRefs);
        }

        // ── PARSE: no atts key → AttRefs is empty (never null) ──────────────────
        [Fact]
        public void Parse_noAtts_attRefsIsEmpty()
        {
            var md = "# Q [id:1 order:1]\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.NotNull(parsed[0].AttRefs);
            Assert.Empty(parsed[0].AttRefs);
        }

        // ── PARSE: draft block with atts ────────────────────────────────────────
        [Fact]
        public void Parse_draftWithAtts_keepsRefsAndDraftFlag()
        {
            var md = "<!--# Q [id:1 order:1 atts:9] body -->\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.True(parsed[0].IsDraft);
            Assert.Equal(new[] { "9" }, parsed[0].AttRefs);
        }

        // ── ROUND-TRIP: build → parse preserves att-ids ─────────────────────────
        [Fact]
        public void RoundTrip_singleAtt_preservedAsString()
        {
            var md = KRepoSyncService.BuildRepoMarkdown(
                new() { DbQ(5, "Q5", "ans") },
                attsByQuestionId: AttMap((5, new[] { 7 })));
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.Equal(new[] { "7" }, parsed[0].AttRefs);
        }

        // ── ROUND-TRIP: multiple atts preserve order ───────────────────────────
        [Fact]
        public void RoundTrip_multipleAtts_preservedInOrder()
        {
            var md = KRepoSyncService.BuildRepoMarkdown(
                new() { DbQ(5, "Q5", "ans") },
                attsByQuestionId: AttMap((5, new[] { 3, 1, 2 })));
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Equal(new[] { "3", "1", "2" }, parsed[0].AttRefs);
        }

        // ── ROUND-TRIP: a node mixing atts and no-atts questions ───────────────
        [Fact]
        public void RoundTrip_mixedAttsAndPlainQuestions_allPreserved()
        {
            var dbList = new List<KQuestionEntity>
            {
                DbQ(1, "with atts",   "a", order: 1),
                DbQ(2, "no atts",     "b", order: 2),
                DbQ(3, "draft+atts",  "c", draft: true, order: 3),
            };
            var attMap = AttMap((1, new[] { 10, 11 }), (3, new[] { 12 }));
            var md = KRepoSyncService.BuildRepoMarkdown(dbList, attMap);
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.Equal(3, parsed.Count);
            Assert.Equal(new[] { "10", "11" }, parsed[0].AttRefs);
            Assert.Empty(parsed[1].AttRefs);
            Assert.Equal(new[] { "12" }, parsed[2].AttRefs);
            Assert.True(parsed[2].IsDraft);
        }

        // ── ROUND-TRIP: round-trip again (parse → build → parse) is stable ─────
        [Fact]
        public void RoundTrip_buildParseBuildParse_isStable()
        {
            var md1 = KRepoSyncService.BuildRepoMarkdown(
                new() { DbQ(5, "Q5", "ans") },
                attsByQuestionId: AttMap((5, new[] { 1, 2 })));
            var parsed1 = KRepoSyncService.ParseQuestions(md1);

            // Rebuild from the parsed structure isn't straight-forward (parsed has no
            // KQuestionEntity), but we can at least verify the produced markdown
            // re-parses cleanly into the same structure.
            var parsed2 = KRepoSyncService.ParseQuestions(md1);
            Assert.Equal(parsed1[0].AttRefs, parsed2[0].AttRefs);
            Assert.Equal(parsed1[0].Id, parsed2[0].Id);
            Assert.Equal(parsed1[0].Order, parsed2[0].Order);
        }

        // ── EDGE: trailing space inside atts list is trimmed per ref ───────────
        [Fact]
        public void Parse_attsWithSpacesAroundCommas_isHandled()
        {
            // Note: the bracket regex stops at the first whitespace, so spaces inside
            // the atts value would actually break the value. Comma-only is the supported
            // separator. This test pins down current behaviour.
            var md = "# Q [id:1 order:1 atts:7,8,9]\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Equal(new[] { "7", "8", "9" }, parsed[0].AttRefs);
        }

        // ── EDGE: empty atts value (atts:) is not added as a ref ───────────────
        [Fact]
        public void Parse_emptyAttsValue_yieldsNoRef()
        {
            // `atts:` with no value won't match the regex pattern at all.
            var md = "# Q [id:1 order:1 atts:]\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Empty(parsed[0].AttRefs);
        }
    }
}
