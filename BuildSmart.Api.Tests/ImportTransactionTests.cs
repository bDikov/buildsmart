using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Api.GraphQL;

namespace BuildSmart.Api.Tests
{
    public class ImportTransactionTests
    {
        private async Task<AppDbContext> CreateInMemorySqliteDbContextAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return db;
        }

        [Fact]
        public async Task ImportSpiderNetConfig_WhenFailureOccurs_RollsBackAllChanges()
        {
            // Arrange
            using var db = await CreateInMemorySqliteDbContextAsync();

            // Seed initial clean state
            var category = new ServiceCategory
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Original Category",
                Description = "Original Description",
                Status = BuildSmart.Core.Domain.Enums.CategoryStatus.Active
            };
            db.ServiceCategories.Add(category);
            await db.SaveChangesAsync();

            var mutation = new Mutation();

            // 1. Valid configuration that updates the category and creates a new one
            var validJson = @"{
                ""Categories"": [
                    {
                        ""Id"": ""11111111-1111-1111-1111-111111111111"",
                        ""Name"": ""Original Category Updated"",
                        ""Description"": ""Updated Description"",
                        ""IsGlobal"": true,
                        ""TemplateStructure"": ""{}"",
                        ""Status"": ""ACTIVE""
                    },
                    {
                        ""Id"": ""22222222-2222-2222-2222-222222222222"",
                        ""Name"": ""New Category"",
                        ""Description"": ""New Category Desc"",
                        ""IsGlobal"": false,
                        ""TemplateStructure"": ""{}"",
                        ""Status"": ""ACTIVE""
                    }
                ]
            }";

            // Run successful import
            var successResult = await mutation.ImportSpiderNetConfig(validJson, db);
            Assert.True(successResult.Success);

            // Verify changes are committed
            var categories = await db.ServiceCategories.ToListAsync();
            Assert.Equal(2, categories.Count);
            Assert.Equal("Original Category Updated", categories.First(c => c.Id == category.Id).Name);

            // 2. An invalid configuration that will throw an exception during processing.
            // (Invalid JSON syntax to trigger JSON parsing exception)
            var invalidJson = @"{
                ""Categories"": [
                    {
                        ""Id"": ""11111111-1111-1111-1111-111111111111"",
                        ""Name"": ""This Should Roll Back"",
                        ""Description"": ""Should not exist"",
                        ""IsGlobal"": true,
                        ""TemplateStructure"": ""{}"",
                        ""Status"": ""ACTIVE""
                    }
                ],
                INVALID_JSON_SYNTAX_ERROR
            }";

            // Act
            var failResult = await mutation.ImportSpiderNetConfig(invalidJson, db);

            // Assert
            Assert.False(failResult.Success);
            Assert.NotEmpty(failResult.LogLines);
            Assert.Contains(failResult.LogLines, line => line.Contains("[ERROR]"));

            // Verify that the database reverted to the previous state (Original Category Updated, not "This Should Roll Back")
            var rolledBackCategories = await db.ServiceCategories.ToListAsync();
            Assert.Equal(2, rolledBackCategories.Count);
            Assert.Equal("Original Category Updated", rolledBackCategories.First(c => c.Id == category.Id).Name);
        }
    }
}
