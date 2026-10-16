using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.ToTable("Movies");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(m => m.Slug)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(m => m.OriginalTitle)
            .HasMaxLength(255);

        builder.Property(m => m.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.PosterUrl)
            .HasMaxLength(500);

        builder.Property(m => m.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(m => m.Country)
            .HasMaxLength(100);

        builder.Property(m => m.Language)
            .HasMaxLength(100);

        builder.Property(m => m.MovieType)
            .HasMaxLength(50);

        builder.Property(m => m.Status)
            .HasMaxLength(50);

        builder.Property(m => m.Quality)
            .HasMaxLength(50);

        builder.Property(m => m.Rating)
            .HasPrecision(3, 1);

        builder.Property(m => m.ExternalId)
            .HasMaxLength(100);

        builder.Property(m => m.ExternalSource)
            .HasMaxLength(50);

        builder.Property(m => m.OriginalLanguage)
            .HasMaxLength(50);

        builder.Property(m => m.IsVietsub)
            .HasDefaultValue(false);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(m => m.Slug)
            .IsUnique();

        builder.HasIndex(m => m.Title);

        builder.HasIndex(m => m.ReleaseYear);

        builder.HasIndex(m => m.MovieType);

        builder.HasIndex(m => m.CreatedAt);

        builder.HasIndex(m => m.IsVietsub);

        builder.HasIndex(m => m.Country);

        builder.HasIndex(m => m.OriginalLanguage);

        builder.HasIndex(m => new { m.ExternalSource, m.ExternalId })
            .IsUnique()
            .HasFilter("[ExternalSource] IS NOT NULL AND [ExternalId] IS NOT NULL");

        // Delete behavior for child episodes: Restrict to prevent accidental cascading deletion of movie library
        builder.HasMany(m => m.Episodes)
            .WithOne(e => e.Movie)
            .HasForeignKey(e => e.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        // Minimal seed data for Phase 1 verification
        builder.HasData(
            new Movie
            {
                Id = 1,
                Title = "Inception",
                Slug = "inception",
                OriginalTitle = "Inception",
                Description = "A thief who steals corporate secrets through the use of dream-sharing technology is given the inverse task of planting an idea into the mind of a C.E.O.",
                PosterUrl = "https://example.com/posters/inception.jpg",
                ThumbnailUrl = "https://example.com/thumbnails/inception.jpg",
                ReleaseYear = 2010,
                Duration = 148,
                Country = "United States",
                Language = "English",
                OriginalLanguage = "English",
                IsVietsub = true,
                MovieType = "Single",
                Status = "Completed",
                Quality = "HD",
                Rating = 8.8m,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
