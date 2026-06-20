#r "nuget: Npgsql, 7.0.4"
using System;
using Npgsql;

string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";

using (var conn = new NpgsqlConnection(connString)) {
    conn.Open();

    // Get latest project
    Guid projectId = Guid.Empty;
    string projectTitle = "";
    using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"Title\" FROM \"Projects\" ORDER BY \"CreatedAt\" DESC LIMIT 1;", conn))
    using (var reader = cmd.ExecuteReader()) {
        if (reader.Read()) {
            projectId = reader.GetGuid(0);
            projectTitle = reader.GetString(1);
            Console.WriteLine($"PROJECT: {projectTitle} (Id: {projectId})");
        } else {
            Console.WriteLine("No projects found.");
            Environment.Exit(0);
        }
    }

    // Get AiCalculations, Tasks, and SkuItems
    using (var cmd = new NpgsqlCommand(
        "SELECT c.\"Name\" as CatName, t.\"Title\" as TaskTitle, t.\"EstimatedPrice\" as TaskPrice, " +
        "s.\"SkuCode\", s.\"Name\" as SkuName, s.\"BasePrice\", s.\"CalculationFormula\", " +
        "si.\"Quantity\", si.\"EstimatedPrice\" as SkuLinePrice " +
        "FROM \"AiCalculations\" ac " +
        "JOIN \"ServiceCategories\" c ON ac.\"ServiceCategoryId\" = c.\"Id\" " +
        "JOIN \"AiCalculationTasks\" t ON t.\"AiCalculationId\" = ac.\"Id\" " +
        "LEFT JOIN \"AiCalculationSkuItems\" si ON si.\"AiCalculationTaskId\" = t.\"Id\" " +
        "LEFT JOIN \"ServiceSkus\" s ON si.\"ServiceSkuId\" = s.\"Id\" " +
        "WHERE ac.\"ProjectId\" = @projectId " +
        "ORDER BY c.\"Name\", t.\"Title\", s.\"SkuCode\";", conn)) {
        cmd.Parameters.AddWithValue("projectId", projectId);
        using (var reader = cmd.ExecuteReader()) {
            string currentCat = "";
            string currentTask = "";
            while (reader.Read()) {
                string catName = reader.GetString(0);
                string taskTitle = reader.GetString(1);
                decimal taskPrice = reader.GetDecimal(2);
                string skuCode = reader.IsDBNull(3) ? "N/A" : reader.GetString(3);
                string skuName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                decimal basePrice = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5);
                string formula = reader.IsDBNull(6) ? "" : reader.GetString(6);
                decimal qty = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7);
                decimal skuLinePrice = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8);

                if (catName != currentCat) {
                    currentCat = catName;
                    Console.WriteLine($"\n==========================================");
                    Console.WriteLine($"CATEGORY: {currentCat.ToUpper()}");
                    Console.WriteLine($"==========================================");
                }

                if (taskTitle != currentTask) {
                    currentTask = taskTitle;
                    Console.WriteLine($"\n* Task: \"{currentTask}\" (Estimated Price: €{taskPrice:F2})");
                }

                if (skuCode != "N/A") {
                    Console.WriteLine($"  - SKU: {skuCode} | Name: {skuName}");
                    Console.WriteLine($"    Formula: {formula}");
                    Console.WriteLine($"    Quantity: {qty} * Base Price: €{basePrice:F2} = Line Cost: €{skuLinePrice:F2}");
                }
            }
        }
    }
}
