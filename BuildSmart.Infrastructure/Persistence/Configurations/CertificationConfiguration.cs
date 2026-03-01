using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class CertificationConfiguration : IEntityTypeConfiguration<Certification>
{
	public void Configure(EntityTypeBuilder<Certification> builder)
	{
		builder.ToTable("Certifications");

		builder.HasKey(c => c.Id);

		builder.Property(c => c.Title)
			.IsRequired()
			.HasMaxLength(200);

		builder.Property(c => c.DocumentUrl)
			.IsRequired();

		builder.HasOne(c => c.TradesmanProfile)
			.WithMany(tp => tp.Certifications)
			.HasForeignKey(c => c.TradesmanProfileId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}