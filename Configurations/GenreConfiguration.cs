using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Configurations;

public class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.ToTable("Genres");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.Description)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(g => g.Name)
            .IsUnique();

        builder.HasIndex(g => g.Slug)
            .IsUnique();

        // Seed data requested in Section 11
        builder.HasData(
            new Genre { Id = 1, Name = "Action", Slug = "action", Description = "High-energy films featuring physical feats and combat" },
            new Genre { Id = 2, Name = "Comedy", Slug = "comedy", Description = "Humorous films designed to make audiences laugh" },
            new Genre { Id = 3, Name = "Drama", Slug = "drama", Description = "Narrative cinema focused on realistic characters and emotional themes" },
            new Genre { Id = 4, Name = "Animation", Slug = "animation", Description = "Animated movies created through illustration or 3D rendering" },
            new Genre { Id = 5, Name = "Horror", Slug = "horror", Description = "Films designed to elicit fear, suspense, and dread" },
            new Genre { Id = 6, Name = "Sci-Fi", Slug = "sci-fi", Description = "Speculative fiction based on imagined future scientific or technological advances" }
        );
    }
}
