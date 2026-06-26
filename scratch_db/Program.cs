using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var projectId = "872a17e3-fba7-45b5-939d-8038121eedff";
        var endpoint = "https://buildsmart.bg/graphql";
        
        using var client = new HttpClient();
        
        // 1. Login to get token
        Console.WriteLine("Logging in to buildsmart.bg...");
        var loginMutation = @"
        mutation Login($email: String!, $password: String!) {
            login(email: $email, password: $password)
        }";
        
        var loginPayload = new
        {
            query = loginMutation,
            variables = new
            {
                email = "admin@buildsmart.com",
                password = "Admin123!"
            }
        };
        
        var loginJson = JsonSerializer.Serialize(loginPayload);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        var loginResponse = await client.PostAsync(endpoint, loginContent);
        var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
        
        string token = "";
        using (var doc = JsonDocument.Parse(loginResponseString))
        {
            if (doc.RootElement.TryGetProperty("data", out var data) && 
                data.TryGetProperty("login", out var loginToken))
            {
                token = loginToken.GetString() ?? "";
            }
            else
            {
                Console.WriteLine("Login failed. Response:");
                Console.WriteLine(JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
                return;
            }
        }
        
        Console.WriteLine("Login successful! Token acquired.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // 2. Query Project details
        var query = @"
        query {
            projectById(projectId: """ + projectId + @""") {
                id
                title
                status
                jobPosts {
                    id
                    title
                    status
                    adminFeedback
                    updatedAt
                }
            }
        }";
        
        var requestPayload = new
        {
            query = query
        };
        
        var json = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        Console.WriteLine($"Sending authenticated request for Project ID: {projectId}...");
        
        try
        {
            var response = await client.PostAsync(endpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Response Status: {response.StatusCode}");
            Console.WriteLine("Response Body:");
            
            // Format and print JSON
            using var doc = JsonDocument.Parse(responseString);
            Console.WriteLine(JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }
    }
}
