using System;
using System.IO;
using System.Text.Json.Nodes;
using Npgsql;

string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
using var conn = new NpgsqlConnection(connString);
try 
{
    conn.Open();
    Console.WriteLine("Connected to DB!");

    // 1. Run the SQL script for Questions
    string sql = File.ReadAllText("../UpdateQuestions.sql");
    using var cmd = new NpgsqlCommand(sql, conn);
    int rowsAffected = cmd.ExecuteNonQuery();
    Console.WriteLine($"Successfully executed SQL for Questions. Rows affected: {rowsAffected}");

    // 2. Parse and Insert SKUs
    Guid elecCategoryId = Guid.Parse("e69f9926-d576-4515-a438-e80a850af656");
    
    // Clean old SKUs just in case
    using var cmdClean = new NpgsqlCommand("DELETE FROM \"ServiceSkus\" WHERE \"ServiceCategoryId\" = @id AND \"SkuCode\" LIKE 'ELEC-%';", conn);
    cmdClean.Parameters.AddWithValue("id", elecCategoryId);
    int cleaned = cmdClean.ExecuteNonQuery();
    Console.WriteLine($"Cleaned {cleaned} old test SKUs.");

    string skusJson = File.ReadAllText("../Electrical_SKUs_Seed.json");
    var skusDoc = JsonNode.Parse(skusJson);
    var skus = skusDoc["skus"].AsArray();

    int skuCount = 0;
    foreach (var sku in skus)
    {
        string rawUnit = sku["unitType"]?.ToString() ?? "";
        string mappedUnit = rawUnit switch {
            "m" => "Per Linear Meter",
            "pcs" => "Per Quantity (Item)",
            "module" => "Per Quantity (Item)",
            _ => "Per Quantity (Item)"
        };

        using var cmdSku = new NpgsqlCommand(
            "INSERT INTO \"ServiceSkus\" (\"Id\", \"CreatedAt\", \"UpdatedAt\", \"ServiceCategoryId\", \"SkuCode\", \"Name\", \"Description\", \"BasePrice\", \"UnitType\") " +
            "VALUES (@id, @created, @updated, @catId, @code, @name, @desc, @price, @unitType);", conn);

        cmdSku.Parameters.AddWithValue("id", Guid.NewGuid());
        cmdSku.Parameters.AddWithValue("created", DateTime.UtcNow);
        cmdSku.Parameters.AddWithValue("updated", DateTime.UtcNow);
        cmdSku.Parameters.AddWithValue("catId", elecCategoryId);
        cmdSku.Parameters.AddWithValue("code", sku["skuCode"]?.ToString());
        cmdSku.Parameters.AddWithValue("name", sku["name"]?.ToString());
        cmdSku.Parameters.AddWithValue("desc", sku["description"]?.ToString());
        cmdSku.Parameters.AddWithValue("price", decimal.Parse(sku["basePrice"]?.ToString() ?? "0"));
        cmdSku.Parameters.AddWithValue("unitType", mappedUnit);

        skuCount += cmdSku.ExecuteNonQuery();
    }
    
    Console.WriteLine($"Successfully inserted {skuCount} detailed Bulgarian SKUs into the Electrical category!");
    Console.WriteLine("All seeding operations complete!");
}
catch (Exception ex)
{
    Console.WriteLine("Database Error: " + ex.Message);
}
