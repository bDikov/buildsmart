using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class BidItemConfiguration : IEntityTypeConfiguration<BidItem>
{
    public void Configure(EntityTypeBuilder<BidItem> builder)
    {
        builder.ToTable("BidItems");

        builder.HasKey(bi => bi.Id);

        builder.Property(bi => bi.Comment)
            .HasMaxLength(1000);

        builder.ComplexProperty(bi => bi.Price, amountBuilder =>
        {
            amountBuilder.Property(a => a.Total).HasColumnName("Price_Total").HasPrecision(18, 2);
            amountBuilder.Property(a => a.Subtotal).HasColumnName("Price_Subtotal").HasPrecision(18, 2);
            amountBuilder.Property(a => a.Tax).HasColumnName("Price_Tax").HasPrecision(18, 2);
            amountBuilder.Property(a => a.Currency).HasColumnName("Price_Currency").HasMaxLength(3);
        });

        builder.HasOne(bi => bi.Bid)
            .WithMany(b => b.BidItems)
            .HasForeignKey(bi => bi.BidId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bi => bi.JobTask)
            .WithMany(jt => jt.BidItems)
            .HasForeignKey(bi => bi.JobTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}