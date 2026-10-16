using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Configurations;

public class EpisodeSourceConfiguration : IEntityTypeConfiguration<EpisodeSource>
{
    public void Configure(EntityTypeBuilder<EpisodeSource> builder)
    {
        builder.ToTable("EpisodeSources");

        builder.HasKey(es => es.Id);

        builder.Property(es => es.ServerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(es => es.Language)
            .HasMaxLength(100);

        builder.Property(es => es.SourceType)
            .HasMaxLength(50);

        builder.Property(es => es.VideoUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(es => es.EmbedUrl)
            .HasMaxLength(1000);

        builder.Property(es => es.CreatedAt)
            .IsRequired();

        builder.Property(es => es.UpdatedAt)
            .IsRequired();

        // Foreign Key
        builder.HasOne(es => es.Episode)
            .WithMany(e => e.Sources)
            .HasForeignKey(es => es.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite unique index to prevent duplicate sources per episode server and type
        builder.HasIndex(es => new { es.EpisodeId, es.ServerName, es.SourceType })
            .IsUnique();
    }
}
