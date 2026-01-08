using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.TestHost; // Needed for IOptions
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildSmart.Api.Tests
{
	    public class TestApplicationFactory : WebApplicationFactory<Program>
	    {
	        private static readonly object _lock = new object();
	        private static bool _authSchemeRegistered;
	
	                protected override void ConfigureWebHost(IWebHostBuilder builder)
	                {
	                    builder.ConfigureTestServices(services =>
	                    {
	                        // Ensure the authentication scheme is only registered once.
	                        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthenticationService));
	                        if (descriptor == null)
	                        {
	                            services.AddAuthentication(TestAuthHandler.SchemeName)
	                                .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options => { });
	                        }
	                    });
	                }	    }}