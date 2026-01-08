using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class PortfolioEntryConfiguration : IEntityTypeConfiguration<PortfolioEntry>
{
	public void Configure(EntityTypeBuilder<PortfolioEntry> builder)
	{
		builder.ToTable("PortfolioEntries");

		builder.HasKey(pe => pe.Id);

		builder.Property(pe => pe.Title)
			.HasMaxLength(200)
			.IsRequired();

		builder.Property(pe => pe.Description)
			.HasMaxLength(1000);

		builder.Property(pe => pe.ImageUrl)
			.HasMaxLength(1024)
			.IsRequired();

		builder.Property(pe => pe.VideoUrl)
			.HasMaxLength(1024);
	}
}