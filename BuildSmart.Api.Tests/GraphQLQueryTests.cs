using Xunit;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Microsoft.Extensions.DependencyInjection;
using BuildSmart.Core.Application.Interfaces;
using Moq;
using BuildSmart.Core.Domain.Entities;
using System.Linq;
using System.Collections.Generic;
using BuildSmart.Api; // Needed for Program class
using Microsoft.AspNetCore.TestHost; // Needed for TestServer
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authentication; // Added
using Microsoft.Extensions.Options; // Added
using System.IdentityModel.Tokens.Jwt; // Added for JwtSecurityTokenHandler

namespace BuildSmart.Api.Tests;

public class GraphQLQueryTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;
    private readonly IConfiguration _configuration; // To access JWT settings

    public GraphQLQueryTests(TestApplicationFactory factory)
    {
        _factory = factory;
        // Build configuration for JWT settings from in-memory collection
        var inMemorySettings = new Dictionary<string, string> {
            {"Jwt:Issuer", "test-issuer"},
            {"Jwt:Audience", "test-audience"},
            {"Jwt:Key", "supersecretkeythatisatleast32characterslong"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private HttpClient CreateClient(Action<IServiceCollection>? configureServices = null, string? jwtToken = null, bool useBasicAuth = false)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Basic setup for ITradesmanProfileRepository and IUserRepository
                // These mocks are reset for each test or configured as needed per test
                services.RemoveAll(typeof(ITradesmanProfileRepository));
                services.AddSingleton(new Mock<ITradesmanProfileRepository>().Object);
                services.RemoveAll(typeof(IUserRepository));
                services.AddSingleton(new Mock<IUserRepository>().Object);

                // Always use our test configuration for JWT settings
                services.RemoveAll(typeof(IConfiguration));
                services.AddSingleton(_configuration);

                configureServices?.Invoke(services);
            });
        }).CreateClient();

        if (useBasicAuth)
        {
            var byteArray = Encoding.ASCII.GetBytes("basicauth:basicauth");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
        else if (jwtToken != null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        }

        return client;
    }

    [Fact]
    public async Task GetTradesmanProfiles_ReturnsAllProfiles_WithHomeownerRole()
    {
        // Arrange
        var homeownerId = Guid.NewGuid();
        var homeownerToken = TestTokenHelper.GenerateJwtToken(homeownerId, "homeowner@example.com", "Homeowner", _configuration);

        var mockTradesmanProfileRepository = new Mock<ITradesmanProfileRepository>();
        mockTradesmanProfileRepository.Setup(repo => repo.GetQueryable())
            .Returns(new List<TradesmanProfile>
            {
                new TradesmanProfile 
                {
                    Id = Guid.NewGuid(),
                    User = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" },
                    Skills = new List<Core.Domain.Entities.JoinEntities.TradesmanSkill>
                    {
                        new Core.Domain.Entities.JoinEntities.TradesmanSkill { ServiceCategory = new ServiceCategory { Id = Guid.NewGuid(), Name = "Plumber" } }
                    }
                },
                new TradesmanProfile 
                {
                    Id = Guid.NewGuid(),
                    User = new User { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith" },
                    Skills = new List<Core.Domain.Entities.JoinEntities.TradesmanSkill>
                    {
                        new Core.Domain.Entities.JoinEntities.TradesmanSkill { ServiceCategory = new ServiceCategory { Id = Guid.NewGuid(), Name = "Electrician" } }
                    }
                }
            }.AsQueryable());

        var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(ITradesmanProfileRepository));
            services.AddSingleton(mockTradesmanProfileRepository.Object);
        }, homeownerToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(
                "{ \"query\": \"{ tradesmanProfiles { user { firstName lastName } skills { serviceCategory { name } } } }\" }",
                System.Text.Encoding.UTF8,
                "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        Snapshot.Match(content, m => m.IgnoreField("data.tradesmanProfiles[*].id")
                                         .IgnoreField("data.tradesmanProfiles[*].user.id")
                                         .IgnoreField("data.tradesmanProfiles[*].skills[*].serviceCategory.id"));
    }

    [Fact]
    public async Task GetTradesmanProfiles_ReturnsAllProfiles_WithTradesmanRole()
    {
        // Arrange
        var tradesmanId = Guid.NewGuid();
        var tradesmanToken = TestTokenHelper.GenerateJwtToken(tradesmanId, "tradesman@example.com", "Tradesman", _configuration);

        var mockTradesmanProfileRepository = new Mock<ITradesmanProfileRepository>();
        mockTradesmanProfileRepository.Setup(repo => repo.GetQueryable())
            .Returns(new List<TradesmanProfile>
            {
                new TradesmanProfile 
                {
                    Id = Guid.NewGuid(),
                    User = new User { Id = Guid.NewGuid(), FirstName = "Kyle", LastName = "Brown" },
                    Skills = new List<Core.Domain.Entities.JoinEntities.TradesmanSkill>
                    {
                        new Core.Domain.Entities.JoinEntities.TradesmanSkill { ServiceCategory = new ServiceCategory { Id = Guid.NewGuid(), Name = "Plumber" } }
                    }
                },
                new TradesmanProfile 
                {
                    Id = Guid.NewGuid(),
                    User = new User { Id = Guid.NewGuid(), FirstName = "Laura", LastName = "Green" },
                    Skills = new List<Core.Domain.Entities.JoinEntities.TradesmanSkill>
                    {
                        new Core.Domain.Entities.JoinEntities.TradesmanSkill { ServiceCategory = new ServiceCategory { Id = Guid.NewGuid(), Name = "Electrician" } }
                    }
                }
            }.AsQueryable());

        var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(ITradesmanProfileRepository));
            services.AddSingleton(mockTradesmanProfileRepository.Object);
        }, tradesmanToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(
                "{ \"query\": \"{ tradesmanProfiles { user { firstName lastName } skills { serviceCategory { name } } } }\" }",
                System.Text.Encoding.UTF8,
                "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        Snapshot.Match(content, m => m.IgnoreField("data.tradesmanProfiles[*].id")
                                         .IgnoreField("data.tradesmanProfiles[*].user.id")
                                         .IgnoreField("data.tradesmanProfiles[*].skills[*].serviceCategory.id"));
    }

    [Fact]
    public async Task GetTradesmanProfiles_ReturnsAllProfiles_WithAdminRole()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var adminToken = TestTokenHelper.GenerateJwtToken(adminId, "admin@example.com", "Admin", _configuration);

        var mockTradesmanProfileRepository = new Mock<ITradesmanProfileRepository>();
        mockTradesmanProfileRepository.Setup(repo => repo.GetQueryable())
            .Returns(new List<TradesmanProfile>
            {
                new TradesmanProfile 
                {
                    Id = Guid.NewGuid(),
                    User = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" },
                    Skills = new List<Core.Domain.Entities.JoinEntities.TradesmanSkill>
                    {
                        new Core.Domain.Entities.JoinEntities.TradesmanSkill { ServiceCategory = new ServiceCategory { Id = Guid.NewGuid(), Name = "Plumber" } }
                    }
                },
                new TradesmanProfile 
                {
                    Id = Guid.NewGuid(),
                    User = new User { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith" },
                    Skills = new List<Core.Domain.Entities.JoinEntities.TradesmanSkill>
                    {
                        new Core.Domain.Entities.JoinEntities.TradesmanSkill { ServiceCategory = new ServiceCategory { Id = Guid.NewGuid(), Name = "Electrician" } }
                    }
                }
            }.AsQueryable());

        var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(ITradesmanProfileRepository));
            services.AddSingleton(mockTradesmanProfileRepository.Object);
        }, adminToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(
                "{ \"query\": \"{ tradesmanProfiles { user { firstName lastName } skills { serviceCategory { name } } } }\" }",
                System.Text.Encoding.UTF8,
                "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        Snapshot.Match(content, m => m.IgnoreField("data.tradesmanProfiles[*].id")
                                         .IgnoreField("data.tradesmanProfiles[*].user.id")
                                         .IgnoreField("data.tradesmanProfiles[*].skills[*].serviceCategory.id"));
    }

    [Fact]
    public async Task GetTradesmanProfiles_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = CreateClient(); // No JWT token

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(
                "{ \"query\": \"{ tradesmanProfiles { user { firstName lastName } skills { serviceCategory { name } } } }\" }",
                System.Text.Encoding.UTF8,
                "application/json")
        };

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var jsonResponse = JObject.Parse(content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(jsonResponse["errors"]);
        Assert.Contains("not authorized", jsonResponse["errors"]![0]!["message"]!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsAuthenticatedUser_WithHomeownerRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testUser = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = BuildSmart.Core.Domain.Enums.UserRoleTypes.Homeowner
        };
        var homeownerToken = TestTokenHelper.GenerateJwtToken(userId, testUser.Email, testUser.Role.ToString(), _configuration);

        var mockUserRepository = new Mock<IUserRepository>();
        mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(testUser);

        var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(IUserRepository));
            services.AddSingleton(mockUserRepository.Object);
        }, homeownerToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(
                "{ \"query\": \"{ currentUser { id firstName lastName email role } }\" }",
                System.Text.Encoding.UTF8,
                "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        Snapshot.Match(content, matchOptions => matchOptions.IgnoreField("data.currentUser.id"));
    }

    [Fact]
    public async Task GetCurrentUser_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = CreateClient(); // No JWT token

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(
                "{ \"query\": \"{ currentUser { id firstName lastName email role } }\" }",
                System.Text.Encoding.UTF8,
                "application/json")
        };

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var jsonResponse = JObject.Parse(content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(jsonResponse["errors"]);
        Assert.Contains("not authorized", jsonResponse["errors"]![0]!["message"]!.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
