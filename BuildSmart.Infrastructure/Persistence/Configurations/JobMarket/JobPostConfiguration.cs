using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations.JobMarket;

public class JobPostConfiguration : IEntityTypeConfiguration<JobPost>
{
    public void Configure(EntityTypeBuilder<JobPost> builder)
    {
        builder.ToTable("JobPosts");

        builder.HasKey(jp => jp.Id);

        builder.Property(jp => jp.Title)
            .HasMaxLength(200)
            .IsRequired();

        // Use JSONB for JobDetails
        builder.Property(jp => jp.JobDetails)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(jp => jp.Description)
            .HasMaxLength(2000);

        builder.Property(jp => jp.Location)
            .HasMaxLength(255);

        // Configure ImageUrls as a simple JSON array in the DB
        builder.Property(jp => jp.ImageUrls)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            )
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));
        
        builder.Property(jp => jp.ImageUrls).HasColumnType("jsonb");

        builder.OwnsOne(jp => jp.EstimatedBudget, amount =>
        {
            amount.Property(a => a.Subtotal).HasColumnName("EstimatedBudget_Subtotal");
            amount.Property(a => a.Tax).HasColumnName("EstimatedBudget_Tax");
            amount.Property(a => a.Total).HasColumnName("EstimatedBudget_Total");
            amount.Property(a => a.Currency).HasColumnName("EstimatedBudget_Currency").HasMaxLength(3);
        });

        // Relationships
        builder.HasOne(jp => jp.HomeownerProfile)
            .WithMany() // No collection on HomeownerProfile yet, optional
            .HasForeignKey(jp => jp.HomeownerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(jp => jp.ServiceCategory)
            .WithMany()
            .HasForeignKey(jp => jp.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Project relationship
        builder.HasOne(jp => jp.Project)
            .WithMany(p => p.JobPosts)
            .HasForeignKey(jp => jp.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(jp => jp.Questions)
            .WithOne(q => q.JobPost)
            .HasForeignKey(q => q.JobPostId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Bids relationship (explicitly configured if needed, though convention works)
        builder.HasMany(jp => jp.Bids)
            .WithOne(b => b.JobPost)
            .HasForeignKey(b => b.JobPostId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}