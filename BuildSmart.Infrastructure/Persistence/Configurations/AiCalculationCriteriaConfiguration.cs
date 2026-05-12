using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class AiCalculationCriteriaConfiguration : IEntityTypeConfiguration<AiCalculationCriteria>
{
    public void Configure(EntityTypeBuilder<AiCalculationCriteria> builder)
    {
        builder.ToTable("AiCalculationCriteria");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Description).IsRequired().HasMaxLength(1000);
    }
}