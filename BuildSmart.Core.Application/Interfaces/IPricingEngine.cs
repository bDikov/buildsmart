using System.Collections.Generic;

namespace BuildSmart.Core.Application.Interfaces;

public interface IPricingEngine
{
    decimal CalculateQuantity(string calculationFormula, string jobDetailsJson);
}
