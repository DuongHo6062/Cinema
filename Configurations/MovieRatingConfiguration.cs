using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Configurations;

public class MovieRatingConfiguration : IEntityTypeConfiguration<MovieRating>
{
    public void Configure(EntityTypeBuilder<MovieRating> builder)
    {
        builder.ToTable("MovieRatings");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        // Foreign keys
        builder.HasOne(r => r.User)
            .WithMany(u => u.MovieRatings)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Movie)
            .WithMany(m => m.MovieRatings)
            .HasForeignKey(r => r.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one rating per user per movie
        builder.HasIndex(r => new { r.UserId, r.MovieId })
            .IsUnique();

        // Indexes for performance (lookup by movie, aggregate calculation, distribution)
        builder.HasIndex(r => r.MovieId);
        builder.HasIndex(r => new { r.MovieId, r.Rating });
    }
}
