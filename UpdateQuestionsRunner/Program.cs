using System;
using Npgsql;
using System.Collections.Generic;

class Program {
    static void Main() {
        string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
        using var conn = new NpgsqlConnection(connString);
        conn.Open();
        
        var categories = new Dictionary<Guid, string>();
        using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"Name\" FROM \"ServiceCategories\";", conn))
        using (var reader = cmd.ExecuteReader()) {
            while(reader.Read()) {
                categories.Add(reader.GetGuid(0), reader.GetString(1));
            }
        }
        
        int updated = 0;
        foreach(var cat in categories) {
            string prefix = "UNK";
            string name = cat.Value.ToLower();
            if (name.Contains("demolition") || name.Contains("къртене")) prefix = "DEMO";
            else if (name.Contains("drywall") || name.Contains("сухо")) prefix = "DRYW";
            else if (name.Contains("painting") || name.Contains("боя")) prefix = "PANT";
            else if (name.Contains("tiling") || name.Contains("подови") || name.Contains("настилки")) prefix = "TILE";
            else if (name.Contains("plumbing") || name.Contains("вик")) prefix = "PLMB";
            else if (name.Contains("electrical") || name.Contains("ел.")) prefix = "ELEC";
            
            var skus = new List<Guid>();
            using (var cmd = new NpgsqlCommand("SELECT \"Id\" FROM \"ServiceSkus\" WHERE \"ServiceCategoryId\" = @catId ORDER BY \"CreatedAt\" ASC;", conn)) {
                cmd.Parameters.AddWithValue("catId", cat.Key);
                using var reader = cmd.ExecuteReader();
                while(reader.Read()) skus.Add(reader.GetGuid(0));
            }
            
            int index = 1;
            foreach(var skuId in skus) {
                using (var updateCmd = new NpgsqlCommand("UPDATE \"ServiceSkus\" SET \"SkuCode\" = @code WHERE \"Id\" = @id;", conn)) {
                    updateCmd.Parameters.AddWithValue("code", $"{prefix}-{index:D3}");
                    updateCmd.Parameters.AddWithValue("id", skuId);
                    updateCmd.ExecuteNonQuery();
                }
                index++;
                updated++;
            }
        }
        
        Console.WriteLine($"Successfully updated {updated} SKU codes to English prefixes.");
    }
}