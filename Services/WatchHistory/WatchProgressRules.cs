namespace MovieStreaming.Services.WatchHistory;

public static class WatchProgressRules
{
    public const decimal CompletionThreshold = 0.90m; // 90%

    public static bool IsCompleted(int progressSeconds, int? durationSeconds)
    {
        if (!durationSeconds.HasValue || durationSeconds.Value <= 0)
        {
            return false;
        }

        return (decimal)progressSeconds / durationSeconds.Value >= CompletionThreshold;
    }

    public static double CalculateProgressPercentage(int progressSeconds, int? durationSeconds)
    {
        if (!durationSeconds.HasValue || durationSeconds.Value <= 0)
        {
            return 0.0;
        }

        var percentage = (double)progressSeconds / durationSeconds.Value * 100.0;
        return Math.Min(100.0, Math.Round(percentage, 1));
    }
}
