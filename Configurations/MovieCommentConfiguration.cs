using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Configurations;

public class MovieCommentConfiguration : IEntityTypeConfiguration<MovieComment>
{
    public void Configure(EntityTypeBuilder<MovieComment> builder)
    {
        builder.ToTable("MovieComments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Foreign keys
        builder.HasOne(c => c.User)
            .WithMany(u => u.MovieComments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Movie)
            .WithMany(m => m.MovieComments)
            .HasForeignKey(c => c.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes for movie comment listing and user query
        builder.HasIndex(c => new { c.MovieId, c.CreatedAt });
        builder.HasIndex(c => c.UserId);
    }
}
