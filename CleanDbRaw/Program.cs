using System;
using Npgsql;

class Program
{
    static void Main()
    {
        string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
        using var conn = new NpgsqlConnection(connString);
        try 
        {
            conn.Open();
            
            // Delete related records first
            using var cmd0 = new NpgsqlCommand("DELETE FROM \"TaskSkuItems\" WHERE \"JobTaskId\" IN (SELECT \"Id\" FROM \"JobTasks\" WHERE \"JobPostId\" IN (SELECT \"Id\" FROM \"JobPosts\" WHERE \"Title\" LIKE 'Add outlets%' OR \"Title\" LIKE 'Wire new basement%' OR \"Title\" LIKE 'Rewire Living Room%'));", conn);
            cmd0.ExecuteNonQuery();

            using var cmd1 = new NpgsqlCommand("DELETE FROM \"JobTasks\" WHERE \"JobPostId\" IN (SELECT \"Id\" FROM \"JobPosts\" WHERE \"Title\" LIKE 'Add outlets%' OR \"Title\" LIKE 'Wire new basement%' OR \"Title\" LIKE 'Rewire Living Room%');", conn);
            cmd1.ExecuteNonQuery();

            using var cmd2 = new NpgsqlCommand("DELETE FROM \"JobPosts\" WHERE \"Title\" LIKE 'Add outlets%' OR \"Title\" LIKE 'Wire new basement%' OR \"Title\" LIKE 'Rewire Living Room%' OR \"Title\" LIKE 'Test Job%';", conn);
            int jp = cmd2.ExecuteNonQuery();
            Console.WriteLine($"Deleted {jp} test Job Posts.");
            
            using var cmd3 = new NpgsqlCommand("DELETE FROM \"Projects\" WHERE \"Title\" IN ('AI Test Project', 'Basement Office Conversion', 'Living Room Tech Upgrade', 'Test');", conn);
            int proj = cmd3.ExecuteNonQuery();
            Console.WriteLine($"Deleted {proj} test projects.");
            
            using var cmd4 = new NpgsqlCommand("DELETE FROM \"ServiceSkus\" WHERE \"ServiceCategoryId\" IN (SELECT \"Id\" FROM \"ServiceCategories\" WHERE \"Name\" LIKE 'Electrical Test%' OR \"Name\" LIKE 'Electrical Room%' OR \"Name\" LIKE 'Complex Electrical%');", conn);
            cmd4.ExecuteNonQuery();

            using var cmd5 = new NpgsqlCommand("DELETE FROM \"ServiceCategories\" WHERE \"Name\" LIKE 'Electrical Test%' OR \"Name\" LIKE 'Electrical Room%' OR \"Name\" LIKE 'Complex Electrical%';", conn);
            int sc = cmd5.ExecuteNonQuery();
            Console.WriteLine($"Deleted {sc} test categories.");
            
            using var cmd6 = new NpgsqlCommand("DELETE FROM \"HomeownerProfiles\" WHERE \"UserId\" IN (SELECT \"Id\" FROM \"Users\" WHERE \"Email\" LIKE 'test-ai-%' OR \"Email\" LIKE 'test-restore%');", conn);
            cmd6.ExecuteNonQuery();

            using var cmd7 = new NpgsqlCommand("DELETE FROM \"Users\" WHERE \"Email\" LIKE 'test-ai-%' OR \"Email\" LIKE 'test-restore%';", conn);
            int u = cmd7.ExecuteNonQuery();
            Console.WriteLine($"Deleted {u} test users.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not connect to database: " + ex.Message);
        }
    }
}