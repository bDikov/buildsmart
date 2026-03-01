using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class CertificationType : ObjectType<Certification>
{
	protected override void Configure(IObjectTypeDescriptor<Certification> descriptor)
	{
		descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
		descriptor.Field(c => c.Title).Type<NonNullType<StringType>>();
		descriptor.Field(c => c.DocumentUrl).Type<NonNullType<StringType>>();
		descriptor.Field(c => c.IssuedAt).Type<NonNullType<DateTimeType>>();
		descriptor.Field(c => c.ExpiresAt).Type<DateTimeType>();
	}
}