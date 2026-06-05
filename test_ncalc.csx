#r "nuget: NCalcSync, 3.8.0"
using System;
using System.Collections.Generic;
using System.Text.Json;
using NCalc;

string formula = "if(Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)";
string json = "{\"elec_scope\": \"Цялостна\", \"global_total_sqm\": 100}";

var expr = new Expression(formula);
var parameters = new Dictionary<string, object>();
using (var doc = JsonDocument.Parse(json)) {
    foreach (var prop in doc.RootElement.EnumerateObject()) {
        if (prop.Value.ValueKind == JsonValueKind.Number) parameters[prop.Name] = prop.Value.GetDecimal();
        else if (prop.Value.ValueKind == JsonValueKind.String) parameters[prop.Name] = prop.Value.GetString();
    }
}
expr.EvaluateParameter += (name, args) => {
    if (parameters.TryGetValue(name, out var val)) {
        if (val is decimal d) args.Result = d;
        else if (val is double db) args.Result = (decimal)db;
        else args.Result = val;
    } else {
        args.Result = 0m;
    }
};
expr.EvaluateFunction += (name, args) => {
    if (name == "Contains") {
        var source = args.Parameters[0].Evaluate()?.ToString() ?? "";
        var target = args.Parameters[1].Evaluate()?.ToString() ?? "";
        args.Result = source.Contains(target);
    }
};
Console.WriteLine("Evaluation Result: " + expr.Evaluate());