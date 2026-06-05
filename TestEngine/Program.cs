using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Globalization;
using NCalc;

namespace TestEngine;

class Program
{
    static void Main()
    {
        CultureInfo.CurrentCulture = new CultureInfo("bg-BG");
        
        string formula = "if(Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)";
        string json = "{\"elec_scope\": \"Цялостна подмяна\", \"global_total_sqm\": 100}";

        var expr = new Expression(formula);
        var parameters = new Dictionary<string, object>();
        using (var doc = JsonDocument.Parse(json)) {
            foreach (var prop in doc.RootElement.EnumerateObject()) {
                if (prop.Value.ValueKind == JsonValueKind.Number) parameters[prop.Name] = prop.Value.GetDecimal();
                else if (prop.Value.ValueKind == JsonValueKind.String) parameters[prop.Name] = prop.Value.GetString();
            }
        }
        expr.EvaluateParameter += (name, args) => {
            if (parameters.TryGetValue(name, out var val)) args.Result = val;
            else args.Result = 0m;
        };
        expr.EvaluateFunction += (name, args) => {
            if (name == "Contains") {
                var source = args.Parameters[0].Evaluate()?.ToString() ?? "";
                var target = args.Parameters[1].Evaluate()?.ToString() ?? "";
                args.Result = source.Contains(target);
            }
        };
        try {
            Console.WriteLine("Result: " + expr.Evaluate());
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}