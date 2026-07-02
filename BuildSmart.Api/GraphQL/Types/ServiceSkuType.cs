using BuildSmart.Core.Domain.Entities;
using HotChocolate.Types;

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
            .ResolveWith<ServiceSkuResolvers>(r => r.GetName(default!, default!));
            
        descriptor.Field(s => s.Description)
            .Type<StringType>()
            .ResolveWith<ServiceSkuResolvers>(r => r.GetDescription(default!, default!));
            
        descriptor.Field(s => s.BasePrice).Type<NonNullType<DecimalType>>();
        
        descriptor.Field(s => s.UnitType)
            .Type<NonNullType<StringType>>()
            .ResolveWith<ServiceSkuResolvers>(r => r.GetUnitType(default!, default!));

        descriptor.Field(s => s.EnglishName).Type<StringType>();
        descriptor.Field(s => s.EnglishDescription).Type<StringType>();
        descriptor.Field(s => s.EnglishUnitType).Type<StringType>();

        descriptor.Field(s => s.CalculationFormula).Type<StringType>();
    }
}

public class ServiceSkuResolvers
{
    public string GetName([Parent] ServiceSku sku, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var langCode = httpContextAccessor.HttpContext?.Items["LanguageCode"]?.ToString() ?? "en";
        if (langCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrEmpty(sku.EnglishName) ? sku.EnglishName : sku.Name;
        }
        return sku.Name;
    }

    public string GetDescription([Parent] ServiceSku sku, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var langCode = httpContextAccessor.HttpContext?.Items["LanguageCode"]?.ToString() ?? "en";
        if (langCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrEmpty(sku.EnglishDescription) ? sku.EnglishDescription : sku.Description;
        }
        return sku.Description;
    }
    
    public string GetUnitType([Parent] ServiceSku sku, [Service] IHttpContextAccessor httpContextAccessor)
    {
        var langCode = httpContextAccessor.HttpContext?.Items["LanguageCode"]?.ToString() ?? "en";
        if (langCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrEmpty(sku.EnglishUnitType) ? sku.EnglishUnitType : sku.UnitType;
        }
        return sku.UnitType;
    }
}
