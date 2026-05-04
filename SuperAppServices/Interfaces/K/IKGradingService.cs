using SuperAppModels.Models;

namespace SuperAppServices.Interfaces
{
    public record KGradedAnswer(int QuestionId, int Point, string? Comment = null);

    public record KGradingResult(
        int TotalPoints,
        int MaxPoints,
        List<KGradedAnswer> Answers);

    public interface IKGradingService
    {
        /// <summary>
        /// Grade a set of answers against their questions using AI.
        /// </summary>
        Task<KGradingResult> GradeSubmissionAsync(
            string? testTitle,
            List<(int QuestionId, string? AnswerText)> submissions,
            List<KQuestionEntity> questions);
    }
}
