using BuildSmart.Maui.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BuildSmart.Maui
{
    // This handler intercepts every HTTP request and adds the JWT token to the header.
    // This is the correct, deadlock-safe way to handle async token retrieval.
    public class AuthenticationHandler : DelegatingHandler
    {
        private readonly IAuthService _authService;

        public AuthenticationHandler(IAuthService authService)
        {
            _authService = authService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
