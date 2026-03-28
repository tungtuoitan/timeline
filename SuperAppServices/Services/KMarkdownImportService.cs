using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;
using System.Text.Json;

namespace SuperAppServices.Services
{
    /// <summary>
    /// AI-powered markdown → k.node import.
    /// Calls ClaudibleService to parse free-form markdown into a node tree,
    /// then recursively creates nodes (parent-first) with statusCode = "draft".
    /// Pattern follows KTestGeneratorService.
    /// </summary>
    public class KMarkdownImportService : IKMarkdownImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IClaudibleService _ai;
        private readonly ILogger<KMarkdownImportService> _logger;

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
            IClaudibleService ai,
            ILogger<KMarkdownImportService> logger)
        {
            _context = context;
            _ai      = ai;
            _logger  = logger;
        }

        public async Task<List<KNodeResponse>> ImportAsync(
            int knowledgeId, int userId, KImportMarkdownRequest request)
        {
            _logger.LogInformation("Markdown import — knowledgeId={KId}, parentNodeId={ParentId}, markdownLen={Len}",
                knowledgeId, request.ParentNodeId, request.Markdown.Length);

            var rawJson = await _ai.ChatAsync(request.Markdown, SystemPrompt);
            _logger.LogDebug("AI raw response:\n{Raw}", rawJson);

            var aiNodes = ParseAiResponse(rawJson);

            var strategy = _context.Database.CreateExecutionStrategy();
            var created  = new List<KNodeResponse>();

            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    await CreateNodesRecursiveAsync(aiNodes, knowledgeId, request.ParentNodeId, created);
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

        // ── Recursive creator ────────────────────────────────────────────────

        private async Task CreateNodesRecursiveAsync(
            List<AiNode> nodes,
            int knowledgeId,
            int? parentId,
            List<KNodeResponse> created)
        {
            foreach (var ai in nodes)
            {
                int nodeId;

                // Dedup: entity nodes with the same name + parent are reused, not re-created
                if (ai.NodeType == "entity")
                {
                    var existing = await _context.KNodes
                        .Where(n => n.KnowledgeId == knowledgeId
                                 && n.ParentId    == parentId
                                 && n.Name        == ai.Name
                                 && n.DeletedAt   == null)
                        .Select(n => (int?)n.Id)
                        .FirstOrDefaultAsync();

                    if (existing.HasValue)
                    {
                        _logger.LogInformation("Skipped duplicate entity node '{Name}' (id={Id}) under parentId={ParentId}", ai.Name, existing.Value, parentId);
                        // Still recurse so children are processed under this existing node
                        if (ai.Children.Count > 0)
                            await CreateNodesRecursiveAsync(ai.Children, knowledgeId, existing.Value, created);
                        continue;
                    }
                }

                var entity = new KNodeEntity
                {
                    KnowledgeId = knowledgeId,
                    ParentId    = parentId,
                    Name        = ai.Name,
                    Description = ai.Description,
                    NodeType    = ai.NodeType,
                    StatusCode  = "draft",
                    Color       = null,
                    Icon        = null,
                    CreatedAt   = DateTime.UtcNow,
                };

                _context.KNodes.Add(entity);
                await _context.SaveChangesAsync(); // flush to get Id before children

                nodeId = entity.Id;

                created.Add(new KNodeResponse
                {
                    Id          = entity.Id,
                    KnowledgeId = entity.KnowledgeId,
                    ParentId    = entity.ParentId,
                    Name        = entity.Name,
                    Description = entity.Description,
                    NodeType    = entity.NodeType,
                    StatusCode  = entity.StatusCode,
                    PathIds     = entity.PathIds,
                    PathDepth   = entity.PathDepth,
                    CreatedAt   = entity.CreatedAt,
                });

                if (ai.Children.Count > 0)
                    await CreateNodesRecursiveAsync(ai.Children, knowledgeId, nodeId, created);
            }
        }

        // ── AI response parser ───────────────────────────────────────────────

        private record AiNode(string Name, string NodeType, string? Description, List<AiNode> Children);

        private List<AiNode> ParseAiResponse(string rawJson)
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
                using var doc  = JsonDocument.Parse(cleaned);
                return ParseArray(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse AI response: {Raw}", rawJson[..Math.Min(300, rawJson.Length)]);
                throw new InvalidOperationException("AI returned invalid JSON. Please try again.", ex);
            }
        }

        private static List<AiNode> ParseArray(JsonElement array)
        {
            var result = new List<AiNode>();
            foreach (var el in array.EnumerateArray())
            {
                var name        = el.TryGetProperty("name",        out var n) ? (n.GetString() ?? "Untitled") : "Untitled";
                var nodeType    = el.TryGetProperty("nodeType",    out var t) ? (t.GetString() ?? "entity")   : "entity";
                var description = el.TryGetProperty("description", out var d) ? d.GetString()                 : null;
                var children    = el.TryGetProperty("children",    out var c) && c.ValueKind == JsonValueKind.Array
                    ? ParseArray(c)
                    : new List<AiNode>();

                // Deterministic override — always trust the "?" rule regardless of AI output
                nodeType = name.TrimEnd().EndsWith("?") ? "question" : "entity";

                result.Add(new AiNode(name, nodeType, description, children));
            }
            return result;
        }
    }
}
