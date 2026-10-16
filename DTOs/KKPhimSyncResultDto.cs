namespace MovieStreaming.DTOs;

public class KKPhimSyncResultDto
{
    public int PagesProcessed { get; set; }
    public int MoviesDiscovered { get; set; }
    public int EnglishMovies { get; set; }
    public int VietsubAvailable { get; set; }
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int DuplicatesPrevented { get; set; }
    public int Errors { get; set; }
    public List<string> ImportedTitles { get; set; } = new();
    public List<string> ErrorMessages { get; set; } = new();
}
