using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Configurations;

public class WatchHistoryConfiguration : IEntityTypeConfiguration<WatchHistory>
{
    public void Configure(EntityTypeBuilder<WatchHistory> builder)
    {
        builder.ToTable("WatchHistories");

        builder.HasKey(wh => wh.Id);

        builder.Property(wh => wh.ProgressSeconds)
            .IsRequired();

        builder.Property(wh => wh.DurationSeconds)
            .IsRequired(false);

        builder.Property(wh => wh.LastWatchedAt)
            .IsRequired();

        builder.Property(wh => wh.CreatedAt)
            .IsRequired();

        builder.Property(wh => wh.UpdatedAt)
            .IsRequired();

        // Foreign keys
        builder.HasOne(wh => wh.User)
            .WithMany(u => u.WatchHistories)
            .HasForeignKey(wh => wh.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wh => wh.Episode)
            .WithMany(e => e.WatchHistories)
            .HasForeignKey(wh => wh.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wh => wh.Movie)
            .WithMany(m => m.WatchHistories)
            .HasForeignKey(wh => wh.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one watch record per user per episode
        builder.HasIndex(wh => new { wh.UserId, wh.EpisodeId })
            .IsUnique();

        // Index on UserId for user history filtering
        builder.HasIndex(wh => wh.UserId);

        // Composite index for fast LastWatchedAt descending pagination
        builder.HasIndex(wh => new { wh.UserId, wh.LastWatchedAt });
    }
}
