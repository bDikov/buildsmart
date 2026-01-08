using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		// Configure table name (optional, by default will be "Users")
		builder.ToTable("Users");

		// Configure primary key
		builder.HasKey(u => u.Id);

		// Configure properties
		builder.Property(u => u.FirstName)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(u => u.LastName)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(u => u.Email)
			.HasMaxLength(255)
			.IsRequired();

		builder.HasIndex(u => u.Email)
			.IsUnique();

		builder.Property(u => u.HashedPassword)
			.IsRequired();

		builder.Property(u => u.PhoneNumber)
			.HasMaxLength(20);

		// Configure relationships

		// One-to-One: User -> TradesmanProfile
		builder.HasOne(u => u.TradesmanProfile)
			.WithOne(tp => tp.User)
			.HasForeignKey<TradesmanProfile>(tp => tp.UserId)
			.OnDelete(DeleteBehavior.Cascade); // If a User is deleted, their profile is also deleted.

        // One-to-One: User -> HomeownerProfile
        builder.HasOne(u => u.HomeownerProfile)
            .WithOne(hp => hp.User)
            .HasForeignKey<HomeownerProfile>(hp => hp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
	}
}