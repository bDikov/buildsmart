using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
	public void Configure(EntityTypeBuilder<ServiceCategory> builder)
	{
		builder.ToTable("ServiceCategories");

		builder.HasKey(sc => sc.Id);

		builder.Property(sc => sc.Name)
			.HasMaxLength(150)
			.IsRequired();

		builder.HasIndex(sc => sc.Name)
			.IsUnique();

		builder.Property(sc => sc.Description)
			.HasMaxLength(500);

        // Map TemplateStructure to JSONB for PostgreSQL
        builder.Property(sc => sc.TemplateStructure)
            .HasColumnType("jsonb")
            .IsRequired();

        // Relationship is now handled via TradesmanSkill join entity
	}
}