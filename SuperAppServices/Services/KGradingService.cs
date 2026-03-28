using System.Text.Json;
using Microsoft.Extensions.Logging;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    public class KGradingService : IKGradingService
    {
        private readonly IClaudibleService _ai;
        private readonly ILogger<KGradingService> _logger;

        private const string SystemPrompt = @"Bạn là giáo viên chấm điểm. Nhiệm vụ: chấm điểm câu trả lời của học viên theo thang 0–5 và đưa ra nhận xét ngắn gọn.

Rubric:
- 5: Hoàn toàn đúng, đầy đủ ý.
- 4: Đúng hầu hết, thiếu 1–2 ý nhỏ.
- 3: Đúng một phần, có sai sót nhưng nắm được ý chính.
- 2: Hiểu mơ hồ, nhiều sai sót.
- 1: Sai hoàn toàn nhưng có cố gắng trả lời.
- 0: Bỏ qua / không trả lời.

Trả về JSON array (không có markdown):
[{ ""nodeId"": <int>, ""point"": <0-5>, ""comment"": ""<nêu ngắn gọn những thứ bị thiếu, bị sai, tối đa 30 từ, nếu sai nhiều thì chỉ nêu những ý quan trọng nhất >"" }, ...]

Lưu ý: comment phải ngắn gọn, tối đa 15 từ, bằng tiếng Việt hoặc tiếng Anh tùy ngôn ngữ câu hỏi.";

        public KGradingService(IClaudibleService ai, ILogger<KGradingService> logger)
        {
            _ai     = ai     ?? throw new ArgumentNullException(nameof(ai));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<KGradingResult> GradeSubmissionAsync(
            string? testTitle,
            List<(int NodeId, string? AnswerText)> submissions,
            List<KNodeEntity> questionNodes)
        {
            // Separate skipped (null/empty answer) from answered
            var skipped  = submissions.Where(s => string.IsNullOrWhiteSpace(s.AnswerText)).ToList();
            var answered = submissions.Where(s => !string.IsNullOrWhiteSpace(s.AnswerText)).ToList();

            var results = new List<KGradedAnswer>();

            // Skipped → 0 points immediately
            results.AddRange(skipped.Select(s => new KGradedAnswer(s.NodeId, 0)));

            if (answered.Any())
            {
                try
                {
                    var nodeMap  = questionNodes.ToDictionary(n => n.Id);
                    var prompt   = BuildGradingPrompt(testTitle, answered, nodeMap);
                    var response = await _ai.ChatAsync(prompt, SystemPrompt);
                    var graded   = ParseAiResponse(response);

                    // Map graded results; fall back to 0/null for any missing
                    var gradedMap = graded.ToDictionary(g => g.NodeId);
                    results.AddRange(answered.Select(s =>
                        gradedMap.TryGetValue(s.NodeId, out var g)
                            ? g
                            : new KGradedAnswer(s.NodeId, 0)));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI grading failed — defaulting all answered to 0");
                    results.AddRange(answered.Select(s => new KGradedAnswer(s.NodeId, 0)));
                }
            }

            var total = results.Sum(r => r.Point);
            var max   = submissions.Count * 5;
            return new KGradingResult(total, max, results);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static string BuildGradingPrompt(
            string? testTitle,
            List<(int NodeId, string? AnswerText)> answered,
            Dictionary<int, KNodeEntity> nodeMap)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(testTitle))
                sb.AppendLine($"Bài kiểm tra: {testTitle}").AppendLine();

            for (var i = 0; i < answered.Count; i++)
            {
                var (nodeId, answerText) = answered[i];
                nodeMap.TryGetValue(nodeId, out var node);

                sb.AppendLine($"--- Câu {i + 1} (nodeId: {nodeId}) ---");
                sb.AppendLine($"Câu hỏi: {node?.Name ?? "(không rõ)"}");
                sb.AppendLine($"Đáp án mẫu: {node?.Description ?? "(không có đáp án mẫu)"}");
                sb.AppendLine($"Câu trả lời của học viên: {answerText}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static List<KGradedAnswer> ParseAiResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return [];

            // Strip markdown code fences if present
            var json = response.Trim();
            if (json.StartsWith("```")) json = string.Join('\n', json.Split('\n').Skip(1).SkipLast(1));

            var raw = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (raw == null) return [];

            return raw.Select(e =>
            {
                var nodeId  = e.GetProperty("nodeId").GetInt32();
                var point   = e.GetProperty("point").GetInt32();
                var comment = e.TryGetProperty("comment", out var c) ? c.GetString() : null;
                return new KGradedAnswer(nodeId, point, comment);
            }).ToList();
        }
    }
}
