using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class LandingPageContentConfiguration : IEntityTypeConfiguration<LandingPageContent>
{
    public void Configure(EntityTypeBuilder<LandingPageContent> builder)
    {
        builder.ToTable("LandingPages");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.Slug)
            .IsUnique();

        builder.Property(e => e.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.PageType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.TitleBg)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.TitleEn)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.SubtitleBg)
            .HasMaxLength(1000);

        builder.Property(e => e.SubtitleEn)
            .HasMaxLength(1000);

        builder.Property(e => e.BadgeBg)
            .HasMaxLength(100);

        builder.Property(e => e.BadgeEn)
            .HasMaxLength(100);

        builder.Property(e => e.HeroImageUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.HeroVideoUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.CtaTextBg)
            .HasMaxLength(300);

        builder.Property(e => e.CtaTextEn)
            .HasMaxLength(300);

        builder.Property(e => e.CtaLink)
            .HasMaxLength(500);

        builder.Property(e => e.MetaTitleBg)
            .HasMaxLength(300);

        builder.Property(e => e.MetaTitleEn)
            .HasMaxLength(300);

        builder.Property(e => e.MetaDescriptionBg)
            .HasMaxLength(1000);

        builder.Property(e => e.MetaDescriptionEn)
            .HasMaxLength(1000);

        builder.HasIndex(e => e.PageType);
        builder.HasIndex(e => e.IsPublished);
    }
}
