using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        // Seed data mentioned in Section 5.7
        builder.HasData(
            new Category { Id = 1, Name = "Trending", Slug = "trending", Description = "Currently trending titles" },
            new Category { Id = 2, Name = "Popular", Slug = "popular", Description = "Most watched titles of all time" },
            new Category { Id = 3, Name = "Featured", Slug = "featured", Description = "Curated titles hand-picked by editors" },
            new Category { Id = 4, Name = "Latest", Slug = "latest", Description = "Recently released additions to the platform" }
        );
    }
}
