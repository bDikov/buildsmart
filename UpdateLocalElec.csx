#r "nuget: Npgsql, 7.0.4"
using System;
using System.IO;
using System.Text.Json;
using Npgsql;

string jsonPath = @"Categories_Seed_Templates.json";
string jsonContent = File.ReadAllText(jsonPath);
using (var doc = JsonDocument.Parse(jsonContent)) {
    string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
    using (var conn = new NpgsqlConnection(connString)) {
        conn.Open();

        int updated = 0;
        foreach (var category in doc.RootElement.EnumerateObject()) {
            string catName = category.Value.GetProperty("name").GetString();
            if (catName != "Електрическа Инсталация") continue;

            string templateStructure = category.Value.GetProperty("templateStructure").GetRawText();

            using (var cmd = new NpgsqlCommand("UPDATE \"ServiceCategories\" SET \"TemplateStructure\" = @template::jsonb WHERE \"Name\" = @name OR \"Name\" ILIKE '%' || @name || '%';", conn)) {
                cmd.Parameters.AddWithValue("template", templateStructure);
                cmd.Parameters.AddWithValue("name", catName);
                int affected = cmd.ExecuteNonQuery();
                updated += affected;
            }
        }
        Console.WriteLine($"Successfully updated {updated} Category templates locally.");

        var queries = new[] {
            "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'if(Contains(global_wall_material, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)' WHERE \"SkuCode\" = 'ELEC-CHASE-CONC';",
            "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'if(Contains(global_wall_material, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)' WHERE \"SkuCode\" = 'ELEC-CHASE-BRICK';",
            "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'elec_lv_count' WHERE \"SkuCode\" = 'ELEC-POINT-LV';",
            "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'elec_dev_count' WHERE \"SkuCode\" = 'ELEC-POINT-DEV';",
            "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'elec_spec_count' WHERE \"SkuCode\" = 'ELEC-POINT-SPEC';"
        };

        int skuRows = 0;
        foreach(var q in queries) {
            using (var cmd = new NpgsqlCommand(q, conn)) {
                skuRows += cmd.ExecuteNonQuery();
            }
        }
        Console.WriteLine($"Successfully updated {skuRows} SKU formulas locally.");
    }
}