#r "nuget: Npgsql, 7.0.4"
using System;
using Npgsql;

string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
using (var conn = new NpgsqlConnection(connString)) {
    conn.Open();

    string sql = "SELECT \"SkuCode\", \"Name\", \"CalculationFormula\" FROM \"ServiceSkus\" WHERE \"SkuCode\" IN ('ELEC-084', 'ELEC-085', 'ELEC-020', 'ELEC-002', 'ELEC-001', 'ELEC-009', 'ELEC-010', 'ELEC-007', 'ELEC-037', 'ELEC-LED-TRAFO') ORDER BY \"SkuCode\";";
    using (var cmd = new NpgsqlCommand(sql, conn))
    using (var reader = cmd.ExecuteReader()) {
        Console.WriteLine("--- Database Verification ---");
        while (reader.Read()) {
            string formula = reader.IsDBNull(2) ? "" : reader.GetString(2);
            Console.WriteLine($"{reader.GetString(0)} | {reader.GetString(1)} | Formula: {formula}");
        }
    }
}