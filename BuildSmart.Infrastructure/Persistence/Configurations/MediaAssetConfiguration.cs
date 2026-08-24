using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.R2Key)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.PublicUrl)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.ThumbnailUrl)
            .HasMaxLength(1000);

        builder.Property(a => a.MediaType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.SizeBytes)
            .IsRequired();

        builder.Property(a => a.AltTextBg)
            .HasMaxLength(255);

        builder.Property(a => a.AltTextEn)
            .HasMaxLength(255);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        builder.HasOne(a => a.Folder)
            .WithMany(f => f.Assets)
            .HasForeignKey(a => a.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.UploaderUser)
            .WithMany()
            .HasForeignKey(a => a.UploaderUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.R2Key)
            .IsUnique();

        builder.HasIndex(a => a.FolderId);
        builder.HasIndex(a => a.MediaType);
    }
}
