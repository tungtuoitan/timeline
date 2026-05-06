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
        /// Calculate the next SRS state after a review with the given score (1–5).
        /// Score 1-3: reset to 0 interval, with different review delays.
        /// Score 4-5: advance per SM-2, incrementing repetitions.
        /// </summary>
        /// <param name="score">Self-assigned score 1–5 (1=very forgot, 5=remembered perfectly)</param>
        /// <param name="current">Current SRS state of the question</param>
        public static SrsState CalculateNext(int score, SrsState current)
        {
            var interval    = current.Interval;
            var easeFactor  = current.EaseFactor;
            var repetitions = current.Repetitions;

            DateTime nextReview;

            if (score == 1)
            {
                // Very forgot — reset, requeue in 30 minutes
                repetitions = 0;
                interval = 0;
                nextReview = VietnamDateTime.Now().AddMinutes(30);
            }
            else if (score == 2)
            {
                // Forgot — reset, requeue in 2 hours
                repetitions = 0;
                interval = 0;
                nextReview = VietnamDateTime.Now().AddHours(2);
            }
            else if (score == 3)
            {
                // Okay — reset, requeue in 4 hours
                repetitions = 0;
                interval = 0;
                nextReview = VietnamDateTime.Now().AddHours(4);
            }
            else if (score == 4)
            {
                // Good — advance interval per SM-2
                if (repetitions == 0)
                    interval = 1;
                else if (repetitions == 1)
                    interval = 6;
                else
                    interval = (int)Math.Round(interval * easeFactor);

                repetitions++;
                nextReview = VietnamDateTime.Now().AddDays(interval);
            }
            else
            {
                // Perfect (5) — advance interval per SM-2
                if (repetitions == 0)
                    interval = 1;
                else if (repetitions == 1)
                    interval = 6;
                else
                    interval = (int)Math.Round(interval * easeFactor);

                repetitions++;
                nextReview = VietnamDateTime.Now().AddDays(interval);
            }

            // Update ease factor based on score (1-5)
            // SM-2 formula: EF' = EF + 0.1 - (5 - q) × (0.08 + (5 - q) × 0.02)
            easeFactor += 0.1 - (5 - score) * (0.08 + (5 - score) * 0.02);
            easeFactor = Math.Max(1.3, easeFactor);

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

    }
}
