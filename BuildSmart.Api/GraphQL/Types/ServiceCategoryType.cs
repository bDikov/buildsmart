using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Api.GraphQL.Types;

public class ServiceCategoryType : ObjectType<ServiceCategory>
{
    protected override void Configure(IObjectTypeDescriptor<ServiceCategory> descriptor)
    {
        descriptor.Description("Represents a service category.");

        descriptor.Field(c => c.Id).Type<NonNullType<UuidType>>();
        
        descriptor.Field(c => c.Name)
            .Type<NonNullType<StringType>>()
            .ResolveWith<ServiceCategoryResolvers>(r => r.GetName(default!, default!, default!));
            
        descriptor.Field(c => c.Description)
            .Type<StringType>()
            .ResolveWith<ServiceCategoryResolvers>(r => r.GetDescription(default!, default!, default!));
            
        descriptor.Field(c => c.Status).Type<NonNullType<EnumType<Core.Domain.Enums.CategoryStatus>>>();
        descriptor.Field(c => c.IsGlobal).Type<NonNullType<BooleanType>>();
        descriptor.Field(c => c.TemplateStructure).Type<NonNullType<StringType>>();
    }
}

public class ServiceCategoryResolvers
{
    public async Task<string> GetName([Parent] ServiceCategory category, [Service] AppDbContext dbContext, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var langCode = httpContextAccessor.HttpContext?.Items["LanguageCode"]?.ToString() ?? "en";
        if (langCode != "en")
        {
            var translation = await dbContext.ServiceCategoryTranslations
                .FirstOrDefaultAsync(t => t.CategoryId == category.Id && t.LanguageCode == langCode);
            if (translation != null && !string.IsNullOrEmpty(translation.Name))
            {
                return translation.Name;
            }
        }
        return category.Name;
    }

    public async Task<string?> GetDescription([Parent] ServiceCategory category, [Service] AppDbContext dbContext, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var langCode = httpContextAccessor.HttpContext?.Items["LanguageCode"]?.ToString() ?? "en";
        if (langCode != "en")
        {
            var translation = await dbContext.ServiceCategoryTranslations
                .FirstOrDefaultAsync(t => t.CategoryId == category.Id && t.LanguageCode == langCode);
            if (translation != null && !string.IsNullOrEmpty(translation.Description))
            {
                return translation.Description;
            }
        }
        return category.Description;
    }
}