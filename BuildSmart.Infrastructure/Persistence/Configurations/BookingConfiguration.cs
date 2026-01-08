using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
	public void Configure(EntityTypeBuilder<Booking> builder)
	{
		builder.ToTable("Bookings");

		builder.HasKey(b => b.Id);

		builder.Property(b => b.JobDescription)
			.HasMaxLength(2000); // string? is nullable, so IsRequired() is not needed

		builder.Property(b => b.Status)
			.HasConversion(
				s => s.ToString(),
				s => (BookingStatusTypes)Enum.Parse(typeof(BookingStatusTypes), s))
			.HasMaxLength(50)
			.IsRequired();

		// This configures the owned Amount Value Objects
		builder.OwnsOne(b => b.AgreedBidAmount, amountBuilder =>
		{
			amountBuilder.Property(a => a.Total).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Subtotal).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Tax).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Currency).HasMaxLength(3);
		});

        builder.OwnsOne(b => b.PlatformFeeHomeowner, amountBuilder =>
		{
			amountBuilder.Property(a => a.Total).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Subtotal).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Tax).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Currency).HasMaxLength(3);
		});

        builder.OwnsOne(b => b.PlatformFeeTradesman, amountBuilder =>
		{
			amountBuilder.Property(a => a.Total).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Subtotal).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Tax).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Currency).HasMaxLength(3);
		});

        builder.OwnsOne(b => b.TotalEscrowAmount, amountBuilder =>
		{
			amountBuilder.Property(a => a.Total).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Subtotal).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Tax).HasPrecision(18, 2);
			amountBuilder.Property(a => a.Currency).HasMaxLength(3);
		});

		// Relationship to User (Homeowner)
		builder.HasOne(b => b.Homeowner)
			.WithMany() // No navigation property on User side
			.HasForeignKey(b => b.HomeownerId)
			.OnDelete(DeleteBehavior.Restrict);

		// Relationship to TradesmanProfile
		builder.HasOne(b => b.TradesmanProfile)
			.WithMany(tp => tp.Bookings)
			.HasForeignKey(b => b.TradesmanProfileId)
			.OnDelete(DeleteBehavior.Restrict);

		// Relationship to Review
		builder.HasOne(b => b.Review)
			.WithOne(r => r.Booking)
			.HasForeignKey<Review>(r => r.BookingId);
	}
}