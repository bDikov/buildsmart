namespace BuildSmart.Core.Application.Interfaces;

public record RenovationEstimateRequest(
    string Scope = "full", // "bathroom", "full", "finishing"
    int SelectedArea = 70,
    string BuildingStatus = "old", // "new", "rough", "old"
    string QualityTier = "standard", // "standard", "premium", "luxury"
    int BathroomCount = 1,
    int[]? BathroomSizes = null,
    bool IncludeFurniture = false,
    bool IncludeEquipment = true
);

public record RenovationEstimateResult(
    decimal MinPriceEur,
    decimal MaxPriceEur,
    decimal MinPriceBgn,
    decimal MaxPriceBgn,
    int EstimatedDays,
    decimal LaborMinEur,
    decimal LaborMaxEur,
    decimal RoughMaterialsMinEur,
    decimal RoughMaterialsMaxEur,
    decimal FinishMaterialsMinEur,
    decimal FinishMaterialsMaxEur,
    decimal RatePerSqmMinEur,
    decimal RatePerSqmMaxEur
);

public interface IRenovationEstimatorCalculator
{
    RenovationEstimateResult CalculateEstimate(RenovationEstimateRequest request);
    (decimal min, decimal max) GetCategoryBudget(int catId, int selectedArea, string buildingStatus, string qualityTier, bool includeFurniture = false);
    decimal GetSingleBathroomPriceEur(int area, bool isMax, string buildingStatus, string qualityTier, bool includeEquipment = true);
    string GetEstimatorKnowledgeSummary();
    bool IsPricingOrDimensionQuery(string message);
}
