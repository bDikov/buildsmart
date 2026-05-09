using System;
using Npgsql;

class Program {
    static void Main() {
        string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
        using var conn = new NpgsqlConnection(connString);
        conn.Open();

        using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"Title\", LENGTH(\"MasterOfferPdf\") FROM \"Projects\" WHERE \"Id\" = '7259347f-a5eb-4c50-bcd1-aae754748222';", conn))
        using (var reader = cmd.ExecuteReader()) {
            Console.WriteLine("Project Check:");
            while(reader.Read()) {
                long pdfLength = reader.IsDBNull(2) ? 0 : reader.GetInt64(2);
                Console.WriteLine($"{reader.GetGuid(0)} | {reader.GetString(1)} | PDF Size: {pdfLength} bytes");
            }
        }
    }
}