using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(p => p.Homeowner)
            .WithMany()
            .HasForeignKey(p => p.HomeownerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.JobPosts)
            .WithOne(jp => jp.Project)
            .HasForeignKey(jp => jp.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
