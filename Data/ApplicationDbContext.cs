using Microsoft.EntityFrameworkCore;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<EpisodeSource> EpisodeSources => Set<EpisodeSource>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<WatchHistory> WatchHistories => Set<WatchHistory>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<MovieRating> MovieRatings => Set<MovieRating>();
    public DbSet<MovieComment> MovieComments => Set<MovieComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
