namespace SuperAppServices.Services.K
{
    /// <summary>
    /// SM-2 Spaced Repetition algorithm.
    /// Input: score (0–5), current SRS state.
    /// Output: new interval, ease factor, repetitions, next review date.
    /// </summary>
    public static class SpacedRepetitionEngine
    {
        public record SrsState(int Interval, double EaseFactor, int Repetitions, DateTime? NextReviewAt);

        // Use VietnamDateTime.Now() — see SuperAppModels/Utils/VietnamDateTime.cs for rationale.

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

            if (score <= 2)
            {
                // Heavy fail (0–2) — reset, requeue in 30 minutes
                repetitions = 0;
                interval = 0;
            }
            else if (score == 3)
            {
                // Medium fail — reset, requeue in 2 hours
                repetitions = 0;
                interval = 0;
            }
            else if (score == 4)
            {
                // Near-pass — shrink interval by 20%, minimum 1 day
                interval = Math.Max(1, (int)Math.Round(interval * 0.8));
                // repetitions unchanged — not reset, not incremented
            }
            else
            {
                // Perfect (5) — interval grows per SM-2
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

            DateTime nextReview;
            if (score <= 2)
                nextReview = VietnamDateTime.Now().AddMinutes(30);
            else if (score == 3)
                nextReview = VietnamDateTime.Now().AddHours(2);
            else
                nextReview = VietnamDateTime.Now().AddDays(interval);

            return new SrsState(interval, easeFactor, repetitions, nextReview);
        }

        /// <summary>
        /// Calculate current retention (0–100%) using forgetting curve.
        /// R = 0.9 ^ (daysSinceLastReview / interval)
        /// </summary>
        public static double CalculateRetention(int interval, DateTime? nextReviewAt)
        {
            if (nextReviewAt == null || interval <= 0) return 0;

            var lastReview = nextReviewAt.Value.AddDays(-interval);
            var daysSince  = (VietnamDateTime.Now() - lastReview).TotalDays;
            if (daysSince < 0) daysSince = 0;

            return Math.Round(Math.Pow(0.9, daysSince / interval) * 100, 1);
        }

        /// <summary>
        /// Calculate retention at a specific date (0–100%).
        /// </summary>
        public static double CalculateRetentionAtDate(int interval, DateTime? nextReviewAt, DateTime atDate)
        {
            if (nextReviewAt == null || interval <= 0) return 0;

            var lastReview = nextReviewAt.Value.AddDays(-interval);
            var daysSince  = (atDate - lastReview).TotalDays;

            // Before last review: approximate as freshly reviewed (previous cycle)
            if (daysSince < 0) return 100;

            return Math.Round(Math.Pow(0.9, daysSince / interval) * 100, 1);
        }

        /// <summary>
        /// Check if a test should be auto-promoted to mastered.
        /// Criteria: last 5 sessions all have avgPoint > 4.5.
        /// </summary>
        public static bool ShouldPromoteToMastered(List<(double AvgPoint, double AvgSpeedRatio)> recentSessions)
        {
            if (recentSessions.Count < 5) return false;
            return recentSessions.All(s => s.AvgPoint > 4.5);
        }

        /// <summary>
        /// Check if a mastered test should regress to learning.
        /// Criteria: any session in last 5 has avgPoint <= 4.5.
        /// </summary>
        public static bool ShouldRegressToLearning(List<(double AvgPoint, double AvgSpeedRatio)> recentSessions)
        {
            if (recentSessions.Count < 5) return true; // not enough data → regress
            return !recentSessions.All(s => s.AvgPoint > 4.5);
        }
    }
}
