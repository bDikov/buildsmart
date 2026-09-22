using System;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildSmart.Api.Tests;

public class MarkdownOfferParserServiceTests
{
    private AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task PreviewMarkdownOfferAsync_ShouldParseMetadata_AndCalculateCorrectTotalsWithMarkup()
    {
        // Arrange
        var dbName = $"MdOfferTestDb_{Guid.NewGuid()}";
        var categoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Електроинсталации",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(category);

            var sku1 = new ServiceSku
            {
                Id = Guid.NewGuid(),
                ServiceCategoryId = categoryId,
                SkuCode = "ELEC-POINT-STD",
                Name = "Изграждане на контакт",
                BasePrice = 35.0m, // 35 BGN ~= 17.89 EUR
                UnitType = "pcs"
            };
            var sku2 = new ServiceSku
            {
                Id = Guid.NewGuid(),
                ServiceCategoryId = categoryId,
                SkuCode = "ELEC-CABLE-LAY",
                Name = "Полагане на кабел",
                BasePrice = 2.0m, // 2 BGN ~= 1.02 EUR
                UnitType = "m"
            };

            await seedContext.ServiceSkus.AddRangeAsync(sku1, sku2);
            await seedContext.SaveChangesAsync();
        }

        string markdown = @"---
ProjectTitle: ""Ремонт на офис Лозенец""
ClientName: ""Георги Димитров""
ClientEmail: ""georgi@example.com""
SiteAddress: ""гр. София, бул. Черни връх 45""
AssignTo: ""admin""
Currency: ""EUR""
AdminMarkupPercentage: 20
Language: ""bg""
---

# Обхват на проекта
Цялостно окабеляване и монтаж на контакти.

## Раздел: Електроинсталации
| Код / Перо | Дейност / Описание | Мярка | Количество | Ед. цена |
| :--- | :--- | :--- | :--- | :--- |
| ELEC-POINT-STD | Изграждане на контакт | бр. | 10.00 | |
| ELEC-CABLE-LAY | Полагане на кабел | м | 100.00 | |
";

        // Act
        using (var queryContext = CreateDbContext(dbName))
        {
            var service = new MarkdownOfferParserService(queryContext, NullLogger<MarkdownOfferParserService>.Instance);
            var preview = await service.PreviewMarkdownOfferAsync(markdown);

            // Assert
            preview.Should().NotBeNull();
            preview.ProjectTitle.Should().Be("Ремонт на офис Лозенец");
            preview.ClientName.Should().Be("Георги Димитров");
            preview.SiteAddress.Should().Be("гр. София, бул. Черни връх 45");
            preview.AdminMarkupPercentage.Should().Be(20.0m);
            preview.IsValid.Should().BeTrue();
            preview.TotalItemCount.Should().Be(2);
            preview.MatchedSkuCount.Should().Be(2);

            preview.Sections.Should().HaveCount(1);
            var section = preview.Sections[0];
            section.CategoryName.Should().Be("Електроинсталации");
            section.Items.Should().HaveCount(2);

            // Item 1: 35 BGN / 1.95583 = 17.8951... -> 17.90 EUR base. Tradesman total = 179.00. Client with 20% markup = 214.80 EUR
            var item1 = section.Items[0];
            item1.SkuCode.Should().Be("ELEC-POINT-STD");
            item1.Quantity.Should().Be(10.0m);
            item1.Unit.Should().Be("бр.");
            item1.BaseUnitPriceEur.Should().Be(17.90m);
            item1.TotalEur.Should().Be(214.80m);

            // Item 2: 2 BGN / 1.95583 = 1.0225... -> 1.02 EUR base. Tradesman total = 102.00. Client with 20% markup = 122.40 EUR
            var item2 = section.Items[1];
            item2.SkuCode.Should().Be("ELEC-CABLE-LAY");
            item2.Quantity.Should().Be(100.0m);
            item2.Unit.Should().Be("м");
            item2.BaseUnitPriceEur.Should().Be(1.02m);
            item2.TotalEur.Should().Be(122.40m);

            preview.GrandTotalEur.Should().Be(337.20m);
        }
    }

    [Fact]
    public async Task PreviewMarkdownOfferAsync_ShouldFlagUnrecognizedSkuCodes_WithValidationWarnings()
    {
        // Arrange
        var dbName = $"MdOfferTestDb_{Guid.NewGuid()}";

        using (var seedContext = CreateDbContext(dbName))
        {
            var category = new ServiceCategory
            {
                Id = Guid.NewGuid(),
                Name = "Общи дейности",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(category);
            await seedContext.SaveChangesAsync();
        }

        string markdown = @"---
ProjectTitle: ""Тестов Проект""
---

## Раздел: Общи дейности
| Код / Перо | Дейност | Мярка | Количество |
| :--- | :--- | :--- | :--- |
| UNKNOWN-CODE-123 | Несъществуващо СМР перо | м² | 50.00 |
";

        // Act
        using (var queryContext = CreateDbContext(dbName))
        {
            var service = new MarkdownOfferParserService(queryContext, NullLogger<MarkdownOfferParserService>.Instance);
            var preview = await service.PreviewMarkdownOfferAsync(markdown);

            // Assert
            preview.Should().NotBeNull();
            preview.IsValid.Should().BeFalse();
            preview.MatchedSkuCount.Should().Be(0);
            preview.TotalItemCount.Should().Be(1);
            preview.ValidationMessages.Should().Contain(msg => msg.Contains("UNKNOWN-CODE-123"));
        }
    }

    [Fact]
    public async Task ParseMarkdownOfferAsync_ShouldSupportCustomUnitPrices_WhenSpecifiedInMarkdown()
    {
        // Arrange
        var dbName = $"MdOfferTestDb_{Guid.NewGuid()}";
        var categoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Сухо строителство",
                TemplateStructure = "{}"
            };
            var sku = new ServiceSku
            {
                Id = Guid.NewGuid(),
                ServiceCategoryId = categoryId,
                SkuCode = "DRYW-CEILING-STD",
                Name = "Окачен таван",
                BasePrice = 45.0m, // standard is ~23 EUR
                UnitType = "sqm"
            };
            await seedContext.ServiceCategories.AddAsync(category);
            await seedContext.ServiceSkus.AddAsync(sku);
            await seedContext.SaveChangesAsync();
        }

        string markdown = @"---
ProjectTitle: ""Окачени тавани по поръчка""
AdminMarkupPercentage: 20
---

## Раздел: Сухо строителство
| Код / Перо | Дейност | Мярка | Количество | Ед. цена |
| :--- | :--- | :--- | :--- | :--- |
| DRYW-CEILING-STD | Окачен таван сложна форма | sqm | 50.00 | €35.00 |
";

        // Act
        using (var queryContext = CreateDbContext(dbName))
        {
            var service = new MarkdownOfferParserService(queryContext, NullLogger<MarkdownOfferParserService>.Instance);
            var parsed = await service.ParseMarkdownOfferAsync(markdown);

            // Assert
            parsed.Should().NotBeNull();
            parsed.Phases.Should().HaveCount(1);
            var item = parsed.Phases[0].Items[0];
            item.Quantity.Should().Be(50.0m);
            item.UnitPriceEur.Should().Be(35.00m); // Custom price used instead of seed price
            item.TotalEur.Should().Be(1750.00m);
            item.Unit.Should().Be("м²"); // Normalized sqm -> м²
        }
    }

    [Fact]
    public async Task PreviewMarkdownOfferAsync_ShouldHandleBulgarianFreeTextMetadata_SummaryRows_AndCustomNumberedTables()
    {
        // Arrange
        var dbName = $"MdOfferTestDb_{Guid.NewGuid()}";
        using (var seedContext = CreateDbContext(dbName))
        {
            var category = new ServiceCategory
            {
                Id = Guid.NewGuid(),
                Name = "Електро",
                TemplateStructure = "{}"
            };
            var sku = new ServiceSku
            {
                Id = Guid.NewGuid(),
                ServiceCategoryId = category.Id,
                SkuCode = "EL-PTS-01",
                Name = "Монтаж на ключове и контакти",
                BasePrice = 30.0m,
                UnitType = "pcs"
            };
            await seedContext.ServiceCategories.AddAsync(category);
            await seedContext.ServiceSkus.AddAsync(sku);
            await seedContext.SaveChangesAsync();
        }

        string rawOfferText = @"# Официална Оферта за СМР
Клиент: Димитър Стоянов • Обект: гр. София, кв. Лозенец • Назначаване: admin
Надценка: 15%

## Детайлна КСС
| № | Дейност / Перо | Мярка | Количество | Ед. цена | Общо |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | **Монтаж на ключове и контакти** | бр. | 20 | 15.34 € | 306.80 € |
| **2** | **Инженерен надзор и координация** | к-т | 1 | 500 € | 500 € |
| **ОБЩО** | | | | | 806.80 € |
| | | | | | |
";

        // Act
        using (var queryContext = CreateDbContext(dbName))
        {
            var service = new MarkdownOfferParserService(queryContext, NullLogger<MarkdownOfferParserService>.Instance);
            var preview = await service.PreviewMarkdownOfferAsync(rawOfferText);

            // Assert
            preview.Should().NotBeNull();
            preview.ClientName.Should().Be("Димитър Стоянов");
            preview.SiteAddress.Should().Be("гр. София, кв. Лозенец");
            preview.AdminMarkupPercentage.Should().Be(15.0m);
            preview.IsValid.Should().BeTrue();
            preview.TotalItemCount.Should().Be(2); // Summary row and empty row skipped!
            preview.MatchedSkuCount.Should().Be(1); // Item 1 matched catalog by name!
            preview.CustomItemCount.Should().Be(1); // Item 2 accepted as custom item with user price!

            var section = preview.Sections[0];
            section.Items.Should().HaveCount(2);

            var item1 = section.Items[0];
            item1.Index.Should().Be(1);
            item1.SkuCode.Should().Be("EL-PTS-01"); // Auto-matched despite col 0 being '1'!
            item1.Quantity.Should().Be(20m);
            item1.Unit.Should().Be("бр.");

            var item2 = section.Items[1];
            item2.Index.Should().Be(2);
            item2.Title.Should().Be("Инженерен надзор и координация");
            item2.IsCustomPrice.Should().BeTrue();
            item2.BaseUnitPriceEur.Should().Be(500m);
        }
    }

    [Fact]
    public async Task PreviewMarkdownOfferAsync_ShouldProperlyHandleBudgetRangesAndEstimatorReports_WithoutConcatenation()
    {
        // Arrange
        var dbName = $"MdOfferTestDb_{Guid.NewGuid()}";
        using (var seedContext = CreateDbContext(dbName))
        {
            var category = new ServiceCategory
            {
                Id = Guid.NewGuid(),
                Name = "Ремонтни дейности",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(category);
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

## Детайлно СМР разпределение
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
        using (var queryContext = CreateDbContext(dbName))
        {
            var service = new MarkdownOfferParserService(queryContext, NullLogger<MarkdownOfferParserService>.Instance);
            var preview = await service.PreviewMarkdownOfferAsync(rawEstimatorMarkdown);

            // Assert
            preview.Should().NotBeNull();
            preview.IsValid.Should().BeTrue();
            // Should have 9 detailed tasks (summary table de-duplicated, footer rows skipped)
            preview.TotalItemCount.Should().Be(9);

            // Grand total should be the sum of the 9 average prices = ~61,875 EUR!
            // Definitely NOT quintillions!
            preview.GrandTotalEur.Should().BeInRange(60000m, 63000m);

            // Individual item checks:
            var banya = preview.Sections[0].Items.FirstOrDefault(i => i.Title.Contains("Баня"));
            banya.Should().NotBeNull();
            banya!.Quantity.Should().Be(1.0m);
            // 10 125 - 12 150 -> average is 11 137.50
            banya.BaseUnitPriceEur.Should().Be(11137.50m);
            banya.TotalEur.Should().Be(11137.50m);
        }
    }

    [Theory]
    [InlineData("- **Собственик:** Николай Иванов", "Николай Иванов")]
    [InlineData("- **Инвеститор:** Николай Иванов", "Николай Иванов")]
    [InlineData("- **Възложител:** Николай Иванов", "Николай Иванов")]
    [InlineData("- **Клиент:** Николай Иванов", "Николай Иванов")]
    [InlineData("- **Изготвено за:** Николай Иванов", "Николай Иванов")]
    public void ParseMarkdownRaw_ShouldRecognizeClientAliasesDynamically(string clientLine, string expectedName)
    {
        string markdown = $@"# Официална Оферта за СМР
### 1. ОБЩИ ДАННИ ЗА ОБЕКТА И ПРОЕКТА
{clientLine}
- **Местоположение:** гр. София, кв. Лозенец

### 2. СТРУКТУРА НА ИНВЕСТИЦИЯТА
| Компонент | Дял (%) | Стойност EUR | Стойност BGN | Какво включва |
| 1. Труд | 100% | € 1 000 | 1 956 лв. | Описание труд |
";

        var parsed = MarkdownOfferParserService.ParseMarkdownRaw(markdown);
        parsed.ClientName.Should().Be(expectedName);
    }

    [Fact]
    public void ParseMarkdownRaw_ShouldCaptureNestedSubBullets_AndSynthesizeSection5Variants()
    {
        string markdown = @"# ОФИЦИАЛНА ОФЕРТА И КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (КСС)
## Ремонт на апартамент

### 1. ОБЩИ ДАННИ ЗА ОБЕКТА И ПРОЕКТА
- **Собственик:** Николай
- **Местоположение:** София, Лозенец

### 2. СТРУКТУРА НА ИНВЕСТИЦИЯТА
| Разходно перо | Дял (%) | Стойност в EUR | Стойност в BGN | Какво включва |
| 1. Труд | 100% | € 10 000 | 19 558 лв. | Пълен инженерен пакет и надзор. |

### 3. ДЕТАЙЛНА КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (КСС)
#### РАЗДЕЛ 4: Сухо строителство и изолации
1. **Окачени тавани:** CD/UD скара с вата – **€ 3 000**
2. **Скрити корнизи & детайли за усвоени тераси:**
   - Изграждане на скрити джобове за пердета;
   - **Топлоизолационен пакет за усвоените тераси:** XPS по парапети и вата по тавани – **€ 1 350**

#### РАЗДЕЛ 5: Подови настилки – казус с паркета
> [!NOTE]
> **Вариант А: Реставрация на съществуващия паркет**
> - Презалепване, безпрахово циклене и лак.
> - *Стойност за 52 m²:* **~3 200 – 3 600 лв. (€ 1 630 – € 1 840)**

> [!TIP]
> **Вариант Б: Пълна замяна с трислоен паркет**
> - Демонтаж до плоча и саморазливна замазка.
> - *Стойност за 52 m²:* **~7 500 – 9 000 лв. (€ 3 830 – € 4 600)**

### 4. ОБОБЩЕНА ТАБЛИЦА НА ОФЕРТАТА
| № | СМР Раздел | Общ Бюджет (€) |
| :---: | :--- | :---: |
| 4 | Тавани, изолация и усвоени тераси | € 6 750 – 8 100 |
| 5 | Подови настилки & Паркет | € 4 500 – 5 400 |
";

        var parsed = MarkdownOfferParserService.ParseMarkdownRaw(markdown);

        // Section 4 Item 2 should have nested sub-bullets appended without trailing colon truncation
        var sec4 = parsed.RawSections.SelectMany(s => s.RawItems).FirstOrDefault(i => i.Title.Contains("Тавани"));
        sec4.Should().NotBeNull();
        sec4!.SubItems.Should().HaveCount(2);
        sec4.SubItems[1].Should().Contain("Скрити корнизи");
        sec4.SubItems[1].Should().Contain("Топлоизолационен пакет");
        sec4.SubItems[1].Should().Contain("€ 1 350");

        // Section 5 should have synthesized Variant A and Variant B
        var sec5 = parsed.RawSections.SelectMany(s => s.RawItems).FirstOrDefault(i => i.Title.Contains("Подови настилки"));
        sec5.Should().NotBeNull();
        sec5!.SubItems.Should().HaveCount(2);
        sec5.SubItems[0].Should().Contain("Вариант А: Реставрация");
        sec5.SubItems[0].Should().Contain("€ 1 630");
        sec5.SubItems[1].Should().Contain("Вариант Б: Пълна замяна");
        sec5.SubItems[1].Should().Contain("€ 3 830");
    }
}


