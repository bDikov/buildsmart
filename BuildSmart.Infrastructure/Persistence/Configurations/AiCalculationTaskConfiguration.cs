using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class AiCalculationTaskConfiguration : IEntityTypeConfiguration<AiCalculationTask>
{
    public void Configure(EntityTypeBuilder<AiCalculationTask> builder)
    {
        builder.ToTable("AiCalculationTasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.EstimatedPrice).HasColumnType("numeric(18,2)");

        builder.HasMany(t => t.SkuItems)
            .WithOne(s => s.AiCalculationTask)
            .HasForeignKey(s => s.AiCalculationTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.AcceptanceCriteria)
            .WithOne(c => c.AiCalculationTask)
            .HasForeignKey(c => c.AiCalculationTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}