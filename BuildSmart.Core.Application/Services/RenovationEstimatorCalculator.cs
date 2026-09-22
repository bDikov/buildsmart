using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BuildSmart.Core.Application.Interfaces;

namespace BuildSmart.Core.Application.Services;

public class RenovationEstimatorCalculator : IRenovationEstimatorCalculator
{
    private const decimal BgnEurRate = 1.95583m;

    public RenovationEstimateResult CalculateEstimate(RenovationEstimateRequest request)
    {
        var scope = request.Scope?.ToLowerInvariant() ?? "full";
        var status = request.BuildingStatus?.ToLowerInvariant() ?? "old";
        var tier = request.QualityTier?.ToLowerInvariant() ?? "standard";
        var area = request.SelectedArea > 0 ? request.SelectedArea : 70;
        var tierMultiplier = GetTierMultiplier(tier);

        decimal minEur;
        decimal maxEur;
        int estimatedDays;

        if (scope == "bathroom")
        {
            var count = Math.Max(1, request.BathroomCount);
            var sizes = request.BathroomSizes != null && request.BathroomSizes.Length > 0
                ? request.BathroomSizes
                : Enumerable.Repeat(4, count).ToArray();

            minEur = 0;
            maxEur = 0;
            for (int i = 0; i < count; i++)
            {
                var bArea = i < sizes.Length ? sizes[i] : 4;
                minEur += GetSingleBathroomPriceEur(bArea, false, status, tier, request.IncludeEquipment);
                maxEur += GetSingleBathroomPriceEur(bArea, true, status, tier, request.IncludeEquipment);
            }

            int bathroomStatusDays = status == "old" ? 5 : (status == "rough" ? 2 : 0);
            estimatedDays = 12 + (count * 5) + (bathroomStatusDays * count);
        }
        else
        {
            decimal baseRateMin = status == "old" ? 450m : (status == "rough" ? 400m : 370m);
            decimal baseRateMax = status == "old" ? 540m : (status == "rough" ? 470m : 440m);

            if (scope == "finishing")
            {
                baseRateMin = 370m;
                baseRateMax = 440m;
            }

            decimal cmpMin = Math.Round(area * baseRateMin * tierMultiplier, 0);
            decimal cmpMax = Math.Round(area * baseRateMax * tierMultiplier, 0);

            if (request.IncludeFurniture && scope == "full")
            {
                decimal furnMin = Math.Round(area * 230m * tierMultiplier, 0);
                decimal furnMax = Math.Round(area * 350m * tierMultiplier, 0);
                minEur = cmpMin + furnMin;
                maxEur = cmpMax + furnMax;
            }
            else
            {
                minEur = cmpMin;
                maxEur = cmpMax;
            }

            int fullStatusDays = status == "old" ? 20 : (status == "rough" ? 10 : 0);
            decimal tierTimeMultiplier = tier == "luxury" ? 1.65m : (tier == "premium" ? 1.30m : 1.0m);
            int baseDays = 40 + (int)Math.Round(area / 2.5m, 0);
            estimatedDays = (int)Math.Round(baseDays * tierTimeMultiplier, 0) + fullStatusDays;
        }

        decimal minBgn = Math.Round(minEur * BgnEurRate, 0);
        decimal maxBgn = Math.Round(maxEur * BgnEurRate, 0);

        decimal ratePerSqmMin = area > 0 ? Math.Round(minEur / area, 1) : 0;
        decimal ratePerSqmMax = area > 0 ? Math.Round(maxEur / area, 1) : 0;

        return new RenovationEstimateResult(
            MinPriceEur: minEur,
            MaxPriceEur: maxEur,
            MinPriceBgn: minBgn,
            MaxPriceBgn: maxBgn,
            EstimatedDays: estimatedDays,
            LaborMinEur: Math.Round(minEur * 0.48m, 0),
            LaborMaxEur: Math.Round(maxEur * 0.48m, 0),
            RoughMaterialsMinEur: Math.Round(minEur * 0.27m, 0),
            RoughMaterialsMaxEur: Math.Round(maxEur * 0.27m, 0),
            FinishMaterialsMinEur: Math.Round(minEur * 0.25m, 0),
            FinishMaterialsMaxEur: Math.Round(maxEur * 0.25m, 0),
            RatePerSqmMinEur: ratePerSqmMin,
            RatePerSqmMaxEur: ratePerSqmMax
        );
    }

    public (decimal min, decimal max) GetCategoryBudget(int catId, int selectedArea, string buildingStatus, string qualityTier, bool includeFurniture = false)
    {
        var tierMultiplier = GetTierMultiplier(qualityTier);
        if (catId == 9)
        {
            if (!includeFurniture) return (0, 0);
            return (
                Math.Round(selectedArea * 230m * tierMultiplier, 0),
                Math.Round(selectedArea * 350m * tierMultiplier, 0)
            );
        }

        decimal baseRateMin = buildingStatus == "old" ? 450m : (buildingStatus == "rough" ? 400m : 370m);
        decimal baseRateMax = buildingStatus == "old" ? 540m : (buildingStatus == "rough" ? 470m : 440m);

        decimal cmpMin = Math.Round(selectedArea * baseRateMin * tierMultiplier, 0);
        decimal cmpMax = Math.Round(selectedArea * baseRateMax * tierMultiplier, 0);

        decimal weight = catId switch
        {
            1 => 0.18m, // Баня и санитарен възел
            2 => 0.18m, // Шпакловка, штробове, ъгли
            3 => 0.07m, // Боядисване
            4 => 0.12m, // Гипсокартон, ниши, тавани
            5 => 0.08m, // Подови настилки и замазки
            6 => 0.13m, // Електроинсталация и осветление
            7 => 0.06m, // ВиК инсталация
            8 => 0.08m, // Интериорни врати
            _ => 0.10m  // Логистика, къртене, депониране
        };

        if (catId == 10)
        {
            decimal sum1to8Min = Math.Round(cmpMin * 0.18m, 0) * 2
                               + Math.Round(cmpMin * 0.07m, 0)
                               + Math.Round(cmpMin * 0.12m, 0)
                               + Math.Round(cmpMin * 0.08m, 0) * 2
                               + Math.Round(cmpMin * 0.13m, 0)
                               + Math.Round(cmpMin * 0.06m, 0);

            decimal sum1to8Max = Math.Round(cmpMax * 0.18m, 0) * 2
                               + Math.Round(cmpMax * 0.07m, 0)
                               + Math.Round(cmpMax * 0.12m, 0)
                               + Math.Round(cmpMax * 0.08m, 0) * 2
                               + Math.Round(cmpMax * 0.13m, 0)
                               + Math.Round(cmpMax * 0.06m, 0);

            return (cmpMin - sum1to8Min, cmpMax - sum1to8Max);
        }

        return (Math.Round(cmpMin * weight, 0), Math.Round(cmpMax * weight, 0));
    }

    public decimal GetSingleBathroomPriceEur(int area, bool isMax, string buildingStatus, string qualityTier, bool includeEquipment = true)
    {
        decimal multiplier = GetTierMultiplier(qualityTier);
        decimal baseRate = isMax ? 650m : 420m;
        decimal statusDemolition = buildingStatus == "old" ? (isMax ? 650m : 450m) : (buildingStatus == "rough" ? (isMax ? 300m : 200m) : 0m);
        decimal fixedEquipment = includeEquipment ? (isMax ? 2600m : 1800m) : 0m;

        return Math.Round((area * baseRate + fixedEquipment + statusDemolition) * multiplier, 0);
    }

    private static decimal GetTierMultiplier(string? tier) => tier?.ToLowerInvariant() switch
    {
        "luxury" => 1.85m,
        "premium" => 1.25m,
        _ => 1.0m
    };

    public string GetEstimatorKnowledgeSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("ОФИЦИАЛНИ ТАРИФИ И БЕНЧМАРКОВЕ ОТ ON-LINE КАЛКУЛАТОРА НА BUILDSMART (https://buildsmart.bg/renovation-estimator):");
        sb.AppendLine("1. ЦЯЛОСТЕН РЕМОНТ (труд + черни материали) за София и страната:");
        sb.AppendLine("   - Довършителен ремонт / Ново строителство (шпакловка и замазка): €370 – €440 / кв.м (724 – 860 лв./кв.м).");
        sb.AppendLine("   - Ремонт 'на тапа' (БДС груб строеж): €400 – €470 / кв.м (782 – 919 лв./кв.м).");
        sb.AppendLine("   - Цялостен ремонт на старо жилище (панел/тухла с пълно къртене и извозване): €450 – €540 / кв.м (880 – 1056 лв./кв.м).");
        sb.AppendLine("   - Мебели по поръчка (кухня, гардероби до таван, портманто): добавя се €230 – €350 / кв.м.");
        sb.AppendLine("2. РЕМОНТ НА БАНЯ (стандартна баня ~4 кв.м):");
        sb.AppendLine("   - Труд и черни материали (ВиК, хидроизолация, нивелиране, лепене): €1,700 – €2,600.");
        sb.AppendLine("   - Санитарно оборудване (вградена структура, линеен сифон, санитарен фаянс, смесители): €1,800 – €2,600.");
        sb.AppendLine("   - Демонтаж / къртене на стари плочки (при старо строителство): €450 – €650.");
        sb.AppendLine("   - Общо за завършена баня 'до ключ': €3,500 – €5,200 (ново строителство) или €4,000 – €5,800 (старо строителство).");
        sb.AppendLine("3. КОЕФИЦИЕНТИ ЗА НИВО НА ИЗПЪЛНЕНИЕ (КЛАС):");
        sb.AppendLine("   - Стандарт (базов висок клас, доказани качествени материали): 1.0x");
        sb.AppendLine("   - Премиум (дизайнерски детайли, скрито LED осветление, широкоформатен гранитогрес): 1.25x");
        sb.AppendLine("   - Лукс (висок клас камък, сложни интериорни конструкции, смарт детайли): 1.85x");
        sb.AppendLine("4. СТРУКТУРА НА РАЗХОДИТЕ:");
        sb.AppendLine("   - Труд на сертифицирани бригади: ~48%");
        sb.AppendLine("   - Черни строителни материали (мазилки, замазки, кабели, тръби, лепила): ~27%");
        sb.AppendLine("   - Финишни покрития и оборудване (бои, настилки, санитария): ~25%");
        sb.AppendLine("5. ФИРМЕНИ ГАРАНЦИИ НА BUILDSMART:");
        sb.AppendLine("   - 0% аванс за труд: Клиентът заплаща единствено след изпълнен и одобрен етап с двустранен приемо-предавателен протокол.");
        sb.AppendLine("   - Фиксирана крайна цена по договор (КСС) без скрити оскъпявания.");
        sb.AppendLine("   - Срокове: баня 12–20 работни дни; цялостен апартамент 45–75 работни дни.");
        sb.AppendLine("   - Онлайн калкулатор за подробна оферта: https://buildsmart.bg/renovation-estimator");
        return sb.ToString();
    }

    public bool IsPricingOrDimensionQuery(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var pattern = @"(цен[аеи]|стойност|струва|колко|бюджет|оферт|пари|кв\.?\s*м|квадрат|размер|евро|лева|bgn|eur|price|cost|budget|sqm)";
        return Regex.IsMatch(message, pattern, RegexOptions.IgnoreCase);
    }
}
