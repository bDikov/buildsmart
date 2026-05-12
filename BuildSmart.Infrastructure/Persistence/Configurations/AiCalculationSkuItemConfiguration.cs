using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class AiCalculationSkuItemConfiguration : IEntityTypeConfiguration<AiCalculationSkuItem>
{
    public void Configure(EntityTypeBuilder<AiCalculationSkuItem> builder)
    {
        builder.ToTable("AiCalculationSkuItems");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Quantity).HasColumnType("numeric(18,2)");
        builder.Property(s => s.EstimatedPrice).HasColumnType("numeric(18,2)");
        
        builder.HasOne(s => s.ServiceSku)
            .WithMany()
            .HasForeignKey(s => s.ServiceSkuId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}