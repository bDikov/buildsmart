using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class LocalizationResourceConfiguration : IEntityTypeConfiguration<LocalizationResource>
{
    public void Configure(EntityTypeBuilder<LocalizationResource> builder)
    {
        builder.ToTable("LocalizationResources");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(r => r.Culture)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(r => r.Value)
            .IsRequired();

        builder.HasIndex(r => new { r.Key, r.Culture })
            .IsUnique();

        builder.HasIndex(r => r.Key);
    }
}
