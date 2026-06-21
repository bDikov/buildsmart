#r "nuget: Npgsql, 7.0.4"
using System;
using Npgsql;

string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";

using (var conn = new NpgsqlConnection(connString)) {
    conn.Open();
    
    // Find Project
    Guid projectId = Guid.Empty;
    string projectTitle = "";
    string projectStatus = "";
    string projectSummary = "";
    bool hasPdf = false;
    
    using (var cmd = new NpgsqlCommand(
        "SELECT \"Id\", \"Title\", \"Status\", \"GeneralSummary\", \"MasterOfferPdf\" " +
        "FROM \"Projects\" " +
        "WHERE CAST(\"Id\" AS text) LIKE '04ccc672%';", conn)) {
        using (var reader = cmd.ExecuteReader()) {
            if (reader.Read()) {
                projectId = reader.GetGuid(0);
                projectTitle = reader.GetString(1);
                projectStatus = reader.GetString(2);
                projectSummary = reader.IsDBNull(3) ? "NULL" : reader.GetString(3);
                byte[] pdf = reader.IsDBNull(4) ? null : (byte[])reader.GetValue(4);
                hasPdf = pdf != null && pdf.Length > 0;
                Console.WriteLine($"Project Found: ID={projectId} | Title={projectTitle} | Status={projectStatus} | HasPdf={hasPdf}");
                Console.WriteLine($"Summary: {projectSummary}");
            } else {
                Console.WriteLine("No project starting with 04ccc672 found!");
                return;
            }
        }
    }
    
    // Find JobPosts for this project
    Console.WriteLine("\n=== JOB POSTS ===");
    using (var cmd = new NpgsqlCommand(
        "SELECT \"Id\", \"Title\", \"Status\", \"AdminFeedback\", \"GeneratedScope\" " +
        "FROM \"JobPosts\" " +
        "WHERE \"ProjectId\" = @projId;", conn)) {
        cmd.Parameters.AddWithValue("projId", projectId);
        using (var reader = cmd.ExecuteReader()) {
            while (reader.Read()) {
                Guid jobId = reader.GetGuid(0);
                string title = reader.GetString(1);
                string status = reader.GetString(2);
                string feedback = reader.IsDBNull(3) ? "NULL" : reader.GetString(3);
                string scope = reader.IsDBNull(4) ? "NULL" : reader.GetString(4);
                Console.WriteLine($"Job: ID={jobId} | Title={title} | Status={status}");
                Console.WriteLine($"Feedback: {feedback}");
                Console.WriteLine($"Scope (first 100 chars): {(scope.Length > 100 ? scope.Substring(0, 100) : scope)}");
                Console.WriteLine("--------------------------------------------");
            }
        }
    }

    // Find AI Calculations for this project
    Console.WriteLine("\n=== AI CALCULATIONS ===");
    using (var cmd = new NpgsqlCommand(
        "SELECT \"Id\", \"ServiceCategoryId\", \"TotalEstimatedPrice\" " +
        "FROM \"AiCalculations\" " +
        "WHERE \"ProjectId\" = @projId;", conn)) {
        cmd.Parameters.AddWithValue("projId", projectId);
        using (var reader = cmd.ExecuteReader()) {
            while (reader.Read()) {
                Guid calcId = reader.GetGuid(0);
                Guid catId = reader.GetGuid(1);
                decimal price = reader.GetDecimal(2);
                Console.WriteLine($"Calculation: ID={calcId} | CategoryId={catId} | Price={price}");
            }
        }
    }
}
