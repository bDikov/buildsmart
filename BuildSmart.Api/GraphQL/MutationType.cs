using BuildSmart.Api.GraphQL.Types;
using Microsoft.AspNetCore.Authorization;

namespace BuildSmart.Api.GraphQL;

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor.Description("The root mutation object.");

        descriptor.Field(m => m.MigratePasswords(default!))
            .Description("Hashes all existing plain-text passwords in the database. This is a one-time operation.")
            .Authorize(roles: new[] { "Admin" }); // Only Admin

        descriptor.Field(m => m.Login(default!, default!, default!, default!))
            .Description("Authenticates a user and returns a JWT."); // No authorization

        descriptor.Field(m => m.RegisterUser(default!, default!, default!, default!, default!))
            .Description("Creates a new user in the system."); // No authorization

        descriptor.Field(m => m.CreateBooking(default!, default!, default!, default!, default!))
            .Description("Creates a new service booking request.")
            .Authorize(roles: new[] { "Homeowner" }); // Only Homeowner

        descriptor.Field(m => m.SubmitReview(default!, default!, default!, default!, default!))
            .Description("Submits a review and updates the tradesman's average rating.")
            .Authorize(roles: new[] { "Homeowner" }); // Only Homeowner

        descriptor.Field(m => m.CreateServiceCategory(default!, default!, default!, default!))
            .Description("Creates a new service category with a smart blueprint template.")
            .Authorize(roles: new[] { "Admin" }); // Only Admin

        descriptor.Field(m => m.CreateProject(default!, default!, default!, default!))
            .Description("Creates a new project for a homeowner.")
            .Authorize(roles: new[] { "Homeowner" });

        descriptor.Field(m => m.AddJobToProject(default!, default!, default!, default!, default!, default!, default!, default!, default!))
            .Description("Adds a sub-job to a project using the Wizard output.")
            .Authorize(roles: new[] { "Homeowner" });

        descriptor.Field(m => m.SubmitBid(default!, default!, default!, default!, default!, default!))
            .Description("Submits a bid for a specific job post.")
            .Authorize(roles: new[] { "Tradesman" });

        descriptor.Field(m => m.AcceptBid(default!, default!))
            .Description("Accepts a bid and creates a funded booking.")
            .Authorize(roles: new[] { "Homeowner" });
    }
}
