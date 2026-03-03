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
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace BuildSmart.Api.Tests;

public class GetMyProjectsTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;
    private readonly IConfiguration _configuration;

    public GetMyProjectsTests(TestApplicationFactory factory)
    {
        _factory = factory;
        _configuration = factory.Services.GetRequiredService<IConfiguration>();
    }

    private HttpClient CreateClient(Action<IServiceCollection>? configureServices = null, string? jwtToken = null)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            if (configureServices != null)
            {
                builder.ConfigureServices(configureServices);
            }
        }).CreateClient();

        if (!string.IsNullOrEmpty(jwtToken))
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");
        }

        return client;
    }

    [Fact]
    public async Task GetMyProjects_ReturnsProjectsAndJobPosts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var homeownerToken = TestTokenHelper.GenerateJwtToken(userId, "homeowner@example.com", "Homeowner", _configuration);

        var mockProjectRepository = new Mock<IProjectRepository>();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Kitchen Renovation",
            HomeownerId = userId,
            JobPosts = new List<JobPost>
            {
                new JobPost
                {
                    Id = Guid.NewGuid(),
                    Title = "Electrical Work",
                    ServiceCategory = new ServiceCategory { Name = "Electrical" }
                }
            }
        };

        mockProjectRepository.Setup(r => r.GetProjectsByHomeownerAsync(userId))
            .ReturnsAsync(new List<Project> { project });

        var client = CreateClient(services =>
        {
            services.AddSingleton(mockProjectRepository.Object);
        }, homeownerToken);

        var graphQLRequest = new
        {
            query = "query GetMyProjects { myProjects { id title status jobPosts { id title status serviceCategory { name } } } }",
            variables = new { }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(
                Newtonsoft.Json.JsonConvert.SerializeObject(graphQLRequest),
                Encoding.UTF8,
                "application/json")
        };

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var json = JObject.Parse(content);
        
        if (json["errors"] != null)
        {
            Assert.Fail($"GraphQL Errors: {json["errors"]}");
        }

        var projects = json["data"]?["myProjects"] as JArray;
        Assert.NotNull(projects);
        Assert.Single(projects);
        var projectNode = projects[0];
        Assert.Equal("Kitchen Renovation", projectNode?["title"]?.ToString());
        Assert.Single(projectNode?["jobPosts"] as JArray);
    }

    [Fact]
    public async Task GetMyProjects_WithNestedReplies_ReturnsDataSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tradesmanId = Guid.NewGuid();
        var userToken = TestTokenHelper.GenerateJwtToken(userId, "user@example.com", "Homeowner", _configuration);

        var mockProjectRepo = new Mock<IProjectRepository>();
        
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Main Project",
            HomeownerId = userId
        };

        var jobPost = new JobPost
        {
            Id = Guid.NewGuid(),
            Title = "Job 1",
            ProjectId = project.Id,
            Project = project 
        };
        project.JobPosts = new List<JobPost> { jobPost };

        var question = new JobPostQuestion
        {
            Id = Guid.NewGuid(),
            QuestionText = "How tall?",
            AuthorId = tradesmanId,
            JobPostId = jobPost.Id,
            JobPost = jobPost,
            IsEdited = true // Set to true to test IsEdited
        };
        
        var myReply = new JobPostQuestion
        {
            Id = Guid.NewGuid(),
            QuestionText = "Thanks, that works!",
            AuthorId = userId, 
            ParentQuestionId = question.Id,
            JobPostId = jobPost.Id,
            JobPost = jobPost,
            IsEdited = false
        };
        question.Replies = new List<JobPostQuestion> { myReply };
        jobPost.Questions = new List<JobPostQuestion> { question };

        var feedback = new JobPostFeedback
        {
            Id = Guid.NewGuid(),
            Text = "Needs more light",
            AuthorId = Guid.NewGuid(),
            JobPostId = jobPost.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow.AddMinutes(5) // Simulates an edit
        };
        
        var myFeedbackReply = new JobPostFeedback
        {
            Id = Guid.NewGuid(),
            Text = "The height is 36 inches.",
            AuthorId = userId, 
            ParentFeedbackId = feedback.Id,
            JobPostId = jobPost.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow // Simulates no edit
        };
        feedback.Replies = new List<JobPostFeedback> { myFeedbackReply };
        jobPost.Feedbacks = new List<JobPostFeedback> { feedback };

        mockProjectRepo.Setup(r => r.GetProjectsByHomeownerAsync(userId))
            .ReturnsAsync(new List<Project> { project });

        var client = CreateClient(services =>
        {
            services.AddSingleton(mockProjectRepo.Object);
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
                    isEditable
                    isAnswerEditable
                    isEdited
                    createdAt
                    replies {
                      id
                      questionText
                      isEditable
                      isEdited
                      createdAt
                    }
                  }
                  feedbacks {
                    id
                    text
                    isEditable
                    isEdited
                    createdAt
                    replies {
                      id
                      text
                      isEditable
                      isEdited
                      createdAt
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
        
        var questionNode = json["data"]?["myProjects"]?[0]?["jobPosts"]?[0]?["questions"]?[0];
        Assert.True((bool)questionNode?["isEdited"]!);
        Assert.NotNull(questionNode?["createdAt"]);

        var questionReply = questionNode?["replies"]?[0];
        Assert.Equal("Thanks, that works!", questionReply?["questionText"]?.ToString());
        Assert.True((bool)questionReply?["isEditable"]!);
        Assert.False((bool)questionReply?["isEdited"]!);
        Assert.NotNull(questionReply?["createdAt"]);

        var feedbackNode = json["data"]?["myProjects"]?[0]?["jobPosts"]?[0]?["feedbacks"]?[0];
        Assert.True((bool)feedbackNode?["isEdited"]!);
        Assert.NotNull(feedbackNode?["createdAt"]);

        var feedbackReply = feedbackNode?["replies"]?[0];
        Assert.Equal("The height is 36 inches.", feedbackReply?["text"]?.ToString());
        Assert.True((bool)feedbackReply?["isEditable"]!);
        Assert.False((bool)feedbackReply?["isEdited"]!);
        Assert.NotNull(feedbackReply?["createdAt"]);
        
        Assert.False((bool)questionNode?["isEditable"]!);
        Assert.True((bool)questionNode?["isAnswerEditable"]!);
    }
}
