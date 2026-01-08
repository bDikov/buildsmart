using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class TradesmanProfileConfiguration : IEntityTypeConfiguration<TradesmanProfile>
{
	public void Configure(EntityTypeBuilder<TradesmanProfile> builder)
	{
		builder.ToTable("TradesmanProfiles");

		builder.HasKey(tp => tp.Id);

		builder.Property(tp => tp.AverageRating)
			.HasPrecision(3, 2);

		builder.HasOne(tp => tp.User)
			.WithOne(u => u.TradesmanProfile)
			.HasForeignKey<TradesmanProfile>(tp => tp.UserId);

        // Configure Skills (One-to-Many to the Join Entity)
        builder.HasMany(tp => tp.Skills)
            .WithOne(ts => ts.TradesmanProfile)
            .HasForeignKey(ts => ts.TradesmanProfileId)
            .OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(tp => tp.PortfolioEntries)
			.WithOne(pe => pe.TradesmanProfile)
			.HasForeignKey(pe => pe.TradesmanProfileId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(tp => tp.Bookings)
			.WithOne(b => b.TradesmanProfile)
			.HasForeignKey(b => b.TradesmanProfileId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(tp => tp.Reviews)
			.WithOne(r => r.TradesmanProfile)
			.HasForeignKey(r => r.TradesmanProfileId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}