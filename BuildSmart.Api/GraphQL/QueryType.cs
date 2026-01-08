using BuildSmart.Api.GraphQL.Types;
using Microsoft.AspNetCore.Authorization;

namespace BuildSmart.Api.GraphQL;

[Authorize] // This applies to all fields in QueryType by default
public class QueryType : ObjectType<Query>
{
	protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
	{
		descriptor.Description("The root query object.");

		descriptor.Field(q => q.GetTradesmanProfiles(default!))
			.Description("Gets a queryable list of tradesman profiles.")
			.Authorize(roles: new[] { "Homeowner", "Tradesman", "Admin" }); // Explicitly authorize for these roles

		descriptor.Field(q => q.GetCurrentUser(default!, default!))
			.Description("Gets the currently authenticated user.")
			.Type<UserType>()
			.Authorize(roles: new[] { "Homeowner", "Tradesman", "Admin" }); // Explicitly authorize for these roles
	}
}