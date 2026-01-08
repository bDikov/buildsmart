using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BuildSmart.Maui.Services
{
    public class LoggingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Debug.WriteLine("--- HTTP Request ---");
            Debug.WriteLine($"Request: {request.Method} {request.RequestUri}");
            
            if (request.Content != null)
            {
                var content = await request.Content.ReadAsStringAsync(cancellationToken);
                Debug.WriteLine($"Content: {content}");
            }
            Debug.WriteLine("--- End Request ---");

            var response = await base.SendAsync(request, cancellationToken);

            Debug.WriteLine("--- HTTP Response ---");
            Debug.WriteLine($"Status Code: {(int)response.StatusCode} {response.ReasonPhrase}");
            
            if (response.Content != null)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                Debug.WriteLine($"Content: {content}");
            }
            Debug.WriteLine("--- End Response ---");

            return response;
        }
    }
}