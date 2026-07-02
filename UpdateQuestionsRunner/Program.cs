using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Npgsql;

class Program {
    static void Main() {
        string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
        string jsonPath = @"C:\Users\bonch\source\repos\local.json";
        
        if (!File.Exists(jsonPath)) {
            // Try relative path from bin/Debug/...
            jsonPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../local.json"));
        }

        if (!File.Exists(jsonPath)) {
            Console.WriteLine($"Error: local.json not found at {jsonPath}");
            return;
        }

        Console.WriteLine($"Reading data from: {jsonPath}");
        var jsonContent = File.ReadAllText(jsonPath);
        var data = JsonSerializer.Deserialize<LocalJsonData>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data == null || data.Categories == null || data.Skus == null) {
            Console.WriteLine("Error: Failed to deserialize local.json");
            return;
        }

        using var conn = new NpgsqlConnection(connString);
        conn.Open();
        using var trans = conn.BeginTransaction();

        try {
            Console.WriteLine($"Found {data.Categories.Count} categories and {data.Skus.Count} SKUs to import.");

            // 1. Fetch existing categories by Name to match their Guid
            var dbCategories = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new NpgsqlCommand(@"SELECT ""Name"", ""Id"" FROM ""ServiceCategories"";", conn))
            using (var reader = cmd.ExecuteReader()) {
                while (reader.Read()) {
                    dbCategories[reader.GetString(0)] = reader.GetGuid(1);
                }
            }

            // Import Categories
            foreach (var cat in data.Categories) {
                if (dbCategories.TryGetValue(cat.Name, out var dbCatId)) {
                    using var cmd = new NpgsqlCommand(@"
                        UPDATE ""ServiceCategories""
                        SET ""Status"" = @status,
                            ""IsGlobal"" = @isGlobal,
                            ""TemplateStructure"" = @template::jsonb,
                            ""UpdatedAt"" = now()
                        WHERE ""Id"" = @id;", conn);
                    
                    int statusVal = string.Equals(cat.Status, "Active", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    cmd.Parameters.AddWithValue("id", dbCatId);
                    cmd.Parameters.AddWithValue("status", statusVal);
                    cmd.Parameters.AddWithValue("isGlobal", cat.IsGlobal);
                    cmd.Parameters.AddWithValue("template", cat.TemplateStructure);
                    cmd.ExecuteNonQuery();
                } else {
                    using var cmd = new NpgsqlCommand(@"
                        INSERT INTO ""ServiceCategories"" (""Id"", ""Name"", ""Status"", ""IsGlobal"", ""TemplateStructure"", ""CreatedAt"", ""UpdatedAt"")
                        VALUES (@id, @name, @status, @isGlobal, @template::jsonb, now(), now());", conn);
                    
                    int statusVal = string.Equals(cat.Status, "Active", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    cmd.Parameters.AddWithValue("id", cat.Id);
                    cmd.Parameters.AddWithValue("name", cat.Name);
                    cmd.Parameters.AddWithValue("status", statusVal);
                    cmd.Parameters.AddWithValue("isGlobal", cat.IsGlobal);
                    cmd.Parameters.AddWithValue("template", cat.TemplateStructure);
                    cmd.ExecuteNonQuery();
                    dbCategories[cat.Name] = cat.Id;
                }
            }
            Console.WriteLine("Successfully synced all categories.");

            // 2. Fetch existing SKUs by SkuCode to match their Guid
            var dbSkus = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new NpgsqlCommand(@"SELECT ""SkuCode"", ""Id"" FROM ""ServiceSkus"";", conn))
            using (var reader = cmd.ExecuteReader()) {
                while (reader.Read()) {
                    dbSkus[reader.GetString(0)] = reader.GetGuid(1);
                }
            }

            // Import SKUs
            foreach (var sku in data.Skus) {
                // Find correct category ID in case it changed
                string? targetCatName = null;
                foreach (var cat in data.Categories) {
                    if (cat.Id == sku.ServiceCategoryId) {
                        targetCatName = cat.Name;
                        break;
                    }
                }
                
                Guid categoryId = sku.ServiceCategoryId;
                if (targetCatName != null && dbCategories.TryGetValue(targetCatName, out var matchedCatId)) {
                    categoryId = matchedCatId;
                }

                if (dbSkus.TryGetValue(sku.SkuCode, out var dbSkuId)) {
                    using var cmd = new NpgsqlCommand(@"
                        UPDATE ""ServiceSkus""
                        SET ""ServiceCategoryId"" = @catId,
                            ""Name"" = @name,
                            ""Description"" = @desc,
                            ""BasePrice"" = @price,
                            ""UnitType"" = @unit,
                            ""CalculationFormula"" = @formula,
                            ""UpdatedAt"" = now()
                        WHERE ""Id"" = @id;", conn);

                    cmd.Parameters.AddWithValue("id", dbSkuId);
                    cmd.Parameters.AddWithValue("catId", categoryId);
                    cmd.Parameters.AddWithValue("name", sku.Name);
                    cmd.Parameters.AddWithValue("desc", sku.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("price", sku.BasePrice);
                    cmd.Parameters.AddWithValue("unit", sku.UnitType);
                    cmd.Parameters.AddWithValue("formula", sku.CalculationFormula ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                } else {
                    using var cmd = new NpgsqlCommand(@"
                        INSERT INTO ""ServiceSkus"" (""Id"", ""ServiceCategoryId"", ""SkuCode"", ""Name"", ""Description"", ""BasePrice"", ""UnitType"", ""CalculationFormula"", ""CreatedAt"", ""UpdatedAt"")
                        VALUES (@id, @catId, @code, @name, @desc, @price, @unit, @formula, now(), now());", conn);

                    cmd.Parameters.AddWithValue("id", sku.Id);
                    cmd.Parameters.AddWithValue("catId", categoryId);
                    cmd.Parameters.AddWithValue("code", sku.SkuCode);
                    cmd.Parameters.AddWithValue("name", sku.Name);
                    cmd.Parameters.AddWithValue("desc", sku.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("price", sku.BasePrice);
                    cmd.Parameters.AddWithValue("unit", sku.UnitType);
                    cmd.Parameters.AddWithValue("formula", sku.CalculationFormula ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            Console.WriteLine("Successfully synced all SKUs.");

            trans.Commit();
            Console.WriteLine("Database import completed successfully!");
        } catch (Exception ex) {
            trans.Rollback();
            Console.WriteLine($"Error during import: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    public class LocalJsonData {
        public List<CategoryDto> Categories { get; set; } = new();
        public List<SkuDto> Skus { get; set; } = new();
    }

    public class CategoryDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsGlobal { get; set; }
        public string TemplateStructure { get; set; } = string.Empty;
    }

    public class SkuDto {
        public Guid Id { get; set; }
        public string SkuCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string UnitType { get; set; } = string.Empty;
        public Guid ServiceCategoryId { get; set; }
        public string CalculationFormula { get; set; } = string.Empty;
    }
}