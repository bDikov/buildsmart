using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class FormulaConfiguration : IEntityTypeConfiguration<Formula>
{
    public void Configure(EntityTypeBuilder<Formula> builder)
    {
        builder.ToTable("Formulas");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(f => f.Name)
            .IsUnique();

        builder.Property(f => f.Description)
            .HasMaxLength(500);

        builder.Property(f => f.Expression)
            .HasMaxLength(2000)
            .IsRequired();
    }
}
