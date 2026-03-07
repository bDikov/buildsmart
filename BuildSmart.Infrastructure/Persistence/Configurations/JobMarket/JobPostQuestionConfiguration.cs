using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations.JobMarket;

public class JobPostQuestionConfiguration : IEntityTypeConfiguration<JobPostQuestion>
{
    public void Configure(EntityTypeBuilder<JobPostQuestion> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuestionText)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(q => q.AnswerText)
            .HasMaxLength(4000);

        builder.HasOne(q => q.JobPost)
            .WithMany(jp => jp.Questions)
            .HasForeignKey(q => q.JobPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.TradesmanProfile)
            .WithMany()
            .HasForeignKey(q => q.TradesmanProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(q => q.Author)
            .WithMany()
            .HasForeignKey(q => q.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing relationship for threaded replies
        builder.HasOne(q => q.ParentQuestion)
            .WithMany(q => q.Replies)
            .HasForeignKey(q => q.ParentQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for fast lookup via DataLoaders
        builder.HasIndex(q => q.JobPostId);
        builder.HasIndex(q => q.ParentQuestionId);
    }
}
