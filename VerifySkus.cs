        using System;
using Npgsql;

class Program {
    static void Main() {
        string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
        using var conn = new NpgsqlConnection(connString);
        conn.Open();

        string sql = "SELECT \"SkuCode\", \"CalculationFormula\" FROM \"ServiceSkus\" WHERE \"SkuCode\" IN ('TILE-STD', 'TILE-LARGE', 'TILE-LAMINATE');";
        using (var cmd = new NpgsqlCommand(sql, conn))
        using (var reader = cmd.ExecuteReader()) {
            Console.WriteLine("--- Local Database SKU Formulas ---");
            while (reader.Read()) {
                Console.WriteLine($"{reader.GetString(0)}: {reader.GetString(1)}");
            }
        }
    }
}