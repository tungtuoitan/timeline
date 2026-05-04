using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services.K
{
    public class KGradingService : IKGradingService
    {
        private readonly IClaudibleService _primary;
        private readonly ChatClient _fallback;
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
[{ ""questionId"": <int>, ""point"": <0-5>, ""comment"": ""<nêu ngắn gọn những thứ bị thiếu, bị sai, tối đa 30 từ, nếu sai nhiều thì chỉ nêu những ý quan trọng nhất >"" }, ...]

Lưu ý: comment phải ngắn gọn, tối đa 15 từ, bằng tiếng Việt hoặc tiếng Anh tùy ngôn ngữ câu hỏi.";

        public KGradingService(IClaudibleService primary, IConfiguration config, ILogger<KGradingService> logger)
        {
            _primary = primary ?? throw new ArgumentNullException(nameof(primary));
            _logger  = logger;

            var apiKey        = config["OpenRouter:ApiKey"]       ?? throw new InvalidOperationException("OpenRouter:ApiKey is not configured.");
            var baseUrl       = config["OpenRouter:BaseUrl"]       ?? "https://openrouter.ai/api/v1";
            var fallbackModel = config["OpenRouter:FallbackModel"] ?? throw new InvalidOperationException("OpenRouter:FallbackModel is not configured.");

            var options    = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
            var credential = new System.ClientModel.ApiKeyCredential(apiKey);
            _fallback = new OpenAIClient(credential, options).GetChatClient(fallbackModel);
        }

        public async Task<KGradingResult> GradeSubmissionAsync(
            string? testTitle,
            List<(int QuestionId, string? AnswerText)> submissions,
            List<KQuestionEntity> questions)
        {
            var skipped  = submissions.Where(s => string.IsNullOrWhiteSpace(s.AnswerText)).ToList();
            var answered = submissions.Where(s => !string.IsNullOrWhiteSpace(s.AnswerText)).ToList();

            var results = new List<KGradedAnswer>();
            results.AddRange(skipped.Select(s => new KGradedAnswer(s.QuestionId, 0)));

            if (answered.Any())
            {
                var questionMap = questions.ToDictionary(q => q.Id);
                var prompt      = BuildGradingPrompt(testTitle, answered, questionMap);

                _logger.LogDebug("[KGrading] Prompt:\n{Prompt}", prompt);

                string response;
                try
                {
                    _logger.LogDebug("[KGrading] Calling primary (Claudible)");
                    response = await _primary.ChatAsync(prompt, SystemPrompt);
                    _logger.LogDebug("[KGrading] Primary response:\n{Response}", response);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[KGrading] Primary failed, trying fallback (OpenRouter Qwen)");
                    var messages = new List<ChatMessage>
                    {
                        new SystemChatMessage(SystemPrompt),
                        new UserChatMessage(prompt),
                    };
                    var res = await _fallback.CompleteChatAsync(messages);
                    response = res.Value.Content[0].Text;
                    _logger.LogDebug("[KGrading] Fallback response:\n{Response}", response);
                }

                var graded    = ParseAiResponse(response);
                var gradedMap = graded.ToDictionary(g => g.QuestionId);
                results.AddRange(answered.Select(s =>
                    gradedMap.TryGetValue(s.QuestionId, out var g)
                        ? g
                        : new KGradedAnswer(s.QuestionId, 0)));
            }

            var total = results.Sum(r => r.Point);
            var max   = submissions.Count * 5;
            return new KGradingResult(total, max, results);
        }

        private static string BuildGradingPrompt(
            string? testTitle,
            List<(int QuestionId, string? AnswerText)> answered,
            Dictionary<int, KQuestionEntity> questionMap)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(testTitle))
                sb.AppendLine($"Bài kiểm tra: {testTitle}").AppendLine();

            for (var i = 0; i < answered.Count; i++)
            {
                var (questionId, answerText) = answered[i];
                questionMap.TryGetValue(questionId, out var q);

                sb.AppendLine($"--- Câu {i + 1} (questionId: {questionId}) ---");
                sb.AppendLine($"Câu hỏi: {q?.Name ?? "(không rõ)"}");
                sb.AppendLine($"Đáp án mẫu: {q?.Description ?? "(không có đáp án mẫu)"}");
                sb.AppendLine($"Câu trả lời của học viên: {answerText}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static List<KGradedAnswer> ParseAiResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return [];

            var json = response.Trim();
            if (json.StartsWith("```")) json = string.Join('\n', json.Split('\n').Skip(1).SkipLast(1));

            var raw = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (raw == null) return [];

            return raw.Select(e =>
            {
                var questionId = e.GetProperty("questionId").GetInt32();
                var point      = e.GetProperty("point").GetInt32();
                var comment    = e.TryGetProperty("comment", out var c) ? c.GetString() : null;
                return new KGradedAnswer(questionId, point, comment);
            }).ToList();
        }
    }
}
