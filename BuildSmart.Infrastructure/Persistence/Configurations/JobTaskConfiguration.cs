using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class JobTaskConfiguration : IEntityTypeConfiguration<JobTask>
{
    public void Configure(EntityTypeBuilder<JobTask> builder)
    {
        builder.ToTable("JobTasks");

        builder.HasKey(jt => jt.Id);

        builder.Property(jt => jt.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(jt => jt.Description)
            .HasMaxLength(2000);

        builder.HasOne(jt => jt.JobPost)
            .WithMany(jp => jp.JobTasks)
            .HasForeignKey(jt => jt.JobPostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Database Optimizations: Indexes for querying tasks by job post and sequence order
        builder.HasIndex(jt => jt.JobPostId);
        builder.HasIndex(jt => new { jt.JobPostId, jt.SequenceOrder });
    }
}