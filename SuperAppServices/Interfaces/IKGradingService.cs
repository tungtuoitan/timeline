using SuperAppModels.Models;

namespace SuperAppServices.Interfaces
{
    public record KGradedAnswer(int NodeId, int Point, string? Comment = null);

    public record KGradingResult(
        int TotalPoints,
        int MaxPoints,
        List<KGradedAnswer> Answers);

    public interface IKGradingService
    {
        /// <summary>
        /// Grade a set of answers against their question nodes using AI.
        /// </summary>
        Task<KGradingResult> GradeSubmissionAsync(
            string? testTitle,
            List<(int NodeId, string? AnswerText)> submissions,
            List<KNodeEntity> questionNodes);
    }
}
