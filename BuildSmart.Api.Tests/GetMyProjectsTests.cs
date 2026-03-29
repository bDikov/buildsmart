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
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            IsEdited = true
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
            UpdatedAt = DateTime.UtcNow.AddMinutes(5)
        };
        
        var myFeedbackReply = new JobPostFeedback
        {
            Id = Guid.NewGuid(),
            Text = "The height is 36 inches.",
            AuthorId = userId, 
            ParentFeedbackId = feedback.Id,
            JobPostId = jobPost.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        feedback.Replies = new List<JobPostFeedback> { myFeedbackReply };
        jobPost.Feedbacks = new List<JobPostFeedback> { feedback };

        mockProjectRepo.Setup(r => r.GetProjectsByHomeownerAsync(userId))
            .ReturnsAsync(new List<Project> { project });

        var client = CreateClient(services =>
        {
            services.AddSingleton(mockProjectRepo.Object);
            
            // Mock IJobPostService for the field resolvers
            var mockJobPostService = new Mock<IJobPostService>();

            var questionsLookup = new List<JobPostQuestion> { question }.ToLookup(q => q.JobPostId);
            mockJobPostService.Setup(s => s.GetQuestionsBatchByJobPostIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(questionsLookup);

            var feedbacksLookup = new List<JobPostFeedback> { feedback }.ToLookup(f => f.JobPostId);
            mockJobPostService.Setup(s => s.GetFeedbacksBatchByJobPostIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(feedbacksLookup);

            mockJobPostService.Setup(s => s.GetQuestionRepliesAsync(question.Id, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(question.Replies);
            mockJobPostService.Setup(s => s.GetQuestionReplyCountAsync(question.Id))
                .ReturnsAsync(question.Replies.Count);

            var replyCounts = new Dictionary<Guid, int> { { question.Id, question.Replies.Count } };
            mockJobPostService.Setup(s => s.GetQuestionReplyCountsBatchAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(replyCounts);
                
            services.AddSingleton(mockJobPostService.Object);
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
                    replyCount
                    replies(offset: 0, limit: 5) {
                      id
                      questionText
                      authorId
                      isEditable
                      isEdited
                      createdAt
                    }
                  }
                  feedbacks {
                    id
                    text
                    authorId
                    isEditable
                    isEdited
                    createdAt
                    replies {
                      id
                      text
                      authorId
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
        var questionReply = questionNode?["replies"]?[0];
        
        // Use Guid.Parse to handle hyphenation differences
        var returnedAuthorId = questionReply?["authorId"]?.ToString();
        Assert.Equal(userId, Guid.Parse(returnedAuthorId!));
        
        Assert.True((bool)questionReply?["isEditable"]!, $"isEditable was false for reply. Current User: {userId}, Reply Author: {returnedAuthorId}");

        var feedbackNode = json["data"]?["myProjects"]?[0]?["jobPosts"]?[0]?["feedbacks"]?[0];
        var feedbackReply = feedbackNode?["replies"]?[0];
        Assert.True((bool)feedbackReply?["isEditable"]!);
        
        Assert.False((bool)questionNode?["isEditable"]!);
        Assert.True((bool)questionNode?["isAnswerEditable"]!);
    }

    [Fact]
    public async Task GetMyProjects_WithBids_ReturnsTasksAndCriteria()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userToken = TestTokenHelper.GenerateJwtToken(userId, "user@example.com", "Homeowner", _configuration);

        var mockProjectRepo = new Mock<IProjectRepository>();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Project with Bids",
            HomeownerId = userId
        };
        var jobPost = new JobPost { Id = Guid.NewGuid(), ProjectId = project.Id, Project = project };
        project.JobPosts = new List<JobPost> { jobPost };

        var jobTask = new JobTask 
        { 
            Id = Guid.NewGuid(), 
            Title = "Task 1", 
            AcceptanceCriteria = new List<TaskAcceptanceCriteria>
            {
                new TaskAcceptanceCriteria { Id = Guid.NewGuid(), Description = "Criteria 1" }
            }
        };

        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            JobPostId = jobPost.Id,
            JobPost = jobPost,
            BidItems = new List<BidItem>
            {
                new BidItem
                {
                    Id = Guid.NewGuid(),
                    JobTaskId = jobTask.Id,
                    JobTask = jobTask,
                    Price = BuildSmart.Core.Domain.ValueObjects.Amount.Create("USD", 1000)
                }
            }
        };

        mockProjectRepo.Setup(r => r.GetProjectsByHomeownerAsync(userId)).ReturnsAsync(new List<Project> { project });

        var client = CreateClient(services =>
        {
            services.AddSingleton(mockProjectRepo.Object);
            
            var mockJobPostService = new Mock<IJobPostService>();
            var bidsLookup = new List<Bid> { bid }.ToLookup(b => b.JobPostId);
            mockJobPostService.Setup(s => s.GetBidsBatchByJobPostIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(bidsLookup);

            var jobTasksLookup = new List<JobTask> { jobTask }.ToLookup(jt => jobPost.Id);
            mockJobPostService.Setup(s => s.GetJobTasksBatchByJobPostIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(jobTasksLookup);
                
            services.AddSingleton(mockJobPostService.Object);

            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockJobTaskRepo = new Mock<IJobTaskRepository>();
            mockJobTaskRepo.Setup(r => r.GetQueryable()).Returns(new List<JobTask> { jobTask }.AsQueryable());
            mockUnitOfWork.Setup(u => u.JobTasks).Returns(mockJobTaskRepo.Object);
            services.RemoveAll(typeof(IUnitOfWork));
            services.AddSingleton(mockUnitOfWork.Object);

        }, userToken);

        var query = @"
            query GetMyProjects {
              myProjects {
                jobPosts {
                  jobTasks {
                    title
                    acceptanceCriteria { description }
                  }
                  bids {
                    id
                    bidItems {
                      jobTask { title }
                      price { total }
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

        var jobPostsArray = json["data"]?["myProjects"]?[0]?["jobPosts"] as JArray;
        Assert.NotNull(jobPostsArray);
        Assert.Single(jobPostsArray);

        var jobTasksArray = jobPostsArray[0]?["jobTasks"] as JArray;
        Assert.NotNull(jobTasksArray);
        Assert.Single(jobTasksArray);
        Assert.Equal("Task 1", jobTasksArray[0]?["title"]?.ToString());

        var bidsArray = jobPostsArray[0]?["bids"] as JArray;
        Assert.NotNull(bidsArray);
        Assert.Single(bidsArray);
        
        var bidItemsArray = bidsArray[0]?["bidItems"] as JArray;
        Assert.NotNull(bidItemsArray);
        Assert.Single(bidItemsArray);
        Assert.Equal("Task 1", bidItemsArray[0]?["jobTask"]?["title"]?.ToString());
    }
}
