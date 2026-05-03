using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class AiCalculationConfiguration : IEntityTypeConfiguration<AiCalculation>
{
    public void Configure(EntityTypeBuilder<AiCalculation> builder)
    {
        builder.ToTable("AiCalculations");
        builder.HasKey(a => a.Id);

        // Enforce only one AI Calculation per Project + Category
        builder.HasIndex(a => new { a.ProjectId, a.ServiceCategoryId }).IsUnique();

        builder.Property(a => a.TotalEstimatedPrice)
            .HasColumnType("numeric(18,2)");

        builder.HasMany(a => a.Tasks)
            .WithOne(t => t.AiCalculation)
            .HasForeignKey(t => t.AiCalculationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}