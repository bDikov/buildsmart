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
            SELECT 
                jp.""Title"",
                c.""Name"",
                jp.""JobDetails""
            FROM ""JobPosts"" jp
            JOIN ""ServiceCategories"" c ON jp.""ServiceCategoryId"" = c.""Id""
            WHERE jp.""ProjectId"" IN (SELECT ""Id"" FROM ""Projects"" WHERE ""Id""::text LIKE 'dd7e3002%')
            ORDER BY c.""Name"";
        ";
        
        using var cmd = new NpgsqlCommand(query, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var title = reader.GetString(0);
            var category = reader.GetString(1);
            var details = reader.GetString(2);
            Console.WriteLine($"=== Category: {category} (Title: {title}) ===");
            Console.WriteLine(details);
            Console.WriteLine();
        }
    }
}