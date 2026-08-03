using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class CalculatorLeadConfiguration : IEntityTypeConfiguration<CalculatorLead>
{
    public void Configure(EntityTypeBuilder<CalculatorLead> builder)
    {
        builder.ToTable("CalculatorLeads");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Scope)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.BuildingStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.QualityTier)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.VerificationStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => new { c.UtmSource, c.UtmCampaign });
        builder.HasIndex(c => c.CreatedAt);
    }
}
