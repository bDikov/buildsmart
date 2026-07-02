using BuildSmart.Core.Domain.Entities;
using HotChocolate.Types;

namespace BuildSmart.Api.GraphQL.Types;

public class ServiceCategoryType : ObjectType<ServiceCategory>
{
    protected override void Configure(IObjectTypeDescriptor<ServiceCategory> descriptor)
    {
        descriptor.Description("Represents a service category.");

        descriptor.Field(c => c.Id).Type<NonNullType<UuidType>>();
        
        descriptor.Field(c => c.Name)
            .Type<NonNullType<StringType>>()
            .ResolveWith<ServiceCategoryResolvers>(r => r.GetName(default!, default!));
            
        descriptor.Field(c => c.Description)
            .Type<StringType>()
            .ResolveWith<ServiceCategoryResolvers>(r => r.GetDescription(default!, default!));

        descriptor.Field(c => c.EnglishName).Type<StringType>();
        descriptor.Field(c => c.EnglishDescription).Type<StringType>();
            
        descriptor.Field(c => c.Status).Type<NonNullType<EnumType<Core.Domain.Enums.CategoryStatus>>>();
        descriptor.Field(c => c.IsGlobal).Type<NonNullType<BooleanType>>();
        descriptor.Field(c => c.Type).Type<NonNullType<EnumType<Core.Domain.Enums.CategoryType>>>();
        descriptor.Field(c => c.TemplateStructure).Type<NonNullType<StringType>>();
    }
}

public class ServiceCategoryResolvers
{
    public string GetName([Parent] ServiceCategory category, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var langCode = httpContextAccessor.HttpContext?.Items["LanguageCode"]?.ToString() ?? "bg";
        if (langCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrEmpty(category.EnglishName) ? category.EnglishName : category.Name;
        }
        return category.Name;
    }

    public string? GetDescription([Parent] ServiceCategory category, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var langCode = httpContextAccessor.HttpContext?.Items["LanguageCode"]?.ToString() ?? "bg";
        if (langCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrEmpty(category.EnglishDescription) ? category.EnglishDescription : category.Description;
        }
        return category.Description;
    }
}