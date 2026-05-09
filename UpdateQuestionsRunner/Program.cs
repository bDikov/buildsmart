using System;
using System.IO;
using System.Text.Json;
using Npgsql;
using System.Collections.Generic;

class Program {
    static void Main() {
        string jsonPath = @"C:\Users\bonch\source\repos\BuildSmart\Categories_Seed_Templates.json";
        string jsonContent = File.ReadAllText(jsonPath);
        
        using var doc = JsonDocument.Parse(jsonContent);
        
        string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
        using var conn = new NpgsqlConnection(connString);
        conn.Open();

        int updated = 0;
        foreach (var category in doc.RootElement.EnumerateObject()) {
            string catName = category.Value.GetProperty("name").GetString()!;
            string templateStructure = category.Value.GetProperty("templateStructure").GetRawText();
            
            using var cmd = new NpgsqlCommand("UPDATE \"ServiceCategories\" SET \"TemplateStructure\" = @template::jsonb WHERE \"Name\" = @name OR \"Name\" ILIKE '%' || @name || '%';", conn);
            cmd.Parameters.AddWithValue("template", templateStructure);
            cmd.Parameters.AddWithValue("name", catName);
            int affected = cmd.ExecuteNonQuery();
            Console.WriteLine($"Updated {affected} rows for category '{catName}'.");
            updated += affected;
        }
        Console.WriteLine($"Total categories updated: {updated}");
    }
}