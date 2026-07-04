using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

// Corrected namespace
namespace BuildSmart.Api.GraphQL.Types;

public class UserType : ObjectType<User>
{
	protected override void Configure(IObjectTypeDescriptor<User> descriptor)
	{
		descriptor.Description("Represents a user of the application, who can be a homeowner or a tradesman.");

		descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
		descriptor.Field(u => u.FirstName).Type<NonNullType<StringType>>();
		descriptor.Field(u => u.LastName).Type<NonNullType<StringType>>();
		descriptor.Field(u => u.Email).Type<NonNullType<StringType>>();
		descriptor.Field(u => u.PhoneNumber).Type<StringType>();
		descriptor.Field(u => u.Role).Type<NonNullType<EnumType<BuildSmart.Core.Domain.Enums.UserRoleTypes>>>();
		descriptor.Field(u => u.Bio).Type<StringType>();
		descriptor.Field(u => u.Location).Type<StringType>();
		descriptor.Field(u => u.ProfilePictureUrl).Type<StringType>();
		descriptor.Field(u => u.EmailOnOfferReady).Type<NonNullType<BooleanType>>();
		descriptor.Field(u => u.EmailOnNewChatMessage).Type<NonNullType<BooleanType>>();

		descriptor.Field(u => u.HashedPassword).Ignore(); // Do not expose password hash

        descriptor.Field(u => u.HomeownerProfile).Type<HomeownerProfileType>();
        descriptor.Field(u => u.TradesmanProfile).Type<TradesmanProfileType>();

		descriptor.Field("isOnline")
			.Type<NonNullType<BooleanType>>()
			.Resolve(ctx =>
			{
				var user = ctx.Parent<User>();
				var presenceService = ctx.Service<IUserPresenceService>();
				return presenceService.IsUserOnline(user.Id.ToString());
			});

		descriptor.Field(u => u.LastSeenAt).Type<DateTimeType>();

		descriptor.Field("activeStatus")
			.Type<NonNullType<EnumType<BuildSmart.Core.Domain.Enums.UserActiveStatus>>>()
			.Resolve(ctx =>
			{
				var user = ctx.Parent<User>();
				var presenceService = ctx.Service<IUserPresenceService>();
				return presenceService.GetUserActiveStatus(user.Id.ToString(), user.LastSeenAt);
			});

		descriptor.Field("remainingAiRequests")
			.Type<NonNullType<IntType>>()
			.Resolve(ctx =>
			{
				var config = ctx.Service<IConfiguration>();
				var enableLimitsStr = config["Gemini:EnableRequestLimits"];
				var enableLimits = enableLimitsStr == null || !bool.TryParse(enableLimitsStr, out var val) || val;
				if (!enableLimits)
				{
					return 9999;
				}

				var user = ctx.Parent<User>();
				var limitStr = config["Gemini:MaxAiRequests"];
				var maxRequests = int.TryParse(limitStr, out var limitVal) ? limitVal : 20;

				var now = DateTime.UtcNow;
				var requestCount = user.AiRequestCount;
				if (user.LastAiRequestDate == null || 
					user.LastAiRequestDate.Value.Month != now.Month || 
					user.LastAiRequestDate.Value.Year != now.Year)
				{
					requestCount = 0;
				}

				return Math.Max(0, maxRequests - requestCount);
			});

		// Relationships will be configured here later
	}
}