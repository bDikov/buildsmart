using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class ServiceSkuConfiguration : IEntityTypeConfiguration<ServiceSku>
{
    public void Configure(EntityTypeBuilder<ServiceSku> builder)
    {
        builder.ToTable("ServiceSkus");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SkuCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(s => s.SkuCode)
            .IsUnique();

        builder.Property(s => s.Name)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.BasePrice)
            .HasPrecision(18, 4);

        builder.Property(s => s.UnitType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.CalculationFormula)
            .HasMaxLength(1000);

        // English Translation columns
        builder.Property(s => s.EnglishName)
            .HasMaxLength(250);

        builder.Property(s => s.EnglishDescription)
            .HasMaxLength(1000);

        builder.Property(s => s.EnglishUnitType)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(s => s.ServiceCategory)
            .WithMany()
            .HasForeignKey(s => s.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
