using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;
using System.Text.Json;

namespace SuperAppServices.Services.K
{
    /// <summary>
    /// Markdown → k.node import.
    /// Approach 1 (AI): calls ClaudibleService to parse markdown into a node tree.
    /// Approach 2 (self-parse): deterministic markdown parser, no AI needed.
    /// Currently wired to Approach 2 in ImportAsync.
    /// </summary>
    public class KMarkdownImportService : IKMarkdownImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IClaudibleService    _ai;
        private readonly ILogger<KMarkdownImportService> _logger;

        // ── Approach 1: AI prompt ────────────────────────────────────────────
        private const string SystemPrompt = """
            Bạn là trợ lý phân tích nội dung học tập. Nhiệm vụ là chuyển đổi markdown thành cấu trúc node kiến thức.

            Quy tắc:
            1. Heading (#, ##, ###, ...) tạo ra node cha/con theo cấp bậc — cấp sâu hơn là con của cấp trên.
            2. Heading kết thúc bằng "?" → nodeType = "question", description = nội dung bên dưới heading (tóm gọn 1–2 câu, hoặc null nếu không có).
            3. Heading KHÔNG kết thúc bằng "?" → nodeType = "entity", description = null (bỏ qua toàn bộ nội dung bên dưới).
            4. Trả về JSON thuần (không markdown, không code block, không text thêm).

            Format JSON bắt buộc:
            [
              {
                "name": "Tên entity heading",
                "nodeType": "entity",
                "description": null,
                "children": [
                  { "name": "Câu hỏi là gì?", "nodeType": "question", "description": "Câu trả lời tóm tắt.", "children": [] }
                ]
              }
            ]
            """;

        public KMarkdownImportService(
            ApplicationDbContext context,
            IClaudibleService    ai,
            ILogger<KMarkdownImportService> logger)
        {
            _context = context;
            _ai      = ai;
            _logger  = logger;
        }

        // ── Entry point ──────────────────────────────────────────────────────

        public async Task<List<KNodeResponse>> ImportAsync(
            int knowledgeId, int userId, KImportMarkdownRequest request)
        {
            _logger.LogInformation("Markdown import — knowledgeId={KId}, parentNodeId={ParentId}, markdownLen={Len}",
                knowledgeId, request.ParentNodeId, request.Markdown.Length);

            // ── Approach 2: self-parse (active) ──────────────────────────────
            var nodes = ParseMarkdown(request.Markdown);
            _logger.LogDebug("Parsed {Count} root nodes from markdown", nodes.Count);

            var strategy = _context.Database.CreateExecutionStrategy();
            var created  = new List<KNodeResponse>();

            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    await CreateNodesRecursiveAsync(nodes, knowledgeId, request.ParentNodeId, created);
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            _logger.LogInformation("Markdown import complete — created {Count} nodes in knowledge {KId}", created.Count, knowledgeId);
            return created;
        }

        // ── Approach 2: self-parse ───────────────────────────────────────────
        // Rules:
        //   heading text        → node name
        //   content below       → node description (kept as-is, markdown included)
        //   ends with "?"       → nodeType = "question" (no children)
        //   otherwise           → nodeType = "entity"   (children allowed)
        //   horizontal rules    → ignored

        private sealed class NodeBuilder
        {
            public string            Name      { get; set; } = "";
            public string            NodeType  { get; set; } = "entity";
            public List<string>      DescLines { get; }      = new();
            public List<NodeBuilder> Children  { get; }      = new();
            public int               Level     { get; set; }

            public ParsedNode Build() => new(
                Name,
                NodeType,
                DescLines.Count > 0 ? string.Join("\n", DescLines).Trim() : null,
                Children.Select(c => c.Build()).ToList()
            );
        }

        private List<ParsedNode> ParseMarkdown(string markdown)
        {
            var roots   = new List<NodeBuilder>();
            var stack   = new List<NodeBuilder>(); // ancestor chain, shallowest → deepest
            NodeBuilder? current = null;            // collects description lines

            foreach (var rawLine in markdown.Split('\n'))
            {
                var line = rawLine.TrimEnd();

                if (line.StartsWith("#"))
                {
                    int level = 0;
                    while (level < line.Length && line[level] == '#') level++;
                    var name = line[level..].Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var nodeType = name.TrimEnd().EndsWith('?') ? "question" : "entity";
                    var node     = new NodeBuilder { Name = name, NodeType = nodeType, Level = level };

                    // Pop until we find the correct parent level
                    while (stack.Count > 0 && stack[^1].Level >= level)
                        stack.RemoveAt(stack.Count - 1);

                    if (stack.Count == 0)
                        roots.Add(node);
                    else
                        stack[^1].Children.Add(node);

                    stack.Add(node);
                    current = node;
                }
                else if (current != null && !string.IsNullOrWhiteSpace(line) && !IsHorizontalRule(line))
                {
                    current.DescLines.Add(line);
                }
            }

            var result = roots.Select(r => r.Build()).ToList();
            StripQuestionChildren(result);
            return result;
        }

        private static bool IsHorizontalRule(string line)
        {
            var t = line.Trim();
            return t.Length >= 3 &&
                   (t.Replace("-", "").Length == 0 ||
                    t.Replace("*", "").Length == 0 ||
                    t.Replace("_", "").Length == 0);
        }

        private static void StripQuestionChildren(List<ParsedNode> nodes)
        {
            foreach (var n in nodes)
            {
                if (n.NodeType == "question")
                    n.Children.Clear();
                else
                    StripQuestionChildren(n.Children);
            }
        }

        // ── Approach 1: AI parse (kept for reference) ────────────────────────

        private async Task<List<ParsedNode>> ParseWithAiAsync(string markdown)
        {
            var rawJson = await _ai.ChatAsync(markdown, SystemPrompt);
            _logger.LogDebug("AI raw response:\n{Raw}", rawJson);
            return ParseAiResponse(rawJson);
        }

        private record AiNode(string Name, string NodeType, string? Description, List<AiNode> Children);

        private List<ParsedNode> ParseAiResponse(string rawJson)
        {
            var cleaned = rawJson.Trim();
            if (cleaned.StartsWith("```"))
            {
                var start = cleaned.IndexOf('\n');
                var end   = cleaned.LastIndexOf("```");
                if (start >= 0 && end > start)
                    cleaned = cleaned[(start + 1)..end].Trim();
            }

            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                return ParseAiArray(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse AI response: {Raw}", rawJson[..Math.Min(300, rawJson.Length)]);
                throw new InvalidOperationException("AI returned invalid JSON. Please try again.", ex);
            }
        }

        private static List<ParsedNode> ParseAiArray(JsonElement array)
        {
            var result = new List<ParsedNode>();
            foreach (var el in array.EnumerateArray())
            {
                var name        = el.TryGetProperty("name",        out var n) ? n.GetString() ?? "Untitled" : "Untitled";
                var nodeType    = el.TryGetProperty("nodeType",    out var t) ? t.GetString() ?? "entity"   : "entity";
                var description = el.TryGetProperty("description", out var d) ? d.GetString()                 : null;
                var children    = el.TryGetProperty("children",    out var c) && c.ValueKind == JsonValueKind.Array
                    ? ParseAiArray(c)
                    : new List<ParsedNode>();

                // Deterministic override regardless of AI output
                nodeType = name.TrimEnd().EndsWith("?") ? "question" : "entity";

                result.Add(new ParsedNode(name, nodeType, description, children));
            }
            return result;
        }

        // ── Recursive creator (shared) ───────────────────────────────────────

        private async Task CreateNodesRecursiveAsync(
            List<ParsedNode> nodes,
            int knowledgeId,
            int? parentId,
            List<KNodeResponse> created)
        {
            foreach (var node in nodes)
            {
                int nodeId;

                // Skip question nodes — questions now live in k.question, not k.node
                if (node.NodeType == "question") continue;

                // Dedup: nodes with the same name + parent are reused
                {
                    var existing = await _context.KNodes
                        .Where(n => n.KnowledgeId == knowledgeId
                                 && n.ParentId    == parentId
                                 && n.Name        == node.Name
                                 && n.DeletedAt   == null)
                        .Select(n => (int?)n.Id)
                        .FirstOrDefaultAsync();

                    if (existing.HasValue)
                    {
                        _logger.LogInformation("Skipped duplicate entity '{Name}' (id={Id}) under parentId={ParentId}", node.Name, existing.Value, parentId);
                        if (node.Children.Count > 0)
                            await CreateNodesRecursiveAsync(node.Children, knowledgeId, existing.Value, created);
                        continue;
                    }
                }

                var entity = new KNodeEntity
                {
                    KnowledgeId = knowledgeId,
                    ParentId    = parentId,
                    Name        = node.Name,
                    Description = node.Description,
                    StatusCode  = "draft",
                    Color       = null,
                    Icon        = null,
                    CreatedAt   = DateTime.UtcNow,
                };

                _context.KNodes.Add(entity);
                await _context.SaveChangesAsync();

                _context.KNodeStatusHistory.Add(new KNodeStatusHistoryEntity
                {
                    NodeId     = entity.Id,
                    StatusCode = entity.StatusCode ?? "learning",
                    ChangedAt  = entity.CreatedAt ?? DateTime.UtcNow,
                    UserId     = null,
                });
                await _context.SaveChangesAsync();

                nodeId = entity.Id;

                created.Add(new KNodeResponse
                {
                    Id          = entity.Id,
                    KnowledgeId = entity.KnowledgeId,
                    ParentId    = entity.ParentId,
                    Name        = entity.Name,
                    Description = entity.Description,
                    StatusCode  = entity.StatusCode,
                    PathIds     = entity.PathIds,
                    PathDepth   = entity.PathDepth,
                    CreatedAt   = entity.CreatedAt,
                });

                if (node.Children.Count > 0)
                    await CreateNodesRecursiveAsync(node.Children, knowledgeId, nodeId, created);
            }
        }

        // ── ImportTestMarkdown ───────────────────────────────────────────────
        // Tests no longer exist — questions are created directly under the knowledge.

        public async Task<int> ImportTestMarkdownAsync(
            int knowledgeId, int userId, KImportTestMarkdownRequest request)
        {
            _logger.LogInformation(
                "ImportTestMarkdown — knowledgeId={KId}, tests={Tests}, orphans={Orphans}",
                knowledgeId, request.Tests.Count, request.OrphanQuestions.Count);

            var strategy = _context.Database.CreateExecutionStrategy();
            int questionsCreated = 0;

            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    var nodeId = request.ParentNodeId;

                    // Flatten all test groups into questions under the node
                    foreach (var testItem in request.Tests)
                    {
                        var ids = await CreateQuestionsForNodeAsync(testItem.Questions, nodeId);
                        questionsCreated += ids.Count;
                    }

                    // Orphan questions go directly under the node
                    if (request.OrphanQuestions.Count > 0)
                    {
                        var ids = await CreateQuestionsForNodeAsync(request.OrphanQuestions, nodeId);
                        questionsCreated += ids.Count;
                    }

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            _logger.LogInformation(
                "ImportTestMarkdown complete — {Count} questions created in knowledge {KId}", questionsCreated, knowledgeId);
            return questionsCreated;
        }

        /// <summary>Creates k.question rows directly under a node and returns their IDs.</summary>
        private async Task<List<int>> CreateQuestionsForNodeAsync(
            List<KMdQuestionItem> questions, int nodeId)
        {
            var maxOrder = await _context.KQuestions
                .Where(q => q.NodeId == nodeId)
                .Select(q => (int?)q.SortOrder)
                .MaxAsync() ?? -1;

            var entities = questions.Select((q, i) => new KQuestionEntity
            {
                NodeId      = nodeId,
                Name        = q.Question,
                Description = string.IsNullOrWhiteSpace(q.Answer) ? null : q.Answer.Trim(),
                StatusCode  = "learning",
                SortOrder   = maxOrder + 1 + i,
            }).ToList();

            _context.KQuestions.AddRange(entities);
            await _context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            _context.KQuestionStatusHistory.AddRange(entities.Select(e => new KQuestionStatusHistoryEntity
            {
                QuestionId = e.Id,
                StatusCode = e.StatusCode,
                ChangedAt  = now,
                UserId     = null,
            }));
            await _context.SaveChangesAsync();

            return entities.Select(e => e.Id).ToList();
        }

        // ── Shared internal DTO ──────────────────────────────────────────────

        private record ParsedNode(
            string Name,
            string NodeType,
            string? Description,
            List<ParsedNode> Children);
    }
}
