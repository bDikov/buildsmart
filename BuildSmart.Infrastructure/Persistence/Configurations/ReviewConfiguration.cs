using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
	public void Configure(EntityTypeBuilder<Review> builder)
	{
		builder.ToTable("Reviews");

		builder.HasKey(r => r.Id);

		builder.Property(r => r.Rating)
			.IsRequired();

		builder.Property(r => r.Comment)
			.HasMaxLength(1500);

		builder.HasOne(r => r.Homeowner)
			.WithMany()
			.HasForeignKey(r => r.HomeownerId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(r => r.TradesmanProfile)
			.WithMany(tp => tp.Reviews)
			.HasForeignKey(r => r.TradesmanProfileId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}