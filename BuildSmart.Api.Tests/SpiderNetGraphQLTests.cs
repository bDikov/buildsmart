using Xunit;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using System.Collections.Generic;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http.Headers;
using Xunit.Abstractions;
using System.Threading;

namespace BuildSmart.Api.Tests;

public class SpiderNetGraphQLTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;
    private readonly ITestOutputHelper _output;
    private readonly IConfiguration _configuration;

    public SpiderNetGraphQLTests(TestApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;

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
    public async Task SaveCategory_NewCategory_CreatesAndReturnsCategory()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var adminToken = TestTokenHelper.GenerateJwtToken(adminId, "admin@example.com", "Admin", _configuration);

        var mockCategoryRepo = new Mock<IServiceCategoryRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.ServiceCategories).Returns(mockCategoryRepo.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(IUnitOfWork));
            services.AddSingleton(mockUnitOfWork.Object);
        }, adminToken);

        var requestBody = new
        {
            query = @"
                mutation Save($name: String!, $description: String, $isGlobal: Boolean!, $templateStructure: String!, $status: CategoryStatus) {
                  saveCategory(name: $name, description: $description, isGlobal: $isGlobal, templateStructure: $templateStructure, status: $status) {
                    id
                    name
                    description
                    isGlobal
                    status
                  }
                }",
            variables = new
            {
                name = "New Test Category",
                description = "Test Description",
                isGlobal = false,
                templateStructure = "{}",
                status = "ACTIVE"
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine(content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);
        Assert.Null(jsonResponse.errors);
        string name = jsonResponse.data.saveCategory.name;
        Assert.Equal("New Test Category", name);

        mockCategoryRepo.Verify(r => r.AddAsync(It.Is<ServiceCategory>(c => c.Name == "New Test Category")), Times.Once);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateQuestion_ValidData_CreatesAndReturnsQuestion()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var adminToken = TestTokenHelper.GenerateJwtToken(adminId, "admin@example.com", "Admin", _configuration);

        var mockQuestionService = new Mock<IQuestionManagementService>();
        mockQuestionService.Setup(s => s.CreateQuestionAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question q, CancellationToken ct) => {
                q.Id = Guid.NewGuid();
                return q;
            });

        var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(IQuestionManagementService));
            services.AddSingleton(mockQuestionService.Object);
        }, adminToken);

        var requestBody = new
        {
            query = @"
                mutation CreateQ($questionCode: String!, $text: String!, $type: String!, $isRequired: Boolean!, $optionsJson: String, $hintText: String, $serviceCategoryId: UUID, $parentQuestionId: UUID, $displayOrder: Int!, $visibilityCondition: String) {
                  createQuestion(
                    questionCode: $questionCode
                    text: $text
                    type: $type
                    isRequired: $isRequired
                    optionsJson: $optionsJson
                    hintText: $hintText
                    serviceCategoryId: $serviceCategoryId
                    parentQuestionId: $parentQuestionId
                    displayOrder: $displayOrder
                    visibilityCondition: $visibilityCondition
                  ) {
                    id
                    questionCode
                    text
                  }
                }",
            variables = new
            {
                questionCode = "test_code",
                text = "Test Question Text",
                type = "choice",
                isRequired = true,
                optionsJson = "[\"Yes\", \"No\"]",
                hintText = "Test Hint",
                serviceCategoryId = Guid.NewGuid().ToString(),
                parentQuestionId = (string?)null,
                displayOrder = 1,
                visibilityCondition = (string?)null
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine(content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);
        Assert.Null(jsonResponse.errors);
        string code = jsonResponse.data.createQuestion.questionCode;
        Assert.Equal("test_code", code);

        mockQuestionService.Verify(s => s.CreateQuestionAsync(It.Is<Question>(q => q.QuestionCode == "test_code"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateServiceSku_ValidData_CreatesAndReturnsSku()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var adminToken = TestTokenHelper.GenerateJwtToken(adminId, "admin@example.com", "Admin", _configuration);

        var mockSkuRepo = new Mock<IServiceSkuRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.ServiceSkus).Returns(mockSkuRepo.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(IUnitOfWork));
            services.AddSingleton(mockUnitOfWork.Object);
        }, adminToken);

        var categoryId = Guid.NewGuid();
        var requestBody = new
        {
            query = @"
                mutation CreateSku($categoryId: UUID!, $skuCode: String!, $name: String!, $description: String!, $basePrice: Decimal!, $unitType: String!) {
                  createServiceSku(
                    categoryId: $categoryId
                    skuCode: $skuCode
                    name: $name
                    description: $description
                    basePrice: $basePrice
                    unitType: $unitType
                  ) {
                    id
                    skuCode
                    name
                  }
                }",
            variables = new
            {
                categoryId = categoryId.ToString(),
                skuCode = "TEST-SKU-01",
                name = "Test SKU Name",
                description = "Test SKU Desc",
                basePrice = 12.50,
                unitType = "sqm"
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine(content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);
        Assert.Null(jsonResponse.errors);
        string code = jsonResponse.data.createServiceSku.skuCode;
        Assert.Equal("TEST-SKU-01", code);

        mockSkuRepo.Verify(r => r.AddAsync(It.Is<ServiceSku>(s => s.SkuCode == "TEST-SKU-01" && s.ServiceCategoryId == categoryId)), Times.Once);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteServiceSku_ValidData_DeletesSkuAndReturnsTrue()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var adminToken = TestTokenHelper.GenerateJwtToken(adminId, "admin@example.com", "Admin", _configuration);

        var skuId = Guid.NewGuid();
        var testSku = new ServiceSku { Id = skuId, SkuCode = "TEST-DELETE-01" };

        var mockSkuRepo = new Mock<IServiceSkuRepository>();
        mockSkuRepo.Setup(r => r.GetByIdAsync(skuId)).ReturnsAsync(testSku);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.ServiceSkus).Returns(mockSkuRepo.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var client = CreateClient(services =>
        {
            services.RemoveAll(typeof(IUnitOfWork));
            services.AddSingleton(mockUnitOfWork.Object);
        }, adminToken);

        var requestBody = new
        {
            query = @"
                mutation DeleteSku($id: UUID!) {
                  deleteServiceSku(id: $id)
                }",
            variables = new
            {
                id = skuId.ToString()
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine(content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(content);
        Assert.Null(jsonResponse.errors);
        bool success = jsonResponse.data.deleteServiceSku;
        Assert.True(success);

        mockSkuRepo.Verify(r => r.Delete(testSku), Times.Once);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
