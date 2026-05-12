using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class MilestonePaymentConfiguration : IEntityTypeConfiguration<MilestonePayment>
{
    public void Configure(EntityTypeBuilder<MilestonePayment> builder)
    {
        builder.ToTable("MilestonePayments");

        builder.HasKey(mp => mp.Id);

        builder.Property(mp => mp.Status)
            .HasConversion(
                s => s.ToString(),
                s => (MilestoneStatus)Enum.Parse(typeof(MilestoneStatus), s))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(mp => mp.StripeTransferId)
            .HasMaxLength(100);

        builder.OwnsOne(mp => mp.AmountAllocated, amountBuilder =>
        {
            amountBuilder.Property(a => a.Total).HasPrecision(18, 2);
            amountBuilder.Property(a => a.Subtotal).HasPrecision(18, 2);
            amountBuilder.Property(a => a.Tax).HasPrecision(18, 2);
            amountBuilder.Property(a => a.Currency).HasMaxLength(3);
        });

        builder.HasOne(mp => mp.Booking)
            .WithMany(b => b.MilestonePayments)
            .HasForeignKey(mp => mp.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mp => mp.JobTask)
            .WithMany()
            .HasForeignKey(mp => mp.JobTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
