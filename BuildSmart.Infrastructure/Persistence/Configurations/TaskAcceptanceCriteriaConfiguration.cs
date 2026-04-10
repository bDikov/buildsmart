using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class TaskAcceptanceCriteriaConfiguration : IEntityTypeConfiguration<TaskAcceptanceCriteria>
{
    public void Configure(EntityTypeBuilder<TaskAcceptanceCriteria> builder)
    {
        builder.ToTable("TaskAcceptanceCriteria");

        builder.HasKey(ac => ac.Id);

        builder.Property(ac => ac.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasOne(ac => ac.JobTask)
            .WithMany(jt => jt.AcceptanceCriteria)
            .HasForeignKey(ac => ac.JobTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Database Optimizations
        builder.HasIndex(ac => ac.JobTaskId);
    }
}