using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class ChangeOrderConfiguration : IEntityTypeConfiguration<ChangeOrder>
{
    public void Configure(EntityTypeBuilder<ChangeOrder> builder)
    {
        builder.ToTable("ChangeOrders");

        builder.HasKey(co => co.Id);

        builder.OwnsOne(co => co.NewTotalAmount, ab =>
        {
            ab.Property(a => a.Total).HasPrecision(18, 2);
            ab.Property(a => a.Subtotal).HasPrecision(18, 2);
            ab.Property(a => a.Tax).HasPrecision(18, 2);
            ab.Property(a => a.Currency).HasMaxLength(3);
        });

        builder.OwnsOne(co => co.DifferenceAmount, ab =>
        {
            ab.Property(a => a.Total).HasPrecision(18, 2);
            ab.Property(a => a.Subtotal).HasPrecision(18, 2);
            ab.Property(a => a.Tax).HasPrecision(18, 2);
            ab.Property(a => a.Currency).HasMaxLength(3);
        });

        builder.Property(co => co.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(co => co.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
        
        builder.HasOne(co => co.Booking)
            .WithMany(b => b.ChangeOrders)
            .HasForeignKey(co => co.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
