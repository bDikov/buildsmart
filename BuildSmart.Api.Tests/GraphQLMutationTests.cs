using Xunit;
using BuildSmart.Infrastructure.Persistence;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Microsoft.Extensions.DependencyInjection;
using BuildSmart.Core.Application.Interfaces;
using Moq;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.ValueObjects;
using System.Linq;
using System.Collections.Generic;
using BuildSmart.Api; // Needed for Program class
using Microsoft.AspNetCore.TestHost; // Needed for TestServer
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using BuildSmart.Core.Application.Services;
using Xunit.Abstractions; // Needed for ITestOutputHelper
using Microsoft.AspNetCore.Authentication; // Added
using Microsoft.Extensions.Options; // Added
using System.Security.Claims; // Added

namespace BuildSmart.Api.Tests;

public class GraphQLMutationTests : IClassFixture<TestApplicationFactory>
{
	private readonly TestApplicationFactory _factory;
	private readonly ITestOutputHelper _output; // Add ITestOutputHelper
	private readonly IConfiguration _configuration; // To access JWT settings

	public GraphQLMutationTests(TestApplicationFactory factory, ITestOutputHelper output)
	{
		_factory = factory;
		_output = output; // Initialize ITestOutputHelper
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
				services.RemoveAll(typeof(IBookingService));
				services.AddSingleton(new Mock<IBookingService>().Object);

				// Always use our test configuration for JWT settings
				services.RemoveAll(typeof(IConfiguration));
				services.AddSingleton(_configuration);

				configureServices?.Invoke(services);
			});
		}).CreateClient();

		// If useBasicAuth is true, explicitly add the Basic Authorization header.
		// Otherwise, the TestAuthHandler will handle JWT authentication based on whether jwtToken is provided.
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
	public async Task Login_ValidCredentials_ReturnsJwtToken()
	{
		// Arrange
		var mockUserRepository = new Mock<IUserRepository>();
		var testUser = new User
		{
			Id = Guid.NewGuid(),
			Email = "test@example.com",
			HashedPassword = BCrypt.Net.BCrypt.HashPassword("password123"),
			Role = BuildSmart.Core.Domain.Enums.UserRoleTypes.Homeowner,
			IsEmailVerified = true
		};
		mockUserRepository.Setup(repo => repo.GetByEmailAsync("test@example.com"))
			.ReturnsAsync(testUser);

		var mockUnitOfWork = new Mock<IUnitOfWork>();
		mockUnitOfWork.Setup(uow => uow.Users).Returns(mockUserRepository.Object);

		var client = CreateClient(services =>
		{
			services.RemoveAll(typeof(IUnitOfWork));
			services.AddSingleton(mockUnitOfWork.Object);
		}, useBasicAuth: true); // Use basic auth for this test

		var graphQLRequest = new
		{
			query = "mutation Login($email: String!, $password: String!) { login(email: $email, password: $password) }",
			variables = new
			{
				email = "test@example.com",
				password = "password123"
			},
			operationName = "Login"
		};

		var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
		{
			Content = new StringContent(
				JsonConvert.SerializeObject(graphQLRequest),
				System.Text.Encoding.UTF8,
				"application/json")
		};

		// Act
		var response = await client.SendAsync(request);

		// Assert
		response.EnsureSuccessStatusCode();
		var content = await response.Content.ReadAsStringAsync();

		// Assert that a token is returned and it's a valid JWT structure
		var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);
		string? token = jsonResponse?.data?.login; // Made token nullable

		Assert.False(string.IsNullOrEmpty(token));
		var handler = new JwtSecurityTokenHandler();
		Assert.True(handler.CanReadToken(token));
	}

	[Fact]
	public async Task MigratePasswords_ReturnsUpdatedCount()
	{
		// Arrange
		var adminId = Guid.NewGuid();
		var adminToken = TestTokenHelper.GenerateJwtToken(adminId, "admin@example.com", "Admin", _configuration);

		var mockUnitOfWork = new Mock<IUnitOfWork>();
		var mockDataMigrationService = new Mock<DataMigrationService>(mockUnitOfWork.Object); // Pass mocked IUnitOfWork
		mockDataMigrationService.Setup(service => service.HashExistingPasswordsAsync())
			.ReturnsAsync(5);

		var client = CreateClient(services =>
		{
			services.RemoveAll(typeof(DataMigrationService));
			services.AddSingleton(mockDataMigrationService.Object);
			services.RemoveAll(typeof(IUnitOfWork)); // Ensure the mocked UoW is used if DataMigrationService is resolved by DI
			services.AddSingleton(mockUnitOfWork.Object);
		}, adminToken); // Pass Admin JWT token

		var graphQLRequest = new
		{
			query = "mutation { migratePasswords }"
		};

		var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
		{
			Content = new StringContent(
				JsonConvert.SerializeObject(graphQLRequest),
				System.Text.Encoding.UTF8,
				"application/json")
		};

		// Act
		var response = await client.SendAsync(request);

		// Assert
		_output.WriteLine($"Status Code: {response.StatusCode}"); // Print status code
		var content = await response.Content.ReadAsStringAsync();
		_output.WriteLine($"Response Content: {content}"); // Print response content
		response.EnsureSuccessStatusCode();

		// Snapshot testing for GraphQL responses
		Snapshot.Match(content);
	}

	// [Fact]
	// public async Task RegisterUser_ValidData_ReturnsNewUser()
	// {
	//     // Arrange
	//     var mockUserRepository = new Mock<IUserRepository>();
	//     mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>()))
	//         .Returns(Task.CompletedTask);

	//     mockUserRepository.Setup(repo => repo.GetByEmailAsync("newuser@example.com"))
	//         .ReturnsAsync((User?)null); // Ensure user does not exist

	//     var mockUnitOfWork = new Mock<IUnitOfWork>();
	//     mockUnitOfWork.Setup(uow => uow.Users).Returns(mockUserRepository.Object);
	//     mockUnitOfWork.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

	//     var client = _factory.WithWebHostBuilder(builder =>
	//     {
	//         builder.ConfigureTestServices(services =>
	//         {
	//             services.RemoveAll(typeof(IUnitOfWork));
	//             services.AddSingleton(mockUnitOfWork.Object);
	//         });
	//     }).CreateClient();

	//     // Add Basic Authentication Header (assuming admin role for registration)
	//     var byteArray = Encoding.ASCII.GetBytes("basicauth:basicauth");
	//     client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

	//     var graphQLRequest = new
	//     {
	//         query = "mutation RegisterUser($firstName: String!, $lastName: String!, $email: String!, $password: String!) { registerUser(firstName: $firstName, lastName: $lastName, email: $email, password: $password) { id firstName lastName email role } }",
	//         variables = new
	//         {
	//             firstName = "New",
	//             lastName = "User",
	//             email = "newuser@example.com",
	//             password = "SecurePassword123"
	//         },
	//         operationName = "RegisterUser"
	//     };

	//     var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
	//     {
	//         Content = new StringContent(
	//             JsonConvert.SerializeObject(graphQLRequest),
	//             System.Text.Encoding.UTF8,
	//             "application/json")
	//     };

	//     // Act
	//     var response = await client.SendAsync(request);

	//     // Assert
	//     response.EnsureSuccessStatusCode();
	//     var content = await response.Content.ReadAsStringAsync();

	//     // No snapshot assertion for now
	// }

	[Fact]
	public async Task SubmitBid_WithBidItems_ReturnsSuccessfulBid()
	{
		// Arrange
		var tradesmanId = Guid.NewGuid();
		var jobPostId = Guid.NewGuid();
		var jobTaskId = Guid.NewGuid();
		var tradesmanToken = TestTokenHelper.GenerateJwtToken(tradesmanId, "tradesman@example.com", "Tradesman", _configuration);

		var mockJobPostService = new Mock<IJobPostService>();
		mockJobPostService.Setup(s => s.SubmitBidAsync(
				It.IsAny<Guid>(),
				It.IsAny<Guid>(),
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<DateTime?>(),
				It.IsAny<DateTime?>(),
				It.IsAny<int?>(),
				It.IsAny<IEnumerable<(Guid JobTaskId, decimal PriceSubtotal, string? Comment)>>()))
			.ReturnsAsync((Guid tradesmanIdArg, Guid jobPostIdArg, string currency, string comment, DateTime? earliest, DateTime? latest, int? duration, IEnumerable<(Guid JobTaskId, decimal PriceSubtotal, string? Comment)> itemsArg) =>
			{
				var newBid = new Bid
				{
					Id = Guid.NewGuid(),
					TradesmanProfileId = tradesmanIdArg,
					JobPostId = jobPostIdArg,
					Amount = Amount.Create(currency, itemsArg.Sum(i => i.PriceSubtotal)),
					Comment = comment,
					EarliestStartDate = earliest,
					LatestStartDate = latest,
					EstimatedDurationDays = duration,
					BidItems = itemsArg.Select(i => new BidItem
					{
						Id = Guid.NewGuid(),
						JobTaskId = i.JobTaskId,
						Price = Amount.Create(currency, i.PriceSubtotal),
						Comment = i.Comment
					}).ToList()
				};
				return newBid;
			});

		var client = CreateClient(services =>
		{
			services.AddSingleton(mockJobPostService.Object);
		}, tradesmanToken);

		var graphQLRequest = new
		{
			query = @"
                mutation SubmitBid($input: SubmitBidInput!) {
                  submitBid(input: $input) {
                    id
                    amount {
                      total
                      currency
                    }
                    comment
                    earliestStartDate
                    latestStartDate
                    estimatedDurationDays
                    bidItems {
                      id
                      jobTaskId
                      price {
                        total
                      }
                      comment
                    }
                  }
                }",
			variables = new
			{
				input = new
				{
					tradesmanProfileId = tradesmanId,
					jobPostId = jobPostId,
					currency = "USD",
					comment = "This is a structured bid.",
					earliestStartDate = DateTime.UtcNow.AddDays(7),
					latestStartDate = DateTime.UtcNow.AddDays(14),
					estimatedDurationDays = 10,
					bidItems = new[] {
						new {
							jobTaskId = jobTaskId,
							priceSubtotal = 1500m,
							comment = "Demo phase"
						}
					}
				}
			}
		};

		// Act
		var response = await client.PostAsync("/graphql", new StringContent(JsonConvert.SerializeObject(graphQLRequest), Encoding.UTF8, "application/json"));

		// Assert
		var content = await response.Content.ReadAsStringAsync();
		_output.WriteLine(content); // Write response to output for debugging
		response.EnsureSuccessStatusCode();

		Snapshot.Match(content, matchOptions => matchOptions
			.IgnoreField("data.submitBid.id")
			.IgnoreField("data.submitBid.earliestStartDate")
			.IgnoreField("data.submitBid.latestStartDate")
			.IgnoreField("data.submitBid.bidItems[*].id")
			.IgnoreField("data.submitBid.bidItems[*].jobTaskId"));
	}

	[Fact]
	public async Task UpdateJobTasks_ValidData_ReturnsTrue()
	{
		// Arrange
		var homeownerId = Guid.NewGuid();
		var jobPostId = Guid.NewGuid();
		var taskId = Guid.NewGuid();
		var criteriaId = Guid.NewGuid();
		var homeownerToken = TestTokenHelper.GenerateJwtToken(homeownerId, "homeowner@example.com", "Homeowner", _configuration);

		var mockJobPostService = new Mock<IJobPostService>();
		mockJobPostService.Setup(s => s.UpdateJobTasksAsync(
				It.Is<Guid>(id => id == jobPostId),
				It.IsAny<IEnumerable<(Guid? Id, string Title, string Description, int SequenceOrder, IEnumerable<(Guid? Id, string Description)> Criteria)>>()))
			.Returns(Task.CompletedTask);

		var client = CreateClient(services =>
		{
			services.AddSingleton(mockJobPostService.Object);
		}, homeownerToken);

		var graphQLRequest = new
		{
			query = @"
                mutation UpdateJobTasks($input: UpdateJobTasksInput!) {
                  updateJobTasks(input: $input)
                }",
			variables = new
			{
				input = new
				{
					jobPostId = jobPostId,
					tasks = new[] {
						new {
							id = taskId,
							title = "Task 1",
							description = "Task Description",
							sequenceOrder = 1,
							criteria = new[] {
								new {
									id = criteriaId,
									description = "Criteria Description"
								}
							}
						}
					}
				}
			}
		};

		// Act
		var response = await client.PostAsync("/graphql", new StringContent(JsonConvert.SerializeObject(graphQLRequest), Encoding.UTF8, "application/json"));

		// Assert
		var content = await response.Content.ReadAsStringAsync();
		_output.WriteLine(content);
		response.EnsureSuccessStatusCode();

		Snapshot.Match(content);
	}

	[Fact]
	public async Task ReplyToJobQuestion_ValidData_ReturnsReply()
	{
		// Arrange
		var parentQuestionId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		var replyId = Guid.NewGuid();
		var replyText = "This is a reply text.";

		var mockJobPostService = new Mock<IJobPostService>();
		mockJobPostService.Setup(service => service.ReplyToQuestionAsync(
			parentQuestionId,
			authorId,
			replyText))
			.ReturnsAsync(new JobPostQuestion
			{
				Id = replyId,
				ParentQuestionId = parentQuestionId,
				AuthorId = authorId,
				QuestionText = replyText
			});

		var userToken = TestTokenHelper.GenerateJwtToken(authorId, "user@example.com", "Tradesman", _configuration);

		var client = CreateClient(services =>
		{
			services.RemoveAll(typeof(IJobPostService));
			services.AddSingleton(mockJobPostService.Object);
		}, userToken);

		var graphQLRequest = new
		{
			query = "mutation ReplyToJobQuestion($parentQuestionId: UUID!, $replyText: String!) { replyToJobQuestion(parentQuestionId: $parentQuestionId, replyText: $replyText) { id parentQuestionId authorId questionText } }",
			variables = new
			{
				parentQuestionId = parentQuestionId.ToString(),
				replyText = replyText
			},
			operationName = "ReplyToJobQuestion"
		};

		var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
		{
			Content = new StringContent(
				JsonConvert.SerializeObject(graphQLRequest),
				System.Text.Encoding.UTF8,
				"application/json")
		};

		// Act
		var response = await client.SendAsync(request);

		// Assert
		response.EnsureSuccessStatusCode();
		var content = await response.Content.ReadAsStringAsync();

		// Snapshot testing
		Snapshot.Match(content, matchOptions => matchOptions
			.IgnoreField("data.replyToJobQuestion.id")
			.IgnoreField("data.replyToJobQuestion.parentQuestionId")
			.IgnoreField("data.replyToJobQuestion.authorId"));
	}

	[Fact]
	public async Task AcceptBid_ValidData_ReturnsAcceptedBooking()
	{
		// Arrange
		var homeownerId = Guid.NewGuid();
		var bidId = Guid.NewGuid();
		var bookingId = Guid.NewGuid();

		var homeownerToken = TestTokenHelper.GenerateJwtToken(homeownerId, "homeowner@example.com", "Homeowner", _configuration);

		var mockPaymentService = new Mock<IPaymentService>();
		mockPaymentService.Setup(s => s.AcceptBidAsync(homeownerId, bidId))
			.ReturnsAsync(new Booking
			{
				Id = bookingId,
				HomeownerId = homeownerId,
				BidId = bidId
			});

		var client = CreateClient(services =>
		{
			services.RemoveAll(typeof(IPaymentService));
			services.AddSingleton(mockPaymentService.Object);
		}, homeownerToken);

		var graphQLRequest = new
		{
			query = @"
                mutation AcceptBid($bidId: UUID!) {
                  acceptBid(bidId: $bidId) {
                    id
                    status
                  }
                }",
			variables = new
			{
				bidId = bidId.ToString()
			}
		};

		var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
		{
			Content = new StringContent(
				JsonConvert.SerializeObject(graphQLRequest),
				Encoding.UTF8,
				"application/json")
		};

		// Act
		var response = await client.SendAsync(request);

		// Assert
		var content = await response.Content.ReadAsStringAsync();
		_output.WriteLine(content);
		response.EnsureSuccessStatusCode();

		Snapshot.Match(content, matchOptions => matchOptions.IgnoreField("data.acceptBid.id"));
	}

	[Fact]
	public async Task ApproveMilestone_ValidData_ReturnsTrue()
	{
		// Arrange
		var homeownerId = Guid.NewGuid();
		var milestoneId = Guid.NewGuid();

		var homeownerToken = TestTokenHelper.GenerateJwtToken(homeownerId, "homeowner@example.com", "Homeowner", _configuration);

		var mockPaymentService = new Mock<IPaymentService>();
		mockPaymentService.Setup(s => s.ApproveMilestoneAsync(homeownerId, milestoneId))
			.Returns(Task.CompletedTask);

		var client = CreateClient(services =>
		{
			services.RemoveAll(typeof(IPaymentService));
			services.AddSingleton(mockPaymentService.Object);
		}, homeownerToken);

		var graphQLRequest = new
		{
			query = @"
                mutation ApproveMilestone($milestoneId: UUID!) {
                  approveMilestone(milestonePaymentId: $milestoneId)
                }",
			variables = new
			{
				milestoneId = milestoneId.ToString()
			}
		};

		var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
		{
			Content = new StringContent(
				JsonConvert.SerializeObject(graphQLRequest),
				Encoding.UTF8,
				"application/json")
		};

		// Act
		var response = await client.SendAsync(request);

		// Assert
		var content = await response.Content.ReadAsStringAsync();
		_output.WriteLine(content);
		response.EnsureSuccessStatusCode();

		Snapshot.Match(content);
		}

		[Fact]
		public async Task ConfirmVideoUpload_ValidData_ReplacesDomain()
		{
		// Arrange
		var adminToken = TestTokenHelper.GenerateJwtToken(Guid.NewGuid(), "admin@example.com", "Admin", _configuration);
		var tradesmanUserId = Guid.NewGuid();
		var tradesmanProfileId = Guid.NewGuid();

		var testProfile = new TradesmanProfile
		{
		Id = tradesmanProfileId,
		UserId = tradesmanUserId
		};

		var mockProfileRepo = new Mock<ITradesmanProfileRepository>();
		mockProfileRepo.Setup(r => r.GetByUserIdAsync(tradesmanUserId))
		.ReturnsAsync(testProfile);
		mockProfileRepo.Setup(r => r.AddMediaAsync(It.IsAny<TradesmanMedia>()))
		.Returns(Task.CompletedTask);

		var mockUnitOfWork = new Mock<IUnitOfWork>();
		mockUnitOfWork.Setup(u => u.TradesmanProfiles).Returns(mockProfileRepo.Object);

		var client = CreateClient(services =>
		{
		services.RemoveAll(typeof(IUnitOfWork));
		services.AddSingleton(mockUnitOfWork.Object);

		var configValues = new Dictionary<string, string>
		{
		{"CloudflareR2:PublicUrl", "https://pub-my-cool-url.r2.dev"},
		{"CloudflareR2:BucketName", "buildsmart-media"}
		};
		// Add custom configuration builder on top of existing ones
		var newConfig = new ConfigurationBuilder()
		.AddInMemoryCollection(configValues)
		.Build();

		services.AddSingleton<IConfiguration>(newConfig);
		}, adminToken);

		var rawS3Url = "https://myaccount.r2.cloudflarestorage.com/buildsmart-media/myvid.mp4?X-Amz-Signature=12345";

		var graphQLRequest = new
		{
		query = "mutation Confirm($userId: UUID!, $url: String!, $type: MediaType!) { confirmVideoUpload(tradesmanUserId: $userId, videoUrl: $url, type: $type) { videoUrl type } }",
		variables = new
		{
		userId = tradesmanUserId,
		url = rawS3Url,
		type = "VIDEO"
		}
		};

		var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
		{
		Content = new StringContent(JsonConvert.SerializeObject(graphQLRequest), Encoding.UTF8, "application/json")
		};

		// Act
		var response = await client.SendAsync(request);
		var content = await response.Content.ReadAsStringAsync();

		// Assert
		response.EnsureSuccessStatusCode();
		var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);

		Assert.Null(jsonResponse.errors);
		string savedUrl = jsonResponse.data.confirmVideoUpload.videoUrl;
		string savedType = jsonResponse.data.confirmVideoUpload.type;

		Assert.Equal("https://pub-my-cool-url.r2.dev/myvid.mp4", savedUrl);
		Assert.Equal("VIDEO", savedType);
		mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
		}

	[Fact]
	public async Task ApproveJobScope_ValidData_ReturnsTrue()
	{
		// Arrange
		var homeownerId = Guid.NewGuid();
		var jobPostId = Guid.NewGuid();
		var finalScope = "Approved scope text description";

		var homeownerToken = TestTokenHelper.GenerateJwtToken(homeownerId, "homeowner@example.com", "Homeowner", _configuration);

		var mockJobPostService = new Mock<IJobPostService>();
		mockJobPostService.Setup(s => s.ApproveJobScopeAsync(jobPostId, finalScope))
			.Returns(Task.CompletedTask);

		var client = CreateClient(services =>
		{
			services.RemoveAll(typeof(IJobPostService));
			services.AddSingleton(mockJobPostService.Object);
		}, homeownerToken);

		var graphQLRequest = new
		{
			query = @"
                mutation ApproveScope($jobPostId: UUID!, $finalScope: String!) {
                  approveJobScope(jobPostId: $jobPostId, finalScope: $finalScope)
                }",
			variables = new
			{
				jobPostId = jobPostId.ToString(),
				finalScope = finalScope
			}
		};

		var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
		{
			Content = new StringContent(
				JsonConvert.SerializeObject(graphQLRequest),
				Encoding.UTF8,
				"application/json")
		};

		// Act
		var response = await client.SendAsync(request);

		// Assert
		var content = await response.Content.ReadAsStringAsync();
		_output.WriteLine(content);
		response.EnsureSuccessStatusCode();

		var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);
		Assert.Null(jsonResponse.errors);
		bool approved = jsonResponse.data.approveJobScope;
		Assert.True(approved);

		mockJobPostService.Verify(s => s.ApproveJobScopeAsync(jobPostId, finalScope), Times.Once);
	}

	[Fact]
	public async Task UpdateProjectLocation_ValidData_ReturnsTrue()
	{
		// Arrange
		var homeownerId = Guid.NewGuid();
		var projectId = Guid.NewGuid();
		var location = "123 New Address St";

		var homeownerToken = TestTokenHelper.GenerateJwtToken(homeownerId, "homeowner@example.com", "Homeowner", _configuration);

		var project = new Project { Id = projectId, HomeownerId = homeownerId };
		var jobs = new List<JobPost>
		{
			new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, Location = "Old Address" },
			new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, Location = "Old Address" }
		};

		var mockProjectRepo = new Mock<IProjectRepository>();
		mockProjectRepo.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync(project);

		var mockJobPostRepo = new Mock<IJobPostRepository>();
		mockJobPostRepo.Setup(r => r.GetJobsByProjectIdAsync(projectId)).ReturnsAsync(jobs);

		var mockUnitOfWork = new Mock<IUnitOfWork>();
		mockUnitOfWork.Setup(u => u.Projects).Returns(mockProjectRepo.Object);
		mockUnitOfWork.Setup(u => u.JobPosts).Returns(mockJobPostRepo.Object);
		mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

		var client = CreateClient(services =>
		{
			services.RemoveAll(typeof(IUnitOfWork));
			services.AddSingleton(mockUnitOfWork.Object);
		}, homeownerToken);

		var graphQLRequest = new
		{
			query = @"
                mutation UpdateLocation($projectId: UUID!, $location: String!) {
                  updateProjectLocation(projectId: $projectId, location: $location)
                }",
			variables = new
			{
				projectId = projectId.ToString(),
				location = location
			}
		};

		var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
		{
			Content = new StringContent(
				JsonConvert.SerializeObject(graphQLRequest),
				Encoding.UTF8,
				"application/json")
		};

		// Act
		var response = await client.SendAsync(request);

		// Assert
		var content = await response.Content.ReadAsStringAsync();
		_output.WriteLine(content);
		response.EnsureSuccessStatusCode();

		var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);
		Assert.Null(jsonResponse.errors);
		bool success = jsonResponse.data.updateProjectLocation;
		Assert.True(success);

		Assert.All(jobs, j => Assert.Equal(location, j.Location));
		mockJobPostRepo.Verify(r => r.Update(It.IsAny<JobPost>()), Times.Exactly(2));
		mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task DeleteTradesmanMedia_ValidId_DeletesDbRecordAndCDNFiles()
	{
		// Arrange
		var adminToken = TestTokenHelper.GenerateJwtToken(Guid.NewGuid(), "admin@example.com", "Admin", _configuration);
		var mediaId = Guid.NewGuid();
		var videoUrl = "https://pub-my-cool-url.r2.dev/myvid.mp4";
		var imageUrl = "https://pub-my-cool-url.r2.dev/thumb.jpg";

		// Setup Mock MultimediaStorageService
		var mockStorageService = new Mock<IMultimediaStorageService>();
		mockStorageService.Setup(s => s.DeleteFileAsync(It.IsAny<string>()))
			.Returns(Task.CompletedTask);

		var client = CreateClient(services =>
		{
			services.RemoveAll(typeof(IMultimediaStorageService));
			services.AddSingleton(mockStorageService.Object);
		}, adminToken);

		// Seed database
		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			db.Database.EnsureCreated();

			var testMedia = new TradesmanMedia
			{
				Id = mediaId,
				TradesmanId = Guid.NewGuid(),
				VideoUrl = videoUrl,
				ImageUrl = imageUrl,
				Type = Core.Domain.Enums.MediaType.Video,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			db.TradesmanMedia.Add(testMedia);
			await db.SaveChangesAsync();
		}

		// Act: Execute GraphQL mutation
		var graphQLRequest = new
		{
			query = @"
                mutation DeleteMedia($mediaId: UUID!) {
                  deleteTradesmanMedia(mediaId: $mediaId)
                }",
			variables = new
			{
				mediaId = mediaId.ToString()
			}
		};

		var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
		{
			Content = new StringContent(
				JsonConvert.SerializeObject(graphQLRequest),
				Encoding.UTF8,
				"application/json")
		};

		var response = await client.SendAsync(request);

		// Assert
		var content = await response.Content.ReadAsStringAsync();
		_output.WriteLine(content);
		response.EnsureSuccessStatusCode();

		var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);
		Assert.Null(jsonResponse.errors);
		bool success = jsonResponse.data.deleteTradesmanMedia;
		Assert.True(success);

		// Verify CDN file deletion calls
		mockStorageService.Verify(s => s.DeleteFileAsync(videoUrl), Times.Once);
		mockStorageService.Verify(s => s.DeleteFileAsync(imageUrl), Times.Once);

		// Verify database removal
		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var mediaInDb = await db.TradesmanMedia.FindAsync(mediaId);
			Assert.Null(mediaInDb);
		}
	}
}

