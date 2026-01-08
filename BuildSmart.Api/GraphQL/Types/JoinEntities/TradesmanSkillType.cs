using BuildSmart.Core.Domain.Entities.JoinEntities;

namespace BuildSmart.Api.GraphQL.Types;

public class TradesmanSkillType : ObjectType<TradesmanSkill>
{
    protected override void Configure(IObjectTypeDescriptor<TradesmanSkill> descriptor)
    {
        descriptor.Description("Represents a specific skill or service category offered by a tradesman.");

        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.VerificationStatus).Description("The verification level of this skill (Unverified, PortfolioVerified, etc.)");
        
        // Expose the related ServiceCategory
        descriptor.Field(t => t.ServiceCategory).Type<NonNullType<ObjectType<BuildSmart.Core.Domain.Entities.ServiceCategory>>>();
    }
}
