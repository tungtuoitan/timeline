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
        private static KQuestionEntity DbQ(int id, string name, string? desc = null, bool draft = false, int order = 1, string? context = null, string? directives = null) =>
            new()
            {
                Id         = id,
                Name       = name,
                Description = desc,
                Context    = context,
                Directives = directives,
                StatusCode = draft ? "draft" : "active",
                SortOrder  = order,
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

        // ── Code block: leading fenced block in body → captured as Context ──────────
        [Fact]
        public void Parser_hashInsideCodeBlock_notTreatedAsHeading()
        {
            var md = """
                # What is a shell script? [id:20 order:1]
                ```bash
                # this is a bash comment
                echo hello
                # another comment
                ```
                """;
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.Equal(20, parsed[0].Id);
            // Leading code block → context, not answer
            Assert.Contains("# this is a bash comment", parsed[0].Context ?? "");
            Assert.Contains("echo hello", parsed[0].Context ?? "");
            Assert.True(string.IsNullOrWhiteSpace(parsed[0].Answer));
        }

        // ── Code block: description that starts with code block round-trips via Context ─
        [Fact]
        public void RoundTrip_descriptionWithCodeBlock_isEqual()
        {
            // When Context is set explicitly, builder writes code block before description
            var ctx  = "```python\n# python comment\ndef foo():\n    return 1\n```";
            var db   = DbQ(21, "How to write Python?", desc: "Answer text", context: ctx);
            var md   = KRepoSyncService.BuildRepoMarkdown(new() { db });
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.Single(parsed);
            Assert.Equal(21, parsed[0].Id);
            Assert.Equal("Answer text", parsed[0].Answer);
            Assert.NotNull(parsed[0].Context);
            Assert.Contains("def foo():", parsed[0].Context!);
            Assert.True(KRepoSyncService.QuestionsEqual(
                db.Name, db.Description, false,
                parsed[0].Question, parsed[0].Answer, parsed[0].IsDraft,
                db.Context, parsed[0].Context));
        }

        // ── Code block: multiple questions — context in first doesn't bleed into second ─
        [Fact]
        public void Parser_codeBlockInFirstQuestion_doesNotAffectSecond()
        {
            var md = """
                # Question A [id:30 order:1]
                ```js
                // comment
                # not a heading
                const x = 1;
                ```

                # Question B [id:31 order:2]
                plain answer
                """;
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Equal(2, parsed.Count);
            Assert.Equal(30, parsed[0].Id);
            Assert.Equal(31, parsed[1].Id);
            // Leading code block → context of Q A
            Assert.Contains("# not a heading", parsed[0].Context ?? "");
            Assert.True(string.IsNullOrWhiteSpace(parsed[0].Answer));
            Assert.Equal("plain answer", parsed[1].Answer);
        }

        // ── Draft + code block: --> inside code block does not close draft early ──────
        [Fact]
        public void Parser_arrowInsideDraftCodeBlock_doesNotCloseDraftEarly()
        {
            var md = "<!--# Draft with code [id:40 order:1]\n```html\n<!-- comment -->\n<div>hi</div>\n```\n-->";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.Equal(40, parsed[0].Id);
            Assert.True(parsed[0].IsDraft);
            Assert.Contains("<!-- comment -->", parsed[0].Answer);
        }

        // ── Tilde fence (~~~) also tracked as code block ─────────────────────────────
        [Fact]
        public void Parser_tildeFence_alsoTrackedAsCodeBlock()
        {
            var md = """
                # Tilde fence test [id:50 order:1]
                ~~~bash
                # bash comment
                echo hi
                ~~~
                """;
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.Equal(50, parsed[0].Id);
            // Leading tilde fence → context
            Assert.Contains("# bash comment", parsed[0].Context ?? "");
        }

        // ── Owned context: round-trip preserves context separate from answer ──────────
        [Fact]
        public void RoundTrip_ownedContext_preservedOnRoundTrip()
        {
            var ctx = "```python\n# setup\ndef foo():\n    return 1\n```";
            var db  = DbQ(60, "What does foo return?", desc: "It returns 1.", context: ctx);
            var md  = KRepoSyncService.BuildRepoMarkdown(new() { db });
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.Single(parsed);
            Assert.Equal(60, parsed[0].Id);
            Assert.Equal("It returns 1.", parsed[0].Answer);
            Assert.NotNull(parsed[0].Context);
            Assert.Contains("def foo():", parsed[0].Context!);
            // Context must NOT appear in answer
            Assert.DoesNotContain("def foo():", parsed[0].Answer);
            Assert.True(KRepoSyncService.QuestionsEqual(
                db.Name, db.Description, false,
                parsed[0].Question, parsed[0].Answer, parsed[0].IsDraft,
                db.Context, parsed[0].Context));
        }

        // ── Context with blank lines between heading and code block ───────────────────
        [Fact]
        public void Parser_blankLineBetweenHeadingAndContext_stillCapturedAsContext()
        {
            var md = "# Question [id:61 order:1]\n\n```python\ndef bar(): pass\n```\nAnswer here";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.NotNull(parsed[0].Context);
            Assert.Contains("def bar():", parsed[0].Context!);
            Assert.Equal("Answer here", parsed[0].Answer);
        }

        // ── No context: plain-text answer not affected ────────────────────────────────
        [Fact]
        public void Parser_noContext_backwardCompatible()
        {
            var md = "# Plain question [id:62 order:1]\nThis is the answer.\nSecond line.";
            var parsed = KRepoSyncService.ParseQuestions(md);
            Assert.Single(parsed);
            Assert.Null(parsed[0].Context);
            Assert.Contains("This is the answer.", parsed[0].Answer);
            Assert.Contains("Second line.", parsed[0].Answer);
        }

        // ── Scope validity: open without close = context private to opener ─────────
        [Fact]
        public void Scope_openWithoutClose_contextStaysPrivateToOpener()
        {
            var ctx = "```cs\nvar x = 1;\n```";
            var dbList = new List<KQuestionEntity>
            {
                DbQ(1, "Opener",  "ans1", order: 1, context: ctx,  directives: "[\"open-context\"]"),
                DbQ(2, "Child",   "ans2", order: 2),   // no own context, no close-context below → scope invalid
                DbQ(3, "Other",   "ans3", order: 3),
            };
            var md     = KRepoSyncService.BuildRepoMarkdown(dbList);
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.Equal(3, parsed.Count);
            // Opener keeps its own context
            Assert.NotNull(parsed[0].Context);
            // No scope entered — q2 and q3 get no inherited context
            Assert.Null(parsed[1].Context);
            Assert.Null(parsed[2].Context);
        }

        // ── Scope validity: valid open+close pair propagates context ─────────────
        [Fact]
        public void Scope_validOpenClosePair_contextPropagatedToChildren()
        {
            var ctx = "```cs\nvar x = 1;\n```";
            var dbList = new List<KQuestionEntity>
            {
                DbQ(1, "Opener",  "ans1", order: 1, context: ctx,  directives: "[\"open-context\"]"),
                DbQ(2, "Child",   "ans2", order: 2),
                DbQ(3, "Closer",  "ans3", order: 3, directives: "[\"close-context\"]"),
                DbQ(4, "After",   "ans4", order: 4),
            };
            var md     = KRepoSyncService.BuildRepoMarkdown(dbList);
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.Equal(4, parsed.Count);
            Assert.NotNull(parsed[0].Context);
            Assert.Contains("var x = 1", parsed[1].Context ?? "");  // inherited
            Assert.Contains("var x = 1", parsed[2].Context ?? "");  // closer also inherits
            Assert.Null(parsed[3].Context);                          // after scope — no context
        }

        // ── Scope validity: inner question with own context breaks the scope ──────
        [Fact]
        public void Scope_innerQuestionWithOwnContext_invalidatesScope()
        {
            var ctx1 = "```cs\nvar x = 1;\n```";
            var ctx2 = "```cs\nvar y = 2;\n```";
            var dbList = new List<KQuestionEntity>
            {
                DbQ(1, "Opener",    "a1", order: 1, context: ctx1, directives: "[\"open-context\"]"),
                DbQ(2, "HasCtx",    "a2", order: 2, context: ctx2),  // own context breaks scope
                DbQ(3, "Closer",    "a3", order: 3, directives: "[\"close-context\"]"),
            };
            var md     = KRepoSyncService.BuildRepoMarkdown(dbList);
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.Equal(3, parsed.Count);
            // Opener keeps its context
            Assert.NotNull(parsed[0].Context);
            // Inner question keeps its own context (not inherited)
            Assert.Contains("var y = 2", parsed[1].Context ?? "");
            // Closer gets no inherited context (scope was invalid)
            Assert.Null(parsed[2].Context);
        }

        // ── Scope validity: standalone question with context is not shared ────────
        [Fact]
        public void Scope_standaloneContextNotShared()
        {
            var ctx = "```go\nfunc main() {}\n```";
            var dbList = new List<KQuestionEntity>
            {
                DbQ(1, "StandaloneWithCtx", "a1", order: 1, context: ctx),
                DbQ(2, "Next",              "a2", order: 2),
            };
            var md     = KRepoSyncService.BuildRepoMarkdown(dbList);
            var parsed = KRepoSyncService.ParseQuestions(md);

            Assert.Equal(2, parsed.Count);
            Assert.NotNull(parsed[0].Context);
            Assert.Null(parsed[1].Context);
        }

        // ── Scope validity: round-trip of valid scope preserves children context ──
        [Fact]
        public void RoundTrip_validScope_childrenContextPreservedOnRoundTrip()
        {
            var ctx = "```python\ndef foo(): pass\n```";
            var dbList = new List<KQuestionEntity>
            {
                DbQ(1, "Opener", "a1", order: 1, context: ctx, directives: "[\"open-context\"]"),
                DbQ(2, "Child",  "a2", order: 2, context: ctx),  // denormalized copy
                DbQ(3, "Closer", "a3", order: 3, context: ctx, directives: "[\"close-context\"]"),
            };
            var md     = KRepoSyncService.BuildRepoMarkdown(dbList);
            var parsed = KRepoSyncService.ParseQuestions(md);

            // After round-trip, children should have inherited context re-applied
            Assert.Contains("def foo():", parsed[1].Context ?? "");
            Assert.Contains("def foo():", parsed[2].Context ?? "");
            // Opener's context emitted once in the markdown
            Assert.Equal(1, md.Split("def foo():").Length - 1);
        }
    }
}
