using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:44378/graphql/");
        
        var query = "{\"query\":\"query { serviceCategories { id name englishName } }\"}";
        request.Content = new StringContent(query, Encoding.UTF8, "application/json");

        // Bypass SSL certificate check for local dev
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var clientWithBypass = new HttpClient(handler);
        
        var response = await clientWithBypass.PostAsync("https://localhost:44378/graphql/", request.Content);
        var content = await response.Content.ReadAsStringAsync();
        
        Console.WriteLine("Raw GraphQL Response:");
        Console.WriteLine(content);
    }
}
