using BuildSmart.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using NCalc;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace BuildSmart.Infrastructure.Services;

public class PricingEngine : IPricingEngine
{
    private readonly ILogger<PricingEngine> _logger;

    public PricingEngine(ILogger<PricingEngine> logger)
    {
        _logger = logger;
    }

    public decimal CalculateQuantity(string calculationFormula, string jobDetailsJson)
    {
        if (string.IsNullOrWhiteSpace(calculationFormula)) return 0m;
        if (calculationFormula.Trim() == "1") return 1m;
        if (calculationFormula.Trim() == "0") return 0m;

        try
        {
            var expr = new Expression(calculationFormula);

            // Parse jobDetailsJson into Dictionary
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(jobDetailsJson))
            {
                using var doc = JsonDocument.Parse(jobDetailsJson);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Number)
                    {
                        parameters[prop.Name] = prop.Value.GetDecimal();
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        parameters[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                         parameters[prop.Name] = prop.Value.GetRawText(); // String representation of array
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                    {
                         parameters[prop.Name] = prop.Value.GetBoolean();
                    }
                }
            }

            expr.EvaluateParameter += (name, args) =>
            {
                if (parameters.TryGetValue(name, out var val))
                {
                    args.Result = val;
                }
                else
                {
                    args.Result = 0m; // Default missing numbers to 0
                }
            };

            expr.EvaluateFunction += (name, args) =>
            {
                if (name.Equals("Contains", StringComparison.OrdinalIgnoreCase))
                {
                    var source = args.Parameters[0].Evaluate()?.ToString() ?? string.Empty;
                    var target = args.Parameters[1].Evaluate()?.ToString() ?? string.Empty;
                    args.Result = source.Contains(target, StringComparison.OrdinalIgnoreCase);
                }
                else if (name.Equals("Count", StringComparison.OrdinalIgnoreCase))
                {
                    var source = args.Parameters[0].Evaluate()?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(source))
                    {
                         args.Result = 0m;
                    }
                    else if (source.StartsWith("["))
                    {
                         using var arrDoc = JsonDocument.Parse(source);
                         args.Result = (decimal)arrDoc.RootElement.GetArrayLength();
                    }
                    else
                    {
                         // comma separated fallback
                         args.Result = (decimal)source.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                    }
                }
            };

            var result = expr.Evaluate();
            return Convert.ToDecimal(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating formula: {Formula} with JSON: {Json}", calculationFormula, jobDetailsJson);
            return 0m;
        }
    }
}
