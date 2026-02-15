using System.Diagnostics;
using System.Net.Http;

namespace BuildSmart.Maui.Handlers;

public class LoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString().Substring(0, 8);
        Console.WriteLine($"[HTTP Request {requestId}] {request.Method} {request.RequestUri}");

        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            Console.WriteLine($"[HTTP Request {requestId} Content] {content}");
        }

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            Console.WriteLine($"[HTTP Response {requestId}] Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[HTTP Response {requestId} Error] {errorContent}");
            }

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HTTP Request {requestId} Exception] {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
}
