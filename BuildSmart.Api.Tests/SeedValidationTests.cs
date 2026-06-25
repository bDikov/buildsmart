using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using BuildSmart.Infrastructure.Persistence;

namespace BuildSmart.Api.Tests
{
    public class SeedValidationTests
    {
        [Fact]
        public void SeedFiles_ShouldNotContainReplacementCharacters()
        {
            var seedFiles = new[]
            {
                @"..\..\..\..\BuildSmart.Infrastructure\MarketData_Sofia_Seed.json",
                @"..\..\..\..\BuildSmart.Infrastructure\Electrical_SKUs_Seed.json",
                @"..\..\..\..\BuildSmart.Api\MarketData_Sofia_Seed.json",
                @"..\..\..\..\BuildSmart.Api\Electrical_SKUs_Seed.json"
            };

            foreach (var relativePath in seedFiles)
            {
                var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
                Assert.True(File.Exists(fullPath), $"Seed file does not exist: {fullPath}");

                var content = File.ReadAllText(fullPath);
                Assert.DoesNotContain("\uFFFD", content);
                Assert.DoesNotContain("пїЅ", content);
            }
        }

        [Fact]
        public async Task DatabaseSkus_ShouldNotContainCorruptedCharacters()
        {
            string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connString);

            using var db = new AppDbContext(optionsBuilder.Options);

            var skus = await db.ServiceSkus
                .Include(s => s.Translations)
                .ToListAsync();

            var electricalSkus = skus.Where(s => s.SkuCode.StartsWith("ELEC-")).ToList();
            Assert.NotEmpty(electricalSkus);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Total ELEC SKUs: {electricalSkus.Count}");
            foreach (var s in electricalSkus)
            {
                var bg = s.Translations.FirstOrDefault(t => t.LanguageCode == "bg")?.Name ?? "[NONE]";
                sb.AppendLine($"Code: {s.SkuCode} | Name: {s.Name} | Translation: {bg}");
            }
            File.WriteAllText(@"C:\Users\bonch\.gemini\antigravity\brain\e612d187-d5ab-4bfd-ae03-b4e0f879943b\scratch\db_elec_skus.txt", sb.ToString());

            foreach (var sku in electricalSkus)
            {
                Assert.NotNull(sku.Name);
                Assert.DoesNotContain("\uFFFD", sku.Name);
                Assert.DoesNotContain("??", sku.Name); // Check for double question marks (corruption)
                
                // Let's also verify it's not all question marks or empty spaces
                Assert.True(sku.Name.Trim().Length > 2, $"Sku Name for {sku.SkuCode} is too short: {sku.Name}");
                Assert.False(sku.Name.Contains("    "), $"Sku Name for {sku.SkuCode} contains placeholder question marks: {sku.Name}");

                foreach (var trans in sku.Translations)
                {
                    Assert.NotNull(trans.Name);
                    Assert.DoesNotContain("\uFFFD", trans.Name);
                    Assert.DoesNotContain("??", trans.Name);
                    Assert.False(trans.Name.Contains("    "), $"Translation Name for {sku.SkuCode} contains placeholder question marks: {trans.Name}");
                }
            }
        }
    }
}
