using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations.JobMarket;

public class JobPostFeedbackConfiguration : IEntityTypeConfiguration<JobPostFeedback>
{
    public void Configure(EntityTypeBuilder<JobPostFeedback> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.JobPost)
            .WithMany(jp => jp.Feedbacks)
            .HasForeignKey(f => f.JobPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Author)
            .WithMany()
            .HasForeignKey(f => f.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing relationship for threaded replies
        builder.HasOne(f => f.ParentFeedback)
            .WithMany(f => f.Replies)
            .HasForeignKey(f => f.ParentFeedbackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
