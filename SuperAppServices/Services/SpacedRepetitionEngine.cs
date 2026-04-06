namespace SuperAppServices.Services
{
    /// <summary>
    /// SM-2 Spaced Repetition algorithm.
    /// Input: score (0–5), current SRS state.
    /// Output: new interval, ease factor, repetitions, next review date.
    /// </summary>
    public static class SpacedRepetitionEngine
    {
        public record SrsState(int Interval, double EaseFactor, int Repetitions, DateTime? NextReviewAt);

        /// <summary>
        /// Calculate the next SRS state after a review with the given score.
        /// </summary>
        /// <param name="score">AI-assigned score 0–5</param>
        /// <param name="current">Current SRS state of the question</param>
        public static SrsState CalculateNext(int score, SrsState current)
        {
            var interval    = current.Interval;
            var easeFactor  = current.EaseFactor;
            var repetitions = current.Repetitions;

            if (score < 3)
            {
                // Failed — reset
                repetitions = 0;
                interval = score < 2 ? 0 : 1; // 0–1: requeue today, 2: tomorrow
            }
            else
            {
                // Passed
                if (repetitions == 0)
                    interval = 1;
                else if (repetitions == 1)
                    interval = 6;
                else
                    interval = (int)Math.Round(interval * easeFactor);

                repetitions++;
            }

            // Update ease factor based on score
            easeFactor += 0.1 - (5 - score) * (0.08 + (5 - score) * 0.02);
            easeFactor = Math.Max(1.3, easeFactor);

            var nextReview = interval == 0
                ? DateTime.UtcNow.AddHours(1) // re-show in 1 hour for score 0–1
                : DateTime.UtcNow.AddDays(interval);

            return new SrsState(interval, easeFactor, repetitions, nextReview);
        }

        /// <summary>
        /// Check if a test should be auto-promoted to mastered.
        /// Criteria: last 5 sessions all have avgPoint > 4.5 AND avgSpeedRatio < 1.
        /// </summary>
        public static bool ShouldPromoteToMastered(List<(double AvgPoint, double AvgSpeedRatio)> recentSessions)
        {
            if (recentSessions.Count < 5) return false;
            return recentSessions.All(s => s.AvgPoint > 4.5 && s.AvgSpeedRatio < 1.0);
        }

        /// <summary>
        /// Check if a mastered test should regress to learning.
        /// Criteria: any session does NOT meet point > 4.5 AND speedRatio < 1.
        /// </summary>
        public static bool ShouldRegressToLearning(List<(double AvgPoint, double AvgSpeedRatio)> recentSessions)
        {
            if (recentSessions.Count < 5) return true; // not enough data → regress
            return !recentSessions.All(s => s.AvgPoint > 4.5 && s.AvgSpeedRatio < 1.0);
        }
    }
}
