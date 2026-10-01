using System;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        var connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        
        var query = @"
            SELECT ""Id"", ""Name"", ""Phone"", length(""AdminNotes""), ""AdminNotes""
            FROM ""CalculatorLeads""
            WHERE ""Name"" ILIKE '%Pesho%' OR ""Phone"" ILIKE '%0899%'
            ORDER BY ""CreatedAt"" DESC;
        ";
        
        using var cmd = new NpgsqlCommand(query, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetGuid(0);
            var name = reader.GetString(1);
            var phone = reader.GetString(2);
            var len = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            var notes = reader.IsDBNull(4) ? "" : reader.GetString(4);
            Console.WriteLine($"=== Lead {id}: {name} ({phone}) - Length: {len} ===");
            Console.WriteLine(notes);
            Console.WriteLine("==================================================");
        }
    }
}