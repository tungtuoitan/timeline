using SuperAppServices.Services.K;
using SuperAppModels.Models;
using Xunit;

namespace SuperAppServices.Tests
{
    /// <summary>
    /// Round-trip + compare tests for question markdown. These cover the bugs that
    /// surfaced during the K-repo-sync stabilisation session:
    ///   1. q.Name with embedded newline → flattened on write so parser doesn't split it
    ///   2. comparer must flatten DB name (so it matches what the builder wrote)
    ///   3. parser tolerates "&lt;!--" on its own line with the heading on the next line
    ///   4. comparer must check draft status, not just text
    ///   5. comparer must trim trailing per-line whitespace (parser strips it on read)
    ///   6. round-trip produces identical compare-equal result
    /// </summary>
    public class KRepoMarkdownRoundTripTests
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

        // ── FlattenLine: collapses any whitespace run (CRLF, LF, tabs, multi-space) ─
        [Theory]
        [InlineData("",                    "")]
        [InlineData("hello",               "hello")]
        [InlineData("a\nb",                "a b")]
        [InlineData("a\r\nb",              "a b")]
        [InlineData("  a\t\tb  ",          "a b")]
        [InlineData("tôi là ai?\nđây là đâu?", "tôi là ai? đây là đâu?")]
        public void FlattenLine_collapsesWhitespace(string input, string expected)
        {
            Assert.Equal(expected, KRepoSyncService.FlattenLine(input));
        }

        // ── BUG #1: q.Name with embedded newline must not be split by parser ────────
        [Fact]
        public void Builder_questionNameWithNewline_flattensToSingleLine()
        {
            var md = KRepoSyncService.BuildRepoMarkdown(new()
            {
                DbQ(5, "tôi là ai?\nđây là đâu?", desc: "answer here"),
            });

            // Parser must see a SINGLE question with id=5 (not two), no leftover answer leak.
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.Equal(5, parsed[0].Id);
            Assert.Equal("tôi là ai? đây là đâu?", parsed[0].Question);
            Assert.Equal("answer here", parsed[0].Answer);
        }

        // ── BUG #3: "<!--" on its own line + "# ..." on the next line is still draft ─
        [Fact]
        public void Parser_tolerantToOpeningCommentOnSeparateLine()
        {
            var md = "<!-- \n# Hôm nay có gì tiến bộ? [id:2223 order:3]\nliệt kê đi -->\n";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.Equal(2223, parsed[0].Id);
            Assert.Equal("Hôm nay có gì tiến bộ?", parsed[0].Question);
            Assert.True(parsed[0].IsDraft);
            // Body must NOT contain the trailing "-->" leak.
            Assert.DoesNotContain("-->", parsed[0].Answer);
        }

        // ── BUG #3 cont.: "<!-- # ..." (with extra space before #) is also draft ────
        [Fact]
        public void Parser_tolerantToExtraSpaceBetweenCommentOpenAndHash()
        {
            var md = "<!-- # Hãy nhìn vào gương [id:2224 order:4]\n.123456 \n-->";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.Equal(2224, parsed[0].Id);
            Assert.True(parsed[0].IsDraft);
        }

        // ── BUG #2: round-trip equality — write then parse then compare returns true ─
        [Fact]
        public void RoundTrip_basicQuestion_isEqual()
        {
            var db = DbQ(10, "What is X?", "Because Y.\n- bullet\n- bullet");
            var md = KRepoSyncService.BuildRepoMarkdown(new() { db });
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.True(KRepoSyncService.QuestionsEqual(
                db.Name, db.Description, db.StatusCode == "draft",
                parsed[0].Question, parsed[0].Answer, parsed[0].IsDraft));
        }

        // ── BUG #2: name with newline → after flatten round-trip, comparer says equal ─
        [Fact]
        public void RoundTrip_nameWithNewline_comparerAcceptsAsEqual()
        {
            var db = DbQ(5, "tôi là ai?\nđây là đâu?", "ans");
            var md = KRepoSyncService.BuildRepoMarkdown(new() { db });
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.True(KRepoSyncService.QuestionsEqual(
                db.Name, db.Description, false,
                parsed[0].Question, parsed[0].Answer, parsed[0].IsDraft));
        }

        // ── BUG #4: same content but different draft status ⇒ NOT equal ────────────
        [Fact]
        public void Compare_sameContentDifferentDraftStatus_isNotEqual()
        {
            Assert.False(KRepoSyncService.QuestionsEqual(
                "Q1", "ans", dbDraft: false,
                "Q1", "ans", repoDraft: true));
        }

        // ── BUG #5: keep_repo flips draft — covered indirectly via QuestionsEqual ──
        // (the actual write to DB lives in ResolveConflictsAsync which we can't unit-test,
        // but the comparer correctly distinguishing draft means the resolver knows to flip)
        [Fact]
        public void Compare_drafToActive_isNotEqual_andDrafToDraft_isEqual()
        {
            Assert.False(KRepoSyncService.QuestionsEqual("Q", "a", true,  "Q", "a", false));
            Assert.True (KRepoSyncService.QuestionsEqual("Q", "a", true,  "Q", "a", true));
            Assert.True (KRepoSyncService.QuestionsEqual("Q", "a", false, "Q", "a", false));
        }

        // ── BUG #6: trailing whitespace per line in DB description ⇒ still equal ──
        [Fact]
        public void Compare_trailingWhitespacePerLine_isIgnored()
        {
            // DB has trailing space after "node" on the first line; round-trip strips it.
            var dbDesc   = "- tạo node \n- tạo bộ câu hỏi\n- test đi test lại";
            var repoDesc = "- tạo node\n- tạo bộ câu hỏi\n- test đi test lại";
            Assert.True(KRepoSyncService.QuestionsEqual(
                "Cách dùng KTree?", dbDesc, false,
                "Cách dùng KTree?", repoDesc, false));
        }

        // ── BUG #6 cont.: leading/trailing blank lines ignored ──────────────────────
        [Fact]
        public void Compare_leadingTrailingBlankLines_areIgnored()
        {
            Assert.True(KRepoSyncService.QuestionsEqual(
                "Q", "\n\nans\n\n", false,
                "Q", "ans", false));
        }

        // ── BUG #2: comparer flattens DB name before compare ────────────────────────
        [Fact]
        public void Compare_dbNameWithNewline_flattenedBeforeCompare()
        {
            // DB stores name with newline; repo (after round-trip) has it flattened.
            Assert.True(KRepoSyncService.QuestionsEqual(
                "tôi là ai?\nđây là đâu?", "ans", false,
                "tôi là ai? đây là đâu?",  "ans", false));
        }

        // ── Round-trip a draft question (covers BUG #1 + draft path) ────────────────
        [Fact]
        public void RoundTrip_draftQuestion_isEqual()
        {
            var db = DbQ(11, "Q11", "draft answer", draft: true);
            var md = KRepoSyncService.BuildRepoMarkdown(new() { db });
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.True(parsed[0].IsDraft);
            Assert.True(KRepoSyncService.QuestionsEqual(
                db.Name, db.Description, true,
                parsed[0].Question, parsed[0].Answer, parsed[0].IsDraft));
        }

        // ── Round-trip a draft with an empty answer ─────────────────────────────────
        [Fact]
        public void RoundTrip_draftEmptyAnswer_isEqual()
        {
            var db = DbQ(12, "draft empty", desc: null, draft: true);
            var md = KRepoSyncService.BuildRepoMarkdown(new() { db });
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.Single(parsed);
            Assert.True(parsed[0].IsDraft);
            Assert.True(KRepoSyncService.QuestionsEqual(
                db.Name, db.Description, true,
                parsed[0].Question, parsed[0].Answer, parsed[0].IsDraft));
        }

        // ── End-to-end: a node with mixed active/draft questions round-trips clean ─
        [Fact]
        public void RoundTrip_mixedActiveDraftQuestions_allEqual()
        {
            var dbList = new List<KQuestionEntity>
            {
                DbQ(1, "Active 1", "a1", draft: false, order: 1),
                DbQ(2, "Draft 1",  "d1", draft: true,  order: 2),
                DbQ(3, "Active with\nnewline name", "a3", draft: false, order: 3),
                DbQ(4, "Trailing space",  "line1   \nline2", draft: false, order: 4),
            };
            var md = KRepoSyncService.BuildRepoMarkdown(dbList);
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.Equal(dbList.Count, parsed.Count);
            for (var i = 0; i < dbList.Count; i++)
            {
                var db = dbList[i];
                var p  = parsed[i];
                Assert.True(KRepoSyncService.QuestionsEqual(
                    db.Name, db.Description, db.StatusCode == "draft",
                    p.Question, p.Answer, p.IsDraft),
                    $"Question {db.Id} did not round-trip clean: db='{db.Name}' p='{p.Question}'");
            }
        }
    }
}
