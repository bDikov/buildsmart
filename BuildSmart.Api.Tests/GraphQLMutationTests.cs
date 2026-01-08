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
            Role = BuildSmart.Core.Domain.Enums.UserRoleTypes.Homeowner
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
    public async Task CreateBooking_ValidData_ReturnsNewBooking()
    {
        // Arrange
        var newBookingId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        var tradesmanProfileId = Guid.NewGuid();
        var requestedDate = DateTime.UtcNow.AddDays(7);
        var jobDescription = "Fix leaky faucet";

        var mockBookingService = new Mock<IBookingService>();
        mockBookingService.Setup(service => service.CreateBookingAsync(
            homeownerId,
            tradesmanProfileId,
            requestedDate,
            jobDescription))
            .ReturnsAsync(new Booking
            {
                Id = newBookingId,
                HomeownerId = homeownerId,
                TradesmanProfileId = tradesmanProfileId,
                RequestedDate = requestedDate
                // Removed Status assignment as it has a private setter
            });

        var homeownerToken = TestTokenHelper.GenerateJwtToken(homeownerId, "homeowner@example.com", "Homeowner", _configuration);

        var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(IBookingService));
            services.AddSingleton(mockBookingService.Object);
        }, homeownerToken);

        var graphQLRequest = new
        {
            query = "mutation CreateBooking($homeownerId: UUID!, $tradesmanProfileId: UUID!, $requestedDate: DateTime!, $jobDescription: String!) { createBooking(homeownerId: $homeownerId, tradesmanProfileId: $tradesmanProfileId, requestedDate: $requestedDate, jobDescription: $jobDescription) { id homeownerId tradesmanProfileId requestedDate jobDescription status } }",
            variables = new
            {
                homeownerId = homeownerId.ToString(), // Convert to string
                tradesmanProfileId = tradesmanProfileId.ToString(), // Convert to string
                requestedDate = requestedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"), // Convert to ISO 8601 string
                jobDescription = jobDescription
            },
            operationName = "CreateBooking"
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
        
        // Snapshot testing for GraphQL responses, ignoring dynamic fields like dates and IDs
        Snapshot.Match(content, matchOptions => matchOptions.IgnoreField("data.createBooking.id").IgnoreField("data.createBooking.requestedDate"));
    }
}
