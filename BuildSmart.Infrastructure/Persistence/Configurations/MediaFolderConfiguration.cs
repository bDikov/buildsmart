using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class MediaFolderConfiguration : IEntityTypeConfiguration<MediaFolder>
{
    public void Configure(EntityTypeBuilder<MediaFolder> builder)
    {
        builder.ToTable("MediaFolders");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.FullPath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(f => f.IsSystem)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .IsRequired();

        builder.HasOne(f => f.Parent)
            .WithMany(p => p.SubFolders)
            .HasForeignKey(f => f.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.ParentId, f.Slug })
            .IsUnique();

        builder.HasIndex(f => f.FullPath);
    }
}
