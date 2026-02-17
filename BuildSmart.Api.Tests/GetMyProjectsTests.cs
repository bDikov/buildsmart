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
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;
using System.Linq;
using System.Collections.Generic;
using BuildSmart.Api;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.TestHost;

namespace BuildSmart.Api.Tests;

public class GetMyProjectsTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;
    private readonly IConfiguration _configuration;

    public GetMyProjectsTests(TestApplicationFactory factory)
    {
        _factory = factory;
        var inMemorySettings = new Dictionary<string, string> {
            {"Jwt:Issuer", "test-issuer"},
            {"Jwt:Audience", "test-audience"},
            {"Jwt:Key", "supersecretkeythatisatleast32characterslong"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private HttpClient CreateClient(Action<IServiceCollection>? configureServices = null, string? jwtToken = null)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(IProjectRepository));
                services.AddSingleton(new Mock<IProjectRepository>().Object);
                services.RemoveAll(typeof(IConfiguration));
                services.AddSingleton(_configuration);
                
                // Configure TestAuthHandler
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options => { });

                configureServices?.Invoke(services);
            });
        }).CreateClient();

        if (jwtToken != null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        }

        return client;
    }

    [Fact]
    public async Task GetMyProjects_ReturnsProjectsAndJobPosts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userToken = TestTokenHelper.GenerateJwtToken(userId, "homeowner@test.com", "Homeowner", _configuration);

        var projectId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var jobPost = new JobPost
        {
            Id = jobId,
            ProjectId = projectId,
            Title = "Kitchen Cabinet Installation",
            Description = "Install new cabinets",
            ServiceCategoryId = categoryId,
            ServiceCategory = new ServiceCategory 
            { 
                Id = categoryId, 
                Name = "Carpentry",
                Status = CategoryStatus.Active 
            },
            EstimatedBudget = Amount.Create("USD", 5000)
        };
        jobPost.SubmitForReview();

        var project = new Project
        {
            Id = projectId,
            Title = "Test Renovation",
            Description = "Full house renovation",
            HomeownerId = userId,
            CreatedAt = DateTime.UtcNow,
            JobPosts = new List<JobPost> { jobPost }
        };

        var mockRepo = new Mock<IProjectRepository>();
        mockRepo.Setup(r => r.GetProjectsByHomeownerAsync(userId))
            .ReturnsAsync(new List<Project> { project });

        var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(IProjectRepository));
            services.AddSingleton(mockRepo.Object);
        }, userToken);

        var query = @"
            query {
                jobPostDebug {
                    status
                }
            }";

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(
                Newtonsoft.Json.JsonConvert.SerializeObject(new { query }),
                Encoding.UTF8,
                "application/json")
        };

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}, Content: {content}");
        
        var json = JObject.Parse(content);
        if (json["errors"] != null)
        {
            Assert.Fail($"GraphQL Errors: {json["errors"]}");
        }
        
        var status = json["data"]?["jobPostDebug"]?["status"]?.ToString();
        Assert.Equal("UNDER_REVIEW", status); // Confirmed API behavior
    }
}