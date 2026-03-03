using Xunit;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BuildSmart.Core.Application.Interfaces;
using Moq;
using System.Collections.Generic;
using BuildSmart.Core.Domain.Entities;
using System;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Linq;
using BuildSmart.Core.Domain.Enums;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using BuildSmart.Core.Domain.ValueObjects;

namespace BuildSmart.Api.Tests;

public class GetMyProjectsTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;
    private readonly IConfiguration _configuration;

    public GetMyProjectsTests(TestApplicationFactory factory)
    {
        _factory = factory;
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Jwt:Key", "SuperSecretKeyForTesting1234567890"},
                {"Jwt:Issuer", "BuildSmart"},
                {"Jwt:Audience", "BuildSmartUsers"}
            });
        _configuration = builder.Build();
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
                myProjects {
                    id
                    title
                    jobPosts {
                        id
                        status
                    }
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
        
        var status = json["data"]?["myProjects"]?[0]?["jobPosts"]?[0]?["status"]?.ToString();
        Assert.Equal("WAITING_FOR_ADMIN_REVIEW", status); // Confirmed API behavior
    }

    [Fact]
    public async Task GetMyProjects_WithNestedReplies_ReturnsDataSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userToken = TestTokenHelper.GenerateJwtToken(userId, "homeowner@test.com", "Homeowner", _configuration);

        var projectId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var replyId = Guid.NewGuid();

        var tradesmanId = Guid.NewGuid();
        var tradesmanUser = new User { Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Tradesman" };
        var tradesmanProfile = new TradesmanProfile { Id = tradesmanId, User = tradesmanUser };

        var question = new JobPostQuestion
        {
            Id = questionId,
            JobPostId = jobId,
            QuestionText = "How deep are the cabinets?",
            AnswerText = "24 inches",
            TradesmanProfileId = tradesmanId,
            TradesmanProfile = tradesmanProfile,
            AuthorId = tradesmanUser.Id,
            Author = tradesmanUser,
            Replies = new List<JobPostQuestion>
            {
                new JobPostQuestion
                {
                    Id = replyId,
                    ParentQuestionId = questionId,
                    QuestionText = "Thanks, that works!",
                    AuthorId = userId,
                    Author = new User { Id = userId, FirstName = "Test", LastName = "User" }
                }
            }
        };

        var feedbackId = Guid.NewGuid();
        var feedbackReplyId = Guid.NewGuid();
        var feedback = new JobPostFeedback
        {
            Id = feedbackId,
            JobPostId = jobId,
            Text = "Please clarify the height.",
            AuthorId = Guid.NewGuid(), // Admin
            Author = new User { Id = Guid.NewGuid(), FirstName = "Admin", LastName = "User" },
            Replies = new List<JobPostFeedback>
            {
                new JobPostFeedback
                {
                    Id = feedbackReplyId,
                    ParentFeedbackId = feedbackId,
                    Text = "The height is 36 inches.",
                    AuthorId = userId,
                    Author = new User { Id = userId, FirstName = "Test", LastName = "User" }
                }
            }
        };

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
            Questions = new List<JobPostQuestion> { question },
            Feedbacks = new List<JobPostFeedback> { feedback }
        };

        var project = new Project
        {
            Id = projectId,
            Title = "Test Renovation",
            Description = "Full house renovation",
            HomeownerId = userId,
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
            query GetMyProjects {
              myProjects {
                id
                jobPosts {
                  id
                  questions {
                    id
                    questionText
                    replies {
                      id
                      questionText
                    }
                  }
                  feedbacks {
                    id
                    text
                    replies {
                      id
                      text
                    }
                  }
                }
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
        
        var questionReplyText = json["data"]?["myProjects"]?[0]?["jobPosts"]?[0]?["questions"]?[0]?["replies"]?[0]?["questionText"]?.ToString();
        Assert.Equal("Thanks, that works!", questionReplyText);

        var feedbackReplyText = json["data"]?["myProjects"]?[0]?["jobPosts"]?[0]?["feedbacks"]?[0]?["replies"]?[0]?["text"]?.ToString();
        Assert.Equal("The height is 36 inches.", feedbackReplyText);
    }
}
