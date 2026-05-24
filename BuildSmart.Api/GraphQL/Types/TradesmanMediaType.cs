using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class TradesmanMediaType : ObjectType<TradesmanMedia>
{
    protected override void Configure(IObjectTypeDescriptor<TradesmanMedia> descriptor)
    {
        descriptor.Description("Represents a high-quality video (Reel) or media item in the tradesman's portfolio feed.");

        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.TradesmanId).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.VideoUrl).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.ThumbnailUrl).Type<StringType>();
        descriptor.Field(t => t.IsActive).Type<NonNullType<BooleanType>>();
        descriptor.Field(t => t.CreatedAt).Type<NonNullType<DateTimeType>>();
    }
}