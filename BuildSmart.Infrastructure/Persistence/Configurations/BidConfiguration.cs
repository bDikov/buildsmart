using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("Bids");

        builder.HasKey(b => b.Id);

        builder.OwnsOne(b => b.Amount, amountBuilder =>
        {
            amountBuilder.Property(a => a.Total).HasPrecision(18, 2);
            amountBuilder.Property(a => a.Subtotal).HasPrecision(18, 2);
            amountBuilder.Property(a => a.Tax).HasPrecision(18, 2);
            amountBuilder.Property(a => a.Currency).HasMaxLength(3);
        });

        builder.Property(b => b.Comment)
            .HasMaxLength(1000);

        builder.HasOne(b => b.JobPost)
            .WithMany(jp => jp.Bids)
            .HasForeignKey(b => b.JobPostId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade so deleting a JobPost cleans up its bids

        builder.HasOne(b => b.TradesmanProfile)
            .WithMany()
            .HasForeignKey(b => b.TradesmanProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.JobPostId, b.TradesmanProfileId })
            .IsUnique();
    }
}
