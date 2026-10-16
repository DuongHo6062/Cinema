using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("Favorites");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // Foreign keys
        builder.HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Movie)
            .WithMany(m => m.Favorites)
            .HasForeignKey(f => f.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one favorite per user per movie
        builder.HasIndex(f => new { f.UserId, f.MovieId })
            .IsUnique();

        // Composite index for fast CreatedAt descending pagination
        builder.HasIndex(f => new { f.UserId, f.CreatedAt });
    }
}
