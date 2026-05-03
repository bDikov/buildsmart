using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Api.GraphQL.Types;

public class ServiceSkuType : ObjectType<ServiceSku>
{
    protected override void Configure(IObjectTypeDescriptor<ServiceSku> descriptor)
    {
        descriptor.Description("Represents a billable SKU item for a specific service category.");

        descriptor.Field(s => s.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(s => s.ServiceCategoryId).Type<NonNullType<UuidType>>();
        descriptor.Field(s => s.SkuCode).Type<NonNullType<StringType>>();
        
        descriptor.Field(s => s.Name)
            .Type<NonNullType<StringType>>()
            .ResolveWith<ServiceSkuResolvers>(r => r.GetName(default!, default!, default!));
            
        descriptor.Field(s => s.Description)
            .Type<StringType>()
            .ResolveWith<ServiceSkuResolvers>(r => r.GetDescription(default!, default!, default!));
            
        descriptor.Field(s => s.BasePrice).Type<NonNullType<DecimalType>>();
        
        descriptor.Field(s => s.UnitType)
            .Type<NonNullType<StringType>>()
            .ResolveWith<ServiceSkuResolvers>(r => r.GetUnitType(default!, default!, default!));
    }
}

public class ServiceSkuResolvers
{
    public async Task<string> GetName([Parent] ServiceSku sku, [Service] AppDbContext dbContext, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var langCode = httpContextAccessor.HttpContext?.Items["LanguageCode"]?.ToString() ?? "en";
        if (langCode != "en")
        {
            var translation = await dbContext.ServiceSkuTranslations
                .FirstOrDefaultAsync(t => t.SkuId == sku.Id && t.LanguageCode == langCode);
            if (translation != null && !string.IsNullOrEmpty(translation.Name))
            {
                return translation.Name;
            }
        }
        return sku.Name;
    }

    public async Task<string> GetDescription([Parent] ServiceSku sku, [Service] AppDbContext dbContext, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var langCode = httpContextAccessor.HttpContext?.Items["LanguageCode"]?.ToString() ?? "en";
        if (langCode != "en")
        {
            var translation = await dbContext.ServiceSkuTranslations
                .FirstOrDefaultAsync(t => t.SkuId == sku.Id && t.LanguageCode == langCode);
            if (translation != null && !string.IsNullOrEmpty(translation.Description))
            {
                return translation.Description;
            }
        }
        return sku.Description;
    }
    
    public async Task<string> GetUnitType([Parent] ServiceSku sku, [Service] AppDbContext dbContext, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var langCode = httpContextAccessor.HttpContext?.Items["LanguageCode"]?.ToString() ?? "en";
        if (langCode != "en")
        {
            var translation = await dbContext.ServiceSkuTranslations
                .FirstOrDefaultAsync(t => t.SkuId == sku.Id && t.LanguageCode == langCode);
            if (translation != null && !string.IsNullOrEmpty(translation.UnitType))
            {
                return translation.UnitType;
            }
        }
        return sku.UnitType;
    }
}
