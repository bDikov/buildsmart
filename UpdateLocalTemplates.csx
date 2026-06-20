#r "nuget: Npgsql, 7.0.4"
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Npgsql;

string jsonPath = @"Categories_Seed_Templates.json";
if (!File.Exists(jsonPath)) {
    Console.WriteLine($"Error: {jsonPath} not found!");
    Environment.Exit(1);
}

string jsonContent = File.ReadAllText(jsonPath);
using (var doc = JsonDocument.Parse(jsonContent)) {
    string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
    using (var conn = new NpgsqlConnection(connString)) {
        conn.Open();

        int updated = 0;
        foreach (var category in doc.RootElement.EnumerateObject()) {
            string catKey = category.Name;
            string catName = category.Value.GetProperty("name").GetString();
            bool isGlobal = catKey == "global_category";
            string templateStructure = category.Value.GetProperty("templateStructure").GetRawText();

            // Check if category exists
            Guid catId = Guid.Empty;
            using (var cmd = new NpgsqlCommand("SELECT \"Id\" FROM \"ServiceCategories\" WHERE \"Name\" = @name;", conn)) {
                cmd.Parameters.AddWithValue("name", catName);
                var val = cmd.ExecuteScalar();
                if (val != null) catId = (Guid)val;
            }

            if (catId == Guid.Empty) {
                // Insert new category
                catId = Guid.NewGuid();
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO \"ServiceCategories\" (\"Id\", \"Name\", \"Status\", \"IsGlobal\", \"TemplateStructure\", \"CreatedAt\", \"UpdatedAt\") " +
                    "VALUES (@id, @name, 1, @isGlobal, @template::jsonb, now(), now());", conn)) {
                    cmd.Parameters.AddWithValue("id", catId);
                    cmd.Parameters.AddWithValue("name", catName);
                    cmd.Parameters.AddWithValue("isGlobal", isGlobal);
                    cmd.Parameters.AddWithValue("template", templateStructure);
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine($"Inserted category: '{catName}'");
            } else {
                // Update existing category
                using (var cmd = new NpgsqlCommand(
                    "UPDATE \"ServiceCategories\" SET \"TemplateStructure\" = @template::jsonb, \"IsGlobal\" = @isGlobal, \"UpdatedAt\" = now() WHERE \"Id\" = @id;", conn)) {
                    cmd.Parameters.AddWithValue("template", templateStructure);
                    cmd.Parameters.AddWithValue("isGlobal", isGlobal);
                    cmd.Parameters.AddWithValue("id", catId);
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine($"Updated category: '{catName}'");
            }
            updated++;
        }
        Console.WriteLine($"\nSuccessfully processed {updated} categories in the local database.");
    }
}
