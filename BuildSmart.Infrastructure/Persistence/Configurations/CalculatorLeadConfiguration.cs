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

        builder.Property(c => c.IsContacted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.ContactedAt);

        builder.Property(c => c.AdminNotes)
            .HasMaxLength(4000);

        builder.Property(c => c.CallOutcome)
            .HasMaxLength(50);

        builder.Property(c => c.ProspectRating);

        builder.Property(c => c.MeetingStatus)
            .HasMaxLength(50);

        builder.Property(c => c.MeetingDate);

        builder.Property(c => c.MeetingNotes)
            .HasMaxLength(2000);

        builder.Property(c => c.ReadyToStartTimeline)
            .HasMaxLength(100);

        builder.Property(c => c.FollowUpDate);

        builder.Property(c => c.FollowUpReminderSent)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(c => new { c.UtmSource, c.UtmCampaign });
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.ProspectRating);
        builder.HasIndex(c => c.MeetingStatus);
        builder.HasIndex(c => c.FollowUpDate);
    }
}
