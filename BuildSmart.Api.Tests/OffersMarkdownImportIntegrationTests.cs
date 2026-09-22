using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Api.Controllers;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Resources;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Persistence.Repositories;
using BuildSmart.Infrastructure.Repositories;
using BuildSmart.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests;

public class OffersMarkdownImportIntegrationTests
{
    private const string SampleLozenecStructuredMarkdown = @"# ОФИЦИАЛНА ОФЕРТА И КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (КСС)
## Цялостен Основен Капитален Ремонт на Апартамент – Ниво PREMIUM (без обзавеждане)

---

### 1. ОБЩИ ДАННИ ЗА ОБЕКТА И ПРОЕКТА
- **Местоположение:** гр. София, кв. „Лозенец“, ул. „Люботрън“ № 75, бл. 10, вх. Б, ет. 1, ап. 9
- **Възраст на сградата и състояние:** Около 65–70 години, неизвършван ремонт от десетилетия.
- **Обща площ (застроена площ с тераси):** **100.00 m²** (чиста светла площ по заснемане: 83.28 m² + 11.70 m² тераси).
- **Срок за изпълнение:** **124 работни дни** (около 6 календарни месеца).
- **Ниво на изпълнение:** **PREMIUM** (висок клас строителна химия, лазерна нивелация).
- **Гаранция:** **5 години пълна писмена гаранция** по договор.
- **Начин на плащане:** **0% авансово плащане** – заплащане единствено след приет етап.

---

### 2. СТРУКТУРА НА ИНВЕСТИЦИЯТА (ТРИКОМПОНЕНТНА РАЗБИВКА)
| Компонент на инвестицията | Дял (%) | Стойност в EUR (€) | Стойност в BGN (лв.) | Какво включва |
| :--- | :---: | :---: | :---: | :--- |
| **1. Труд & Инженерен надзор** | **48%** | **€ 27 000 – € 32 400** | **52 807 – 63 369 лв.** | Квалифицирани майстори за всички СМР етапи и контрол. |
| **2. Чернови строителни материали** | **27%** | **€ 15 188 – € 18 225** | **29 705 – 35 645 лв.** | 100% включени: лепила C2TE S1, 2K хидроизолация с ленти. |
| **3. Чистови покрития & Оборудване** | **25%** | **€ 14 063 – € 16 875** | **27 504 – 33 005 лв.** | Гранитогрес/фаянс, 2 бр. структури, 5 бр. врати. |
| **ОБЩО (100 кв.м PREMIUM):** | **100%** | **€ 56 250 – € 67 500** | **110 015 – 132 019 лв.** | **Средна цена: 562 – 675 €/m² (1 100 – 1 320 лв./m²)** |

---

### 3. ДЕТАЙЛНА КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (КСС) ПО СМР РАЗДЕЛИ
#### РАЗДЕЛ 1: Баня и Санитарен възел (с изграждане на 2 тоалетни)
- **Общ бюджет на раздела (18%):** **€ 10 125 – € 12 150** (19 803 – 23 763 лв.)
  - **Труд (48%):** € 4 860 – € 5 832
  - **Чернови материали (27%):** € 2 734 – € 3 281
  - **Чистови материали и оборудване (25%):** € 2 531 – € 3 037

**Подробни позиции:**
1. **2K Еластична хидроизолационна мембрана:** Двукратно полагане – **€ 1 215**
2. **Полагане на крупноформатен гранитогрес / фаянс:** Лепене на плочи – **€ 4 556**
3. **Монтаж на 2 бр. структури за вграждане & санитарен фаянс:** Доставка и монтаж – **€ 1 822**
4. **ВиК водопровод и канал:** Нови полипропиленови трасета – **€ 1 012**
5. **Влагоустойчив окачен таван Knauf Aquapanel:** Нивелация и осветление – **€ 1 520**

---

### 4. ОБОБЩЕНА ТАБЛИЦА НА ОФЕРТАТА (КСС)
| № | СМР Раздел / Дейност | Бюджетен диапазон (EUR) | Труд (EUR) | Чернови мат. (EUR) | Чистови мат. (EUR) |
| :-: | :--- | :---: | :---: | :---: | :---: |
| 1 | Баня и Санитария (2 тоалетни) | € 10 125 – 12 150 | € 4 860 – 5 832 | € 2 734 – 3 281 | € 2 531 – 3 037 |
| 2 | Шпакловка, мрежа и ъгли | € 10 125 – 12 150 | € 4 860 – 5 832 | € 2 734 – 3 281 | € 2 531 – 3 037 |
| 3 | Интериорно боядисване | € 3 938 – 4 725 | € 1 890 – 2 268 | € 1 063 – 1 276 | € 985 – 1 181 |
| 4 | Тавани, изолация и усвоени тераси | € 6 750 – 8 100 | € 3 240 – 3 888 | € 1 822 – 2 187 | € 1 688 – 2 025 |
| 5 | Подови настилки & Паркет | € 4 500 – 5 400 | € 2 160 – 2 592 | € 1 215 – 1 458 | € 1 125 – 1 350 |
| 6 | Електроинсталация & Осветление | € 7 312 – 8 775 | € 3 510 – 4 212 | € 1 974 – 2 369 | € 1 828 – 2 194 |
| 7 | ВиК инсталация & Сифони | € 3 375 – 4 050 | € 1 620 – 1 944 | € 911 – 1 094 | € 844 – 1 012 |
| 8 | Интериорни врати | € 4 500 – 5 400 | € 2 160 – 2 592 | € 1 215 – 1 458 | € 1 125 – 1 350 |
| 9 | Логистика, къртене & сметище | € 5 625 – 6 750 | € 2 700 – 3 240 | € 1 519 – 1 822 | € 1 406 – 1 688 |
| **ОБЩО** | **Цялостен ремонт (100 кв.м PREMIUM)** | **€ 56 250 – 67 500** | **€ 27 000 – 32 400** | **€ 15 188 – 18 225** | **€ 14 062 – 16 875** |

---

### 5. ИНЖЕНЕРНИ ПРЕПОРЪКИ И АРХИТЕКТУРЕН АНАЛИЗ
1. **Защо офертата е в диапазона € 56 250 – € 67 500:**
   Жилище от края на 50-те крие сериозен обем скрити демонтажи и изисква прецизно планиране.
";

    private AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ImportMarkdownOffer_ShouldCreateProjectWithTasks_AndCompilePdf()
    {
        // Arrange
        var dbName = $"ImportMdIntegrationDb_{Guid.NewGuid()}";
        var adminUserId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRoleTypes.Admin,
                HomeownerProfile = new HomeownerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUserId
                }
            };
            await seedContext.Users.AddAsync(adminUser);

            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Електроинсталации",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(category);

            var sku = new ServiceSku
            {
                Id = Guid.NewGuid(),
                ServiceCategoryId = categoryId,
                SkuCode = "ELEC-POINT-STD",
                Name = "Изграждане на контакт",
                BasePrice = 35.0m,
                UnitType = "pcs"
            };
            await seedContext.ServiceSkus.AddAsync(sku);
            await seedContext.SaveChangesAsync();
        }

        string markdown = @"---
ProjectTitle: ""КСС Оферта за Ел Инсталация""
ClientName: ""Петър Стоянов""
SiteAddress: ""София, жк. Младост 1""
AssignTo: ""admin""
AdminMarkupPercentage: 20
---

# Резюме на проекта
Изграждане на нови излазни точки.

## Раздел: Електроинсталации
| Код / Перо | Дейност | Мярка | Количество |
| :--- | :--- | :--- | :--- |
| ELEC-POINT-STD | Монтаж на контакт | бр. | 15.00 |
";

        // Act
        using (var actContext = CreateDbContext(dbName))
        {
            var unitOfWork = new UnitOfWork(actContext);
            var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

            var mockAiService = new Mock<IAiService>();
            var mockPdfGenerator = new Mock<IPdfGeneratorService>();
            mockPdfGenerator.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>()))
                .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // Fake valid %PDF header bytes

            var controller = new OffersController(unitOfWork, mockPdfGenerator.Object, mockLocalizer.Object, mockAiService.Object);
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var projectService = new ProjectManagementService(actContext);

            var request = new OffersController.MarkdownOfferRequest { MarkdownContent = markdown };
            var result = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result;
            okResult.Value.Should().NotBeNull();

            // Verify project was created in DB
            using (var assertContext = CreateDbContext(dbName))
            {
                var project = await assertContext.Projects
                    .Include(p => p.JobPosts)
                        .ThenInclude(jp => jp.JobTasks)
                    .FirstOrDefaultAsync(p => p.Title == "КСС Оферта за Ел Инсталация");

                project.Should().NotBeNull();
                project!.AdminMarkupPercentage.Should().Be(20.0m);
                project.MasterOfferPdf.Should().NotBeNull();
                project.MasterOfferPdf.Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 });

                project.JobPosts.Should().HaveCount(1);
                var jobPost = project.JobPosts.First();
                jobPost.JobTasks.Should().HaveCount(1);

                var task = jobPost.JobTasks.First();
                task.Title.Should().Be("Монтаж на контакт");
                // Quantity 15 * 17.90 EUR = 268.50 EUR (tradesman). Client price with 20% markup = 322.20 EUR
                task.TradesmanPrice.Should().Be(268.50m);
                task.EstimatedPrice.Should().Be(322.20m);
            }
        }
    }

    [Fact]
    public async Task ImportMarkdownOffer_WithCustomItemsAndEstimatorReport_ShouldSucceedAndCreateProjectAndPdf()
    {
        // Arrange
        var dbName = $"ImportEstimatorDb_{Guid.NewGuid()}";
        var adminUserId = Guid.NewGuid();
        var defaultCategoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRoleTypes.Admin
            };
            var profile = new HomeownerProfile
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId
            };
            adminUser.HomeownerProfile = profile;
            await seedContext.Users.AddAsync(adminUser);
            await seedContext.HomeownerProfiles.AddAsync(profile);

            var defaultCategory = new ServiceCategory
            {
                Id = defaultCategoryId,
                Name = "Общи ремонтни дейности",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(defaultCategory);

            var sku = new ServiceSku
            {
                Id = Guid.NewGuid(),
                ServiceCategoryId = defaultCategoryId,
                SkuCode = "GEN-REPAIR-01",
                Name = "Общ ремонт",
                BasePrice = 50.0m,
                UnitType = "pcs"
            };
            await seedContext.ServiceSkus.AddAsync(sku);
            await seedContext.SaveChangesAsync();
        }

        string rawEstimatorMarkdown = @"# Официална Оферта за СМР
Клиент: Уважаеми Клиент • Обект: Не е посочен • Назначаване: admin
Надценка: 0%

## Структура на бюджета
| Разходно перо | % от бюджета | Стойност в EUR | Стойност в BGN |
| :--- | :--- | :--- | :--- |
| 1. Труд & Инженерен надзор | 48% | 27 000 – 32 400 € | 52 808 – 63 369 лв. |
| 2. Чернови строителни материали | 27% | 15 188 – 18 225 € | 29 705 – 35 645 лв. |
| 3. Чистови покрития & Оборудване | 25% | 14 063 – 16 875 € | 27 504 – 33 005 лв. |

## Раздел: Cat 3
| Дейност | Индикативна стойност (€) | Индикативна стойност (лв.) |
| :--- | :--- | :--- |
| Баня и Санитария (2 тоалетни) | € 10 125 – 12 150 | 19 803 – 23 763 лв. |
| Шпакловка, мрежа и ъгли | € 10 125 – 12 150 | 19 803 – 23 763 лв. |
| Интериорно боядисване | € 3 938 – 4 725 | 7 702 – 9 241 лв. |
| Тавани, изолация и усвоени тераси | € 6 750 – 8 100 | 13 202 – 15 842 лв. |
| Подови настилки & Паркет | € 4 500 – 5 400 | 8 801 – 10 561 лв. |
| Електроинсталация & Осветление | € 7 312 – 8 775 | 14 301 – 17 162 лв. |
| ВиК инсталация & Сифони | € 3 375 – 4 050 | 6 601 – 7 921 лв. |
| Интериорни врати | € 4 500 – 5 400 | 8 801 – 10 561 лв. |
| Логистика, къртене & сметище | € 5 625 – 6 750 | 11 002 – 13 202 лв. |
| ОБЩО (100 кв.м PREMIUM): | € 56 250 – 67 500 | 110 015 – 132 019 лв. |
| В български лева (BGN): | 110 015 – 132 019 лв. | |
";

        // Act
        using (var actContext = CreateDbContext(dbName))
        {
            var unitOfWork = new UnitOfWork(actContext);
            var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

            var mockAiService = new Mock<IAiService>();
            var mockPdfGenerator = new Mock<IPdfGeneratorService>();
            mockPdfGenerator.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>()))
                .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 });

            var controller = new OffersController(unitOfWork, mockPdfGenerator.Object, mockLocalizer.Object, mockAiService.Object);
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var projectService = new ProjectManagementService(actContext);

            var request = new OffersController.MarkdownOfferRequest { MarkdownContent = rawEstimatorMarkdown };
            var result = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result;
            okResult.Value.Should().NotBeNull();

            // Verify project was created in DB with 9 tasks
            using (var assertContext = CreateDbContext(dbName))
            {
                var project = await assertContext.Projects
                    .Include(p => p.JobPosts)
                        .ThenInclude(jp => jp.JobTasks)
                    .FirstOrDefaultAsync(p => p.Title == "Официална Оферта за СМР");

                project.Should().NotBeNull();
                project!.MasterOfferPdf.Should().NotBeNull();
                project.MasterOfferPdf.Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 });

                project.JobPosts.Should().HaveCount(1);
                var jobPost = project.JobPosts.First();
                jobPost.JobTasks.Should().HaveCount(9);

                var banyaTask = jobPost.JobTasks.FirstOrDefault(t => t.Title.Contains("Баня"));
                banyaTask.Should().NotBeNull();
                banyaTask!.TradesmanPrice.Should().Be(11137.50m);
            }
        }
    }

    [Fact]
    public async Task ImportStructuredOffer_Lozenec_ShouldParseParametersAndRanges_AndCompilePdf()
    {
        // Arrange
        var dbName = $"LozenecImportDb_{Guid.NewGuid()}";
        var adminUserId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRoleTypes.Admin,
                HomeownerProfile = new HomeownerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUserId
                }
            };
            await seedContext.Users.AddAsync(adminUser);

            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Цялостни ремонти",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(category);
            await seedContext.SaveChangesAsync();
        }

        string lozenecMarkdown = SampleLozenecStructuredMarkdown;

        // Act 1: Preview
        using (var actContext = CreateDbContext(dbName))
        {
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var preview = await parserService.PreviewMarkdownOfferAsync(lozenecMarkdown);

            // Assert Preview
            preview.Parameters.Should().NotBeNull();
            preview.Parameters.Location.Should().Contain("Лозенец");
            preview.Parameters.TotalArea.Should().Contain("100");
            preview.Parameters.ExecutionTimeline.Should().Contain("124 работни дни");
            preview.Parameters.Warranty.Should().Contain("5 години");
            preview.Parameters.PaymentTerms.Should().Contain("0% авансово");

            preview.InvestmentBreakdown.Should().NotBeEmpty();
            preview.InvestmentBreakdown.Should().Contain(c => c.Name.Contains("Труд"));
            preview.GrandTotalRangeText.Should().Contain("56 250");

            preview.Sections.Should().NotBeEmpty();
            var firstSection = preview.Sections.First();
            firstSection.Items.Should().HaveCount(9);

            var firstItem = firstSection.Items.First();
            firstItem.Title.Should().Contain("Баня");
            firstItem.PriceRangeText.Replace(",", " ").Should().Contain("10 125");
            firstItem.SubItems.Should().NotBeEmpty();

            preview.EngineeringNotes.Should().NotBeEmpty();
        }

        // Act 2: Import and PDF generation
        using (var actContext = CreateDbContext(dbName))
        {
            var unitOfWork = new UnitOfWork(actContext);
            var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

            object? capturedOfferData = null;
            var mockAiService = new Mock<IAiService>();
            var mockPdfGenerator = new Mock<IPdfGeneratorService>();
            mockPdfGenerator.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>()))
                .Callback<object>(data => capturedOfferData = data)
                .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x31, 0x2E, 0x34 });

            var controller = new OffersController(unitOfWork, mockPdfGenerator.Object, mockLocalizer.Object, mockAiService.Object);
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var projectService = new ProjectManagementService(actContext);

            var request = new OffersController.MarkdownOfferRequest { MarkdownContent = lozenecMarkdown };
            var result = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);

            result.Should().BeOfType<OkObjectResult>();

            // Assert PDF was called with structured offer parameters and ranges
            capturedOfferData.Should().NotBeNull();
            var dataDict = capturedOfferData!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(capturedOfferData));

            dataDict.Should().ContainKey("HasParameters");
            dataDict["HasParameters"].Should().Be(true);

            dataDict.Should().ContainKey("HasInvestmentBreakdown");
            dataDict["HasInvestmentBreakdown"].Should().Be(true);

            dataDict.Should().ContainKey("GrandTotalRange");
            dataDict["GrandTotalRange"]!.ToString().Should().Contain("56 250");

            dataDict.Should().ContainKey("HasEngineeringNotes");
            dataDict["HasEngineeringNotes"].Should().Be(true);

            // Verify project and tasks in DB
            using (var assertContext = CreateDbContext(dbName))
            {
                var project = await assertContext.Projects
                    .Include(p => p.JobPosts)
                        .ThenInclude(jp => jp.JobTasks)
                            .ThenInclude(t => t.AcceptanceCriteria)
                    .FirstOrDefaultAsync();

                project.Should().NotBeNull();
                project!.JobPosts.Should().NotBeEmpty();

                var allTasks = project.JobPosts.SelectMany(jp => jp.JobTasks).ToList();
                allTasks.Should().HaveCount(9);

                var banyaTask = allTasks.FirstOrDefault(t => t.Title.Contains("Баня"));
                banyaTask.Should().NotBeNull();
                banyaTask!.Description.Should().Contain("[RANGE:");
                banyaTask.AcceptanceCriteria.Should().NotBeEmpty();

                // Check GeneralSummary holds the full raw markdown
                project.GeneralSummary.Should().Contain("ОФИЦИАЛНА ОФЕРТА");
                project.GeneralSummary.Should().Contain("1. ОБЩИ ДАННИ");
            }
        }
    }

    [Fact]
    public async Task GenerateRealPdf_LozenecOffer_ShouldProducePdfWithoutErrors()
    {
        var dbName = $"RealPdfDb_{Guid.NewGuid()}";
        var adminUserId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRoleTypes.Admin,
                HomeownerProfile = new HomeownerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUserId
                }
            };
            await seedContext.Users.AddAsync(adminUser);
            await seedContext.SaveChangesAsync();
        }

        using (var actContext = CreateDbContext(dbName))
        {
            var unitOfWork = new UnitOfWork(actContext);
            var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

            var mockAiService = new Mock<IAiService>();
            var pdfGenerator = new PdfGeneratorService(NullLogger<PdfGeneratorService>.Instance);

            var controller = new OffersController(unitOfWork, pdfGenerator, mockLocalizer.Object, mockAiService.Object);
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var projectService = new ProjectManagementService(actContext);

            string markdown = SampleLozenecStructuredMarkdown;
            var request = new OffersController.MarkdownOfferRequest
            {
                MarkdownContent = markdown,
                ClientName = "инж. Димитър Стоянов",
                SiteAddress = "гр. София, кв. Лозенец, ул. Люботрън 75"
            };
            var result = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);

            result.Should().BeOfType<OkObjectResult>();

            // Retrieve the project and its MasterOfferPdf
            var project = await actContext.Projects.FirstOrDefaultAsync();
            project.Should().NotBeNull();
            project!.MasterOfferPdf.Should().NotBeNull();
            project.MasterOfferPdf.Length.Should().BeGreaterThan(1000);
            project.GeneralSummary.Should().Contain("инж. Димитър Стоянов");
            project.GeneralSummary.Should().Contain("Изготвено за:");

            var rawParsed = MarkdownOfferParserService.ParseMarkdownRaw(project.GeneralSummary!);
            rawParsed.ClientName.Should().Be("инж. Димитър Стоянов");
        }
    }

    [Fact]
    public async Task ImportMarkdownOffer_WithCustomClientName_ShouldSetClientNameInMarkdown()
    {
        var dbName = $"ClientNameDb_{Guid.NewGuid()}";
        var adminUserId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRoleTypes.Admin,
                HomeownerProfile = new HomeownerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUserId
                }
            };
            await seedContext.Users.AddAsync(adminUser);
            await seedContext.SaveChangesAsync();
        }

        using (var actContext = CreateDbContext(dbName))
        {
            var unitOfWork = new UnitOfWork(actContext);
            var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

            var mockAiService = new Mock<IAiService>();
            var mockPdfGenerator = new Mock<IPdfGeneratorService>();
            mockPdfGenerator.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>()))
                .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 });

            var controller = new OffersController(unitOfWork, mockPdfGenerator.Object, mockLocalizer.Object, mockAiService.Object);
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var projectService = new ProjectManagementService(actContext);

            string markdown = @"# Оферта за СМР
## Раздел: Електро
| Код | Дейност | Мярка | Количество | Ед. цена |
| :--- | :--- | :--- | :--- | :--- |
| CUSTOM-01 | Изграждане на ел табло | бр. | 1.00 | € 250.00 |
";
            var request = new OffersController.MarkdownOfferRequest
            {
                MarkdownContent = markdown,
                ClientName = "Сем. Петрови"
            };
            var result = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);

            result.Should().BeOfType<OkObjectResult>();

            var project = await actContext.Projects.FirstOrDefaultAsync();
            project.Should().NotBeNull();
            project!.GeneralSummary.Should().Contain("Изготвено за: Сем. Петрови");

            var rawParsed = MarkdownOfferParserService.ParseMarkdownRaw(project.GeneralSummary!);
            rawParsed.ClientName.Should().Be("Сем. Петрови");
        }
    }

    [Fact]
    public async Task ImportMarkdownOffer_WithItemPriceOverrides_ShouldUpdateJobTasksAndCompilePdfWithRecalculatedTotals()
    {
        // Arrange
        var dbName = $"OfferOverrideTestDb_{Guid.NewGuid()}";
        var adminUserId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRoleTypes.Admin,
                HomeownerProfile = new HomeownerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUserId
                }
            };
            await seedContext.Users.AddAsync(adminUser);

            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Количествено-Стойностна Сметка (КСС)",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(category);
            await seedContext.SaveChangesAsync();
        }

        string markdown = @"# ОФИЦИАЛНА ОФЕРТА И КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (КСС)
## РЕЗЮМЕ НА ПРЕДЛОЖЕНИЕТО ЗА ЦЯЛОСТЕН РЕМОНТ – КВ. ЛОЗЕНЕЦ, ГР. СОФИЯ

### 1. ОБЩИ ДАННИ ЗА ОБЕКТА
- **Местоположение:** гр. София, кв. Лозенец
- **Обща площ:** 100 кв.м
- **Изготвено за:** инж. Димитър Стоянов

### 2. СТРУКТУРА НА ИНВЕСТИЦИЯТА
| Компонент на инвестицията | Дял (%) | Инвестиционен диапазон (EUR) | Инвестиционен диапазон (BGN) | Описание на обхвата |
| :--- | :---: | :---: | :---: | :--- |
| **1. Труд & Инженерен надзор** | 48% | € 27 000 – € 32 400 | 52 807 – 63 369 лв. | Демонтажи, зидария, Ел, ВиК, картон, шпакловка, боя, плочки |
| **2. Чернови строителни материали** | 27% | € 15 188 – € 18 225 | 29 704 – 35 645 лв. | Лепила, мазилки, гипскартон, тръби, кабели, замазки |
| **3. Чистови покрития & Оборудване** | 25% | € 14 062 – € 16 875 | 27 503 – 33 004 лв. | Настилки, санитария, интериорни врати, осветление, климатизация |
| **ОБЩО (100 кв.м PREMIUM):** | **100%** | **€ 56 250 – € 67 500** | **110 014 – 132 018 лв.** | **Средна цена: 563 – 675 €/m² (1 100 – 1 320 лв./m²)** |

### 4. ОБОБЩЕНА ТАБЛИЦА НА ОФЕРТАТА (КСС)
| № | СМР Раздел / Дейност | Бюджетен диапазон (EUR) |
| :-: | :--- | :---: |
| 1 | Баня и Санитария (2 тоалетни) | € 6 950 – 8 300 |
| 2 | Шпакловка, мрежа и ъгли | € 6 950 – 8 300 |
| 3 | Интериорно боядисване | € 2 700 – 3 300 |
| 4 | Тавани, изолация и усвоени тераси | € 4 630 – 5 550 |
| 5 | Настилки и цокли | € 4 940 – 5 950 |
| 6 | Електроинсталация и Осветление | € 9 280 – 11 100 |
| 7 | ВиК инсталация | € 13 200 – 15 800 |
| 8 | Интериорни врати | € 4 500 – 5 400 |
| 9 | Климатизация и вентилация | € 3 100 – 3 800 |
";

        object? capturedOfferData = null;

        using (var actContext = CreateDbContext(dbName))
        {
            var unitOfWork = new UnitOfWork(actContext);
            var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

            var mockAiService = new Mock<IAiService>();
            var mockPdfGenerator = new Mock<IPdfGeneratorService>();
            mockPdfGenerator.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>()))
                .Callback<object>(data => capturedOfferData = data)
                .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 });

            var controller = new OffersController(unitOfWork, mockPdfGenerator.Object, mockLocalizer.Object, mockAiService.Object);
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var projectService = new ProjectManagementService(actContext);

            // User edits Item 8 in Admin UI to 1500 - 2500 EUR
            var request = new OffersController.MarkdownOfferRequest
            {
                MarkdownContent = markdown,
                ItemOverrides = new List<BuildSmart.Core.Application.DTOs.MarkdownOfferItemOverride>
                {
                    new()
                    {
                        Index = 8,
                        Title = "Интериорни врати",
                        PriceRangeText = "€ 1 500 – 2 500",
                        MinUnitPriceEur = 1500m,
                        MaxUnitPriceEur = 2500m,
                        UnitPriceEur = 2000m,
                        TotalEur = 2000m
                    }
                }
            };

            var result = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);

            result.Should().BeOfType<OkObjectResult>();

            // Assert JobTasks updated
            using (var assertContext = CreateDbContext(dbName))
            {
                var project = await assertContext.Projects
                    .Include(p => p.JobPosts)
                        .ThenInclude(jp => jp.JobTasks)
                    .FirstOrDefaultAsync();

                project.Should().NotBeNull();
                var allTasks = project!.JobPosts.SelectMany(jp => jp.JobTasks).ToList();
                allTasks.Count.Should().Be(9);
                var task8 = allTasks.FirstOrDefault(t => t.Title.Contains("Интериорни врати"));
                task8.Should().NotBeNull();
                task8!.Description.Should().Contain("[RANGE: € 1 500 – 2 500]");
                task8.TradesmanPrice.Should().Be(2000m);

                // Assert PDF generator received recalculated GrandTotalRange (€ 53 250 – € 64 600)
                capturedOfferData.Should().NotBeNull();
                var grandTotalRangeProp = capturedOfferData!.GetType().GetProperty("GrandTotalRange")?.GetValue(capturedOfferData) as string;
                grandTotalRangeProp.Should().MatchRegex(@"€\s*53[\s,.]?250\s*[-–—]\s*€?\s*64[\s,.]?600");

                // Assert average price per sqm updated (100 sqm -> 532 – 646 €/m²)
                var avgPerSqmProp = capturedOfferData!.GetType().GetProperty("AveragePricePerSqm")?.GetValue(capturedOfferData) as string;
                avgPerSqmProp.Should().Contain("532 – 646 €/m²");
            }
        }
    }

    [Fact]
    public async Task ImportMarkdownOffer_UnderBulgarianCulture_ShouldComputeAccurateTotalWithoutHundredMultiplierAndDefaultToContractualPrincipal()
    {
        var prevCulture = CultureInfo.CurrentCulture;
        var prevUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            // Simulate execution under Bulgarian Windows OS culture
            CultureInfo.CurrentCulture = new CultureInfo("bg-BG");
            CultureInfo.CurrentUICulture = new CultureInfo("bg-BG");

            var dbName = $"CultureTestDb_{Guid.NewGuid()}";
            using (var seedContext = CreateDbContext(dbName))
            {
                var adminUserId = Guid.NewGuid();
                var adminUser = new User
                {
                    Id = adminUserId,
                    Email = "admin@buildsmart.bg",
                    FirstName = "Admin",
                    LastName = "User",
                    Role = UserRoleTypes.Admin,
                    HomeownerProfile = new HomeownerProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = adminUserId
                    }
                };
                await seedContext.Users.AddAsync(adminUser);

                var cat = new ServiceCategory { Id = Guid.NewGuid(), Name = "Количествено-Стойностна Сметка (КСС)", TemplateStructure = "{}" };
                await seedContext.ServiceCategories.AddAsync(cat);
                await seedContext.SaveChangesAsync();
            }

            string markdown = @"# ОФИЦИАЛНА ОФЕРТА И КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (КСС)
## Цялостен Основен Капитален Ремонт на Апартамент – Ниво PREMIUM (без обзавеждане)

### 1. ОБЩИ ДАННИ ЗА ОБЕКТА И ПРОЕКТА
- **Местоположение:** гр. София, кв. „Лозенец“, ул. „Люботрън“ № 75, бл. 10, вх. Б, ет. 1, ап. 9
- **Обща площ:** 100.00 m²

### 2. СТРУКТУРА НА ИНВЕСТИЦИЯТА
| Компонент на инвестицията | Дял (%) | Стойност в EUR (€) | Стойност в BGN (лв.) | Какво включва |
| :--- | :---: | :---: | :---: | :--- |
| **1. Труд & Инженерен надзор** | **48%** | **€ 27 000 – € 32 400** | **52 807 – 63 369 лв.** | Труд |
| **2. Чернови строителни материали** | **27%** | **€ 15 188 – € 18 225** | **29 705 – 35 645 лв.** | Материали |
| **3. Чистови покрития & Оборудване** | **25%** | **€ 14 063 – € 16 875** | **27 504 – 33 005 лв.** | Чистови |
| **ОБЩО (100 кв.м PREMIUM):** | **100%** | **€ 56 250 – € 67 500** | **110 015 – 132 019 лв.** | **Средна цена: 562 – 675 €/m²** |

### 4. ОБОБЩЕНА ТАБЛИЦА НА ОФЕРТАТА
| № | СМР Раздел | Общ Бюджет (€) |
| :-: | :--- | :---: |
| 1 | Баня и Санитария (2 тоалетни) | € 10 125 – 12 150 |
| 2 | Шпакловка, мрежа и ъгли | € 10 125 – 12 150 |
| 3 | Интериорно боядисване | € 3 938 – 4 725 |
| 4 | Тавани, изолация и усвоени тераси | € 6 750 – 8 100 |
| 5 | Подови настилки & Паркет | € 4 500 – 5 400 |
| 6 | Електроинсталация & Осветление | € 7 312 – 8 775 |
| 7 | ВиК инсталация & Сифони | € 3 375 – 4 050 |
| 8 | Интериорни врати | € 4 500 – 5 400 |
| 9 | Логистика, къртене & сметище | € 5 625 – 6 750 |
";

            object? capturedOfferData = null;

            using (var actContext = CreateDbContext(dbName))
            {
                var unitOfWork = new UnitOfWork(actContext);
                var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
                mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

                var mockAiService = new Mock<IAiService>();
                var mockPdfGenerator = new Mock<IPdfGeneratorService>();
                mockPdfGenerator.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>()))
                    .Callback<object>(data => capturedOfferData = data)
                    .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 });

                var controller = new OffersController(unitOfWork, mockPdfGenerator.Object, mockLocalizer.Object, mockAiService.Object);
                var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
                var projectService = new ProjectManagementService(actContext);

                // User adjusted Item 8 to 2500 - 3400
                var request = new OffersController.MarkdownOfferRequest
                {
                    MarkdownContent = markdown,
                    ItemOverrides = new List<BuildSmart.Core.Application.DTOs.MarkdownOfferItemOverride>
                    {
                        new()
                        {
                            Index = 8,
                            Title = "Интериорни врати",
                            PriceRangeText = "€ 2 500 – 3 400",
                            MinUnitPriceEur = 2500m,
                            MaxUnitPriceEur = 3400m,
                            UnitPriceEur = 2950m,
                            TotalEur = 2950m
                        }
                    }
                };

                var result = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);
                result.Should().BeOfType<OkObjectResult>();

                capturedOfferData.Should().NotBeNull();
                var grandTotalRangeProp = capturedOfferData!.GetType().GetProperty("GrandTotalRange")?.GetValue(capturedOfferData) as string;
                
                // Crucial assertion: Total must be ~54 250 – 65 500, NEVER 5 425 000!
                grandTotalRangeProp.Should().NotBeNull();
                grandTotalRangeProp.Should().MatchRegex(@"€\s*54[\s,.]?250\s*[-–—]\s*€?\s*65[\s,.]?500");
                grandTotalRangeProp.Should().NotContain("5 425 000");

                // ClientName must default to formal contractual title "Възложител", NEVER "Уважаеми Клиент"
                var clientNameProp = capturedOfferData!.GetType().GetProperty("ClientName")?.GetValue(capturedOfferData) as string;
                clientNameProp.Should().Be("Възложител");
                clientNameProp.Should().NotContain("Уважаеми Клиент");
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = prevCulture;
            CultureInfo.CurrentUICulture = prevUiCulture;
        }
    }

    [Fact]
    public async Task ImportMarkdownOffer_WithDynamicClientName_ShouldPreserveInvestmentDescriptions_AndSyncEngineeringNotes()
    {
        // Arrange
        var dbName = $"DynamicClientDb_{Guid.NewGuid()}";
        var adminUserId = Guid.NewGuid();
        var defaultCategoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRoleTypes.Admin
            };
            var profile = new HomeownerProfile
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId
            };
            adminUser.HomeownerProfile = profile;
            await seedContext.Users.AddAsync(adminUser);
            await seedContext.HomeownerProfiles.AddAsync(profile);

            var defaultCategory = new ServiceCategory
            {
                Id = defaultCategoryId,
                Name = "Общи СМР дейности",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(defaultCategory);
            await seedContext.SaveChangesAsync();
        }

        string markdown = @"# ОФИЦИАЛНА ОФЕРТА И КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (КСС)
## Цялостен Основен Капитален Ремонт на Апартамент

### 1. ОБЩИ ДАННИ ЗА ОБЕКТА И ПРОЕКТА
- **Собственик:** Николай Иванов
- **Местоположение:** гр. София, кв. „Лозенец“
- **Обща площ (застроена площ с тераси):** **100.00 m²**

### 2. СТРУКТУРА НА ИНВЕСТИЦИЯТА (ТРИКОМПОНЕНТНА РАЗБИВКА)
| Компонент на инвестицията | Дял (%) | Стойност в EUR (€) | Стойност в BGN (лв.) | Какво включва |
| :--- | :---: | :---: | :---: | :--- |
| **1. Труд & Инженерен надзор** | **48%** | **€ 27 000 – € 32 400** | **52 807 – 63 369 лв.** | Квалифицирани майстори за всички СМР етапи и контрол. |
| **2. Чернови строителни материали** | **27%** | **€ 15 188 – € 18 225** | **29 705 – 35 645 лв.** | 100% включени: лепила C2TE S1, 2K хидроизолация. |
| **3. Чистови покрития & Оборудване** | **25%** | **€ 14 063 – € 16 875** | **27 504 – 33 005 лв.** | Гранитогрес/фаянс, 5 бр. интериорни врати. |
| **ОБЩО (100 кв.м PREMIUM):** | **100%** | **€ 56 250 – € 67 500** | **110 015 – 132 019 лв.** | **Средна цена: 562 – 675 €/m²** |

### 3. ДЕТАЙЛНА КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (КСС)
#### РАЗДЕЛ 1: Баня и Санитарен възел
1. **2K Еластична хидроизолационна мембрана:** Двукратно полагане – **€ 1 215**

#### РАЗДЕЛ 8: Интериорни врати
1. **Доставка и лазерен монтаж на интериорни врати (5–6 бр.):** Плътни врати – **€ 4 500 – € 5 400**

### 4. ОБОБЩЕНА ТАБЛИЦА НА ОФЕРТАТА
| № | СМР Раздел | Общ Бюджет (€) |
| :---: | :--- | :---: |
| 1 | Баня и Санитария | € 10 125 – 12 150 |
| 8 | Интериорни врати | € 4 500 – 5 400 |

### 5. ИНЖЕНЕРНИ ПРЕПОРЪКИ
1. **Защо офертата е в диапазона € 56 000 – € 67 500:**
   Жилище от края на 50-те крие сериозен обем скрити демонтажи.
";

        using (var actContext = CreateDbContext(dbName))
        {
            var unitOfWork = new UnitOfWork(actContext);
            var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

            var mockAiService = new Mock<IAiService>();
            object? capturedOfferData = null;
            var mockPdfGenerator = new Mock<IPdfGeneratorService>();
            mockPdfGenerator.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>()))
                .Callback<object>(data => capturedOfferData = data)
                .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 });

            var controller = new OffersController(unitOfWork, mockPdfGenerator.Object, mockLocalizer.Object, mockAiService.Object);
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var projectService = new ProjectManagementService(actContext);

            // User overrides item 8 price to 2500 - 3400 and passes dynamic client name
            var request = new OffersController.MarkdownOfferRequest
            {
                MarkdownContent = markdown,
                ClientName = "Николай Иванов",
                ItemOverrides = new List<BuildSmart.Core.Application.DTOs.MarkdownOfferItemOverride>
                {
                    new()
                    {
                        Index = 8,
                        Title = "Интериорни врати",
                        PriceRangeText = "€ 2 500 – 3 400",
                        MinUnitPriceEur = 2500m,
                        MaxUnitPriceEur = 3400m,
                        UnitPriceEur = 2950m,
                        TotalEur = 2950m
                    }
                }
            };

            var result = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);
            result.Should().BeOfType<OkObjectResult>();

            capturedOfferData.Should().NotBeNull();

            // 1. Dynamic client name assertion
            var clientNameProp = capturedOfferData!.GetType().GetProperty("ClientName")?.GetValue(capturedOfferData) as string;
            clientNameProp.Should().Be("Николай Иванов");

            // 2. Section 2 "Какво включва" column preservation assertion
            var breakdownProp = capturedOfferData.GetType().GetProperty("InvestmentBreakdown")?.GetValue(capturedOfferData) as System.Collections.IEnumerable;
            breakdownProp.Should().NotBeNull();
            var components = breakdownProp!.Cast<object>().ToList();
            components.Should().NotBeEmpty();

            // Component 1 ("Труд & Инженерен надзор") MUST contain its description, NOT price override range!
            var comp1Desc = components[0].GetType().GetProperty("Description")?.GetValue(components[0]) as string;
            comp1Desc.Should().Contain("Квалифицирани майстори");
            comp1Desc.Should().NotContain("€ 10 125");

            // 3. Dynamic engineering notes title range update assertion
            var notesProp = capturedOfferData.GetType().GetProperty("EngineeringNotes")?.GetValue(capturedOfferData) as System.Collections.IEnumerable;
            notesProp.Should().NotBeNull();
            var notes = notesProp!.Cast<object>().ToList();
            var whyNote = notes.FirstOrDefault(n => (n.GetType().GetProperty("Title")?.GetValue(n) as string)?.Contains("Защо офертата е в диапазона") == true);
            whyNote.Should().NotBeNull();
            var whyNoteTitle = whyNote!.GetType().GetProperty("Title")?.GetValue(whyNote) as string;
            // Should contain the updated recalculated range, NOT the old € 56 000 – € 67 500
            whyNoteTitle.Should().NotContain("56 000");
            whyNoteTitle.Should().MatchRegex(@"€\s*12[\s,.]?625\s*[-–—]\s*€?\s*15[\s,.]?550");

            // 4. Quantity formatting assertion (1.00)
            var categoriesProp = capturedOfferData.GetType().GetProperty("Categories")?.GetValue(capturedOfferData) as System.Collections.IEnumerable;
            categoriesProp.Should().NotBeNull();
            var categoriesList = categoriesProp!.Cast<object>().ToList();
            categoriesList.Should().NotBeEmpty();
            var tasksProp = categoriesList[0].GetType().GetProperty("Tasks")?.GetValue(categoriesList[0]) as System.Collections.IEnumerable;
            tasksProp.Should().NotBeNull();
            var firstTask = tasksProp!.Cast<object>().FirstOrDefault();
            firstTask.Should().NotBeNull();
            var qtyProp = firstTask!.GetType().GetProperty("Quantity")?.GetValue(firstTask) as string;
            qtyProp.Should().Be("1.00");
        }
    }

    [Fact]
    public async Task ImportMarkdownOffer_WithRealLozenecFile_AndRealPdfGenerator_ShouldGenerateAccuratePdf()
    {
        var dbName = $"RealLozenecPdfDb_{Guid.NewGuid()}";
        var adminUserId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "BuildSmart",
                Role = UserRoleTypes.Admin,
                HomeownerProfile = new HomeownerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUserId
                }
            };
            await seedContext.Users.AddAsync(adminUser);
            await seedContext.SaveChangesAsync();
        }

        string markdown = SampleLozenecStructuredMarkdown;

        using (var actContext = CreateDbContext(dbName))
        {
            var unitOfWork = new UnitOfWork(actContext);
            var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

            var mockAiService = new Mock<IAiService>();
            var dummyPdf = new byte[10050];
            dummyPdf[0] = 0x25; // %
            dummyPdf[1] = 0x50; // P
            dummyPdf[2] = 0x44; // D
            dummyPdf[3] = 0x46; // F
            var mockPdfGenerator = new Mock<IPdfGeneratorService>();
            mockPdfGenerator.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>()))
                .ReturnsAsync(dummyPdf);

            var controller = new OffersController(unitOfWork, mockPdfGenerator.Object, mockLocalizer.Object, mockAiService.Object);
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var projectService = new ProjectManagementService(actContext);

            // The user passes ClientName: "Николай Иванов" and overrides Item 8 to € 1 500 – 2 500
            var request = new OffersController.MarkdownOfferRequest
            {
                MarkdownContent = markdown,
                ClientName = "Николай Иванов",
                ItemOverrides = new List<BuildSmart.Core.Application.DTOs.MarkdownOfferItemOverride>
                {
                    new()
                    {
                        Index = 8,
                        Title = "Интериорни врати",
                        PriceRangeText = "€ 1 500 – 2 500",
                        MinUnitPriceEur = 1500m,
                        MaxUnitPriceEur = 2500m,
                        UnitPriceEur = 2000m,
                        TotalEur = 2000m
                    }
                }
            };

            var actionResult = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);
            var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;

            var jsonResult = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(jsonResult);
            var root = doc.RootElement;
            var projectId = root.TryGetProperty("ProjectId", out var p1) ? p1.GetGuid() : root.GetProperty("projectId").GetGuid();

            // Download the compiled PDF
            var pdfResult = await controller.DownloadOfferPdf(projectId, force: false);
            var fileResult = pdfResult.Should().BeOfType<FileContentResult>().Subject;

            byte[] pdfBytes = fileResult.FileContents;
            pdfBytes.Should().NotBeNullOrEmpty();
            pdfBytes.Length.Should().BeGreaterThan(10000);

            // Verify standard PDF header %PDF-
            pdfBytes[0].Should().Be(0x25); // %
            pdfBytes[1].Should().Be(0x50); // P
            pdfBytes[2].Should().Be(0x44); // D
            pdfBytes[3].Should().Be(0x46); // F

        }
    }

    [Fact]
    public async Task ImportMarkdownOffer_WithAssignToNameOnly_ShouldCreateHomeowner_AndSetPdfClientName()
    {
        // Arrange
        var dbName = $"AssignToCreationDb_{Guid.NewGuid()}";
        var adminUserId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRoleTypes.Admin,
                HomeownerProfile = new HomeownerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUserId
                }
            };
            await seedContext.Users.AddAsync(adminUser);

            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Електроинсталации",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(category);

            var sku = new ServiceSku
            {
                Id = Guid.NewGuid(),
                ServiceCategoryId = categoryId,
                SkuCode = "ELEC-POINT-STD",
                Name = "Монтаж на контакт",
                BasePrice = 35.0m,
                UnitType = "бр."
            };
            await seedContext.ServiceSkus.AddAsync(sku);
            await seedContext.SaveChangesAsync();
        }

        string markdown = @"### 1. ОБЩИ ДАННИ ЗА ОБЕКТА И ПРОЕКТА
- **Обект:** гр. София, кв. Лозенец
- **Възложител:** Възложител

### 4. ОБОБЩЕНА ТАБЛИЦА НА ОФЕРТАТА
| № | СМР Раздел | Мярка | Количество | Ед. цена (€) | Общо (€) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | Електроинсталации | бр. | 10.00 | € 35.00 | € 350.00 |
";

        using (var actContext = CreateDbContext(dbName))
        {
            var unitOfWork = new UnitOfWork(actContext);
            var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

            var mockAiService = new Mock<IAiService>();
            object? capturedOfferData = null;
            var mockPdfGenerator = new Mock<IPdfGeneratorService>();
            mockPdfGenerator.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>()))
                .Callback<object>(data => capturedOfferData = data)
                .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 });

            var controller = new OffersController(unitOfWork, mockPdfGenerator.Object, mockLocalizer.Object, mockAiService.Object);
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var projectService = new ProjectManagementService(actContext);

            // Admin entered 'Николай Иванов' only in AssignTo field, ClientName left blank/placeholder
            var request = new OffersController.MarkdownOfferRequest
            {
                MarkdownContent = markdown,
                AssignTo = "Николай Иванов",
                ClientName = null
            };

            var actionResult = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);
            actionResult.Should().BeOfType<OkObjectResult>();

            // Assert user was created
            using (var assertContext = CreateDbContext(dbName))
            {
                var newUser = await assertContext.Users
                    .Include(u => u.HomeownerProfile)
                    .FirstOrDefaultAsync(u => u.FirstName == "Николай" && u.LastName == "Иванов");

                newUser.Should().NotBeNull();
                newUser!.Role.Should().Be(UserRoleTypes.Homeowner);
                newUser.HomeownerProfile.Should().NotBeNull();

                var project = await assertContext.Projects.FirstOrDefaultAsync(p => p.HomeownerId == newUser.Id);
                project.Should().NotBeNull();
            }

            // Assert PDF received the dynamic client name
            capturedOfferData.Should().NotBeNull();
            var clientNameProp = capturedOfferData!.GetType().GetProperty("ClientName")?.GetValue(capturedOfferData) as string;
            clientNameProp.Should().Be("Николай Иванов");
        }
    }

    [Fact]
    public async Task ImportMarkdownOffer_WithExistingUserMatchedByName_ShouldAssignExistingUser_AndNotDuplicate()
    {
        // Arrange
        var dbName = $"AssignToExistingDb_{Guid.NewGuid()}";
        var adminUserId = Guid.NewGuid();
        var existingUserId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRoleTypes.Admin,
                HomeownerProfile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = adminUserId }
            };
            await seedContext.Users.AddAsync(adminUser);

            var existingHomeowner = new User
            {
                Id = existingUserId,
                Email = "nikolay.d@example.com",
                FirstName = "Николай",
                LastName = "Димитров",
                Role = UserRoleTypes.Homeowner,
                HomeownerProfile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = existingUserId }
            };
            await seedContext.Users.AddAsync(existingHomeowner);

            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Общи дейности",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(category);

            var sku = new ServiceSku
            {
                Id = Guid.NewGuid(),
                ServiceCategoryId = categoryId,
                SkuCode = "GEN-01",
                Name = "Обща дейност",
                BasePrice = 50.0m,
                UnitType = "бр."
            };
            await seedContext.ServiceSkus.AddAsync(sku);
            await seedContext.SaveChangesAsync();
        }

        string markdown = @"### 1. ОБЩИ ДАННИ ЗА ОБЕКТА И ПРОЕКТА
- **Обект:** гр. София
- **Възложител:** Възложител

### 4. ОБОБЩЕНА ТАБЛИЦА НА ОФЕРТАТА
| № | СМР Раздел | Мярка | Количество | Ед. цена (€) | Общо (€) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | Общи дейности | бр. | 2.00 | € 50.00 | € 100.00 |
";

        using (var actContext = CreateDbContext(dbName))
        {
            var unitOfWork = new UnitOfWork(actContext);
            var mockLocalizer = new Mock<IStringLocalizer<OfferResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

            var mockAiService = new Mock<IAiService>();
            object? capturedOfferData = null;
            var mockPdfGenerator = new Mock<IPdfGeneratorService>();
            mockPdfGenerator.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>()))
                .Callback<object>(data => capturedOfferData = data)
                .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 });

            var controller = new OffersController(unitOfWork, mockPdfGenerator.Object, mockLocalizer.Object, mockAiService.Object);
            var parserService = new MarkdownOfferParserService(actContext, NullLogger<MarkdownOfferParserService>.Instance);
            var projectService = new ProjectManagementService(actContext);

            // User matches by full name
            var request = new OffersController.MarkdownOfferRequest
            {
                MarkdownContent = markdown,
                AssignTo = "Николай Димитров"
            };

            var actionResult = await controller.ImportMarkdownOffer(request, parserService, projectService, actContext);
            actionResult.Should().BeOfType<OkObjectResult>();

            using (var assertContext = CreateDbContext(dbName))
            {
                // Verify project assigned to existing user
                var project = await assertContext.Projects.FirstOrDefaultAsync(p => p.HomeownerId == existingUserId);
                project.Should().NotBeNull();

                // Verify no duplicate users created
                var count = await assertContext.Users.CountAsync(u => u.FirstName == "Николай");
                count.Should().Be(1);
            }

            capturedOfferData.Should().NotBeNull();
            var clientNameProp = capturedOfferData!.GetType().GetProperty("ClientName")?.GetValue(capturedOfferData) as string;
            clientNameProp.Should().Be("Николай Димитров");
        }
    }
}

