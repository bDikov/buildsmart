using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class ProjectMessageConfiguration : IEntityTypeConfiguration<ProjectMessage>
{
    public void Configure(EntityTypeBuilder<ProjectMessage> builder)
    {
        builder.ToTable("ProjectMessages");

        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.MessageText)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(pm => pm.CreatedAt)
            .IsRequired()
            .HasConversion(
                v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.HasOne(pm => pm.Project)
            .WithMany()
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.Sender)
            .WithMany()
            .HasForeignKey(pm => pm.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
