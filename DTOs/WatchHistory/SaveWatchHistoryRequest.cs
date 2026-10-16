namespace MovieStreaming.DTOs.WatchHistory;

public class SaveWatchHistoryRequest
{
    public int EpisodeId { get; set; }
    public int ProgressSeconds { get; set; }
    public int? DurationSeconds { get; set; }
}
