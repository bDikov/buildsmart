using BuildSmart.Core.Domain.Entities.JoinEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations.JoinEntities;

public class TradesmanSkillConfiguration : IEntityTypeConfiguration<TradesmanSkill>
{
    public void Configure(EntityTypeBuilder<TradesmanSkill> builder)
    {
        builder.ToTable("TradesmanSkills");

        builder.HasKey(ts => ts.Id);

        // Relationships
        builder.HasOne(ts => ts.TradesmanProfile)
            .WithMany(tp => tp.Skills)
            .HasForeignKey(ts => ts.TradesmanProfileId);

        builder.HasOne(ts => ts.ServiceCategory)
            .WithMany() // ServiceCategory currently doesn't have a collection of Skills back to Tradesman (uni-directional here is fine for now)
            .HasForeignKey(ts => ts.ServiceCategoryId);
    }
}
