using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Configurations;

public class EpisodeConfiguration : IEntityTypeConfiguration<Episode>
{
    public void Configure(EntityTypeBuilder<Episode> builder)
    {
        builder.ToTable("Episodes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.VideoUrl)
            .HasMaxLength(500);

        builder.Property(e => e.EpisodeNumber)
            .IsRequired();

        builder.Property(e => e.SeasonNumber)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(e => e.Slug)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(e => e.DisplayOrder)
            .IsRequired(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.MovieId);

        builder.HasIndex(e => new { e.MovieId, e.SeasonNumber, e.EpisodeNumber })
            .IsUnique();

        builder.HasOne(e => e.Movie)
            .WithMany(m => m.Episodes)
            .HasForeignKey(e => e.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed 1 episode for movie 1
        builder.HasData(
            new Episode
            {
                Id = 1,
                MovieId = 1,
                EpisodeNumber = 1,
                SeasonNumber = 1,
                Title = "Full Movie",
                Slug = "full-movie",
                DisplayOrder = 1,
                Description = "Full feature presentation of Inception.",
                VideoUrl = "https://example.com/videos/inception.mp4",
                Duration = 148,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
