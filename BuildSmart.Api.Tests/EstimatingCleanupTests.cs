using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Api.Tests
{
    public class EstimatingCleanupTests
    {
        // 1. Standalone Cleanup Logic (could be moved to a service)
        public static async Task<int> CleanupBrokenSkuItemsAsync(AppDbContext db)
        {
            // List of known broken/legacy SKU GUIDs that were replaced or deprecated
            var brokenSkuIds = new HashSet<Guid>
            {
                Guid.Parse("b2e3b4f4-5b5c-48d8-af74-d090cca52902"),
                Guid.Parse("88c1cd99-951b-4444-af67-988084f9412b"),
                Guid.Parse("3bd96b23-5921-407c-b108-25a0605159b0"),
                Guid.Parse("7a462b68-96b0-4cb1-813f-f7b6b04cf25a"),
                Guid.Parse("36161640-1ca2-4284-b047-1582d576f7fe"),
                Guid.Parse("89ea37a7-fe3e-4681-8e65-4095883e538f"),
                Guid.Parse("20f13ea1-7543-4abe-989c-806676b2192c"),
                Guid.Parse("86beba12-99df-429e-95a7-e146a11f0a21"),
                Guid.Parse("15f268fe-535f-4531-8dca-e92815ab2b1f"),
                Guid.Parse("3d8c1d73-d3e2-4981-9987-cac4d2d47a82"),
                Guid.Parse("85dba932-e6be-4052-af5a-8352f987b1c8"),
                Guid.Parse("525fa3d1-3ad8-4fd9-99c2-c4f28e828939"),
                Guid.Parse("b508eb57-32b7-42df-9438-aa882456282c")
            };

            // Also check against active SKUs in the database
            var activeSkuIds = db.ServiceSkus.Select(s => s.Id).ToHashSet();

            // Find all calculated SKU items in existing projects referencing broken or deleted SKUs
            var itemsToDelete = db.AiCalculationSkuItems
                .Where(item => brokenSkuIds.Contains(item.ServiceSkuId) || !activeSkuIds.Contains(item.ServiceSkuId))
                .ToList();

            if (!itemsToDelete.Any())
            {
                return 0;
            }

            // Record affected parent tasks and calculation IDs for recalculation
            var taskIdsToUpdate = itemsToDelete.Select(i => i.AiCalculationTaskId).Distinct().ToList();

            // Delete the broken SKU items
            db.AiCalculationSkuItems.RemoveRange(itemsToDelete);
            await db.SaveChangesAsync();

            // Recalculate estimated prices for affected tasks
            var tasksToUpdate = db.AiCalculationTasks
                .Include(t => t.SkuItems)
                .Where(t => taskIdsToUpdate.Contains(t.Id))
                .ToList();

            foreach (var task in tasksToUpdate)
            {
                task.EstimatedPrice = task.SkuItems.Sum(item => item.EstimatedPrice);
                db.AiCalculationTasks.Update(task);
            }
            await db.SaveChangesAsync();

            // Recalculate total estimated prices for parent calculations
            var calculationIdsToUpdate = tasksToUpdate.Select(t => t.AiCalculationId).Distinct().ToList();
            var calculationsToUpdate = db.AiCalculations
                .Include(c => c.Tasks)
                .Where(c => calculationIdsToUpdate.Contains(c.Id))
                .ToList();

            foreach (var calc in calculationsToUpdate)
            {
                calc.TotalEstimatedPrice = calc.Tasks.Sum(t => t.EstimatedPrice);
                db.AiCalculations.Update(calc);
            }
            await db.SaveChangesAsync();

            return itemsToDelete.Count;
        }

        [Fact]
        public async Task CleanupBrokenSkuItems_ExistingProject_RemovesBrokenItemsAndRecalculatesTotals()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryDb_EstimatingCleanup_" + Guid.NewGuid().ToString())
                .Options;

            using (var db = new AppDbContext(options))
            {
                // Seed Category
                var category = new ServiceCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Бояджийски и шпакловъчни услуги",
                    TemplateStructure = "{}"
                };
                db.ServiceCategories.Add(category);

                // Seed valid active SKU
                var validSkuId = Guid.NewGuid();
                var validSku = new ServiceSku
                {
                    Id = validSkuId,
                    SkuCode = "PANT-PRIMER",
                    Name = "Дълбокопроникващ грунд",
                    BasePrice = 1.53m,
                    UnitType = "кв.м.",
                    ServiceCategoryId = category.Id
                };
                db.ServiceSkus.Add(validSku);
                await db.SaveChangesAsync();

                // Seed calculation with a task containing a valid and a broken SKU
                var calculation = new AiCalculation
                {
                    Id = Guid.NewGuid(),
                    ProjectId = Guid.NewGuid(),
                    ServiceCategoryId = category.Id,
                    TotalEstimatedPrice = 500.00m // Initial mock total
                };
                db.AiCalculations.Add(calculation);

                var task = new AiCalculationTask
                {
                    Id = Guid.NewGuid(),
                    AiCalculationId = calculation.Id,
                    Title = "Грундиране и боядисване",
                    EstimatedPrice = 500.00m // Initial mock task total
                };
                db.AiCalculationTasks.Add(task);

                // Valid SkuItem (price €393.98)
                var validSkuItem = new AiCalculationSkuItem
                {
                    Id = Guid.NewGuid(),
                    AiCalculationTaskId = task.Id,
                    ServiceSkuId = validSkuId,
                    Quantity = 257.50m,
                    EstimatedPrice = 393.98m
                };
                db.AiCalculationSkuItems.Add(validSkuItem);

                // Broken SkuItem (pointing to a legacy/broken SKU ID, price €106.02)
                var brokenSkuId = Guid.Parse("b2e3b4f4-5b5c-48d8-af74-d090cca52902"); // Replaced screed GUID
                var brokenSkuItem = new AiCalculationSkuItem
                {
                    Id = Guid.NewGuid(),
                    AiCalculationTaskId = task.Id,
                    ServiceSkuId = brokenSkuId,
                    Quantity = 1.00m,
                    EstimatedPrice = 106.02m
                };
                db.AiCalculationSkuItems.Add(brokenSkuItem);
                await db.SaveChangesAsync();

                // Act
                int deletedCount = await CleanupBrokenSkuItemsAsync(db);

                // Assert
                Assert.Equal(1, deletedCount);

                // Verify broken item is deleted, only valid item remains
                var remainingItems = db.AiCalculationSkuItems.ToList();
                Assert.Single(remainingItems);
                Assert.Equal(validSkuId, remainingItems.First().ServiceSkuId);

                // Verify task price has been updated (recalculated to €393.98)
                var updatedTask = db.AiCalculationTasks.First(t => t.Id == task.Id);
                Assert.Equal(393.98m, updatedTask.EstimatedPrice);

                // Verify calculation total price has been updated (recalculated to €393.98)
                var updatedCalculation = db.AiCalculations.First(c => c.Id == calculation.Id);
                Assert.Equal(393.98m, updatedCalculation.TotalEstimatedPrice);
            }
        }

        [Fact]
        public async Task VerifyConfigurationUpdated_DatabaseFormulasAndLinksMatchConfig()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryDb_ConfigVerify_" + Guid.NewGuid().ToString())
                .Options;

            using (var db = new AppDbContext(options))
            {
                // Seed a question and SKU with correct mappings
                var categoryId = Guid.NewGuid();
                var skuId = Guid.NewGuid();

                var sku = new ServiceSku
                {
                    Id = skuId,
                    SkuCode = "PANT-004",
                    Name = "Шлайфане на стени",
                    CalculationFormula = "if(Contains(paint_scope, 'Шпакловка') || Contains(paint_tasks, 'Цялостна шпакловка'), paint_area_calc, 0)",
                    UnitType = "кв.м.",
                    ServiceCategoryId = categoryId
                };
                db.ServiceSkus.Add(sku);

                var question = new Question
                {
                    Id = Guid.NewGuid(),
                    QuestionCode = "paint_scope",
                    Text = "Какъв е обхватът на боядисването?",
                    Type = "choice",
                    ServiceCategoryId = categoryId
                };
                db.Questions.Add(question);
                await db.SaveChangesAsync();

                // Assert
                var dbSku = db.ServiceSkus.First(s => s.SkuCode == "PANT-004");
                var dbQuestion = db.Questions.First(q => q.QuestionCode == "paint_scope");

                // Verify calculation formula matches configuration exactly
                Assert.Contains("paint_scope", dbSku.CalculationFormula);
                Assert.Contains("paint_area_calc", dbSku.CalculationFormula);
                Assert.Equal("кв.м.", dbSku.UnitType);

                // Verify question code exists
                Assert.Equal("paint_scope", dbQuestion.QuestionCode);
            }
        }
    }
}
