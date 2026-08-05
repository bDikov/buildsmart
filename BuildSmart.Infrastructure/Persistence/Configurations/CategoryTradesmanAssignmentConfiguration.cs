using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class CategoryTradesmanAssignmentConfiguration : IEntityTypeConfiguration<CategoryTradesmanAssignment>
{
    public void Configure(EntityTypeBuilder<CategoryTradesmanAssignment> builder)
    {
        builder.ToTable("CategoryTradesmanAssignments");

        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Project)
            .WithMany()
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.JobPost)
            .WithMany()
            .HasForeignKey(a => a.JobPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ServiceCategory)
            .WithMany()
            .HasForeignKey(a => a.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Tradesman)
            .WithMany()
            .HasForeignKey(a => a.TradesmanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedByAdmin)
            .WithMany()
            .HasForeignKey(a => a.AssignedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
