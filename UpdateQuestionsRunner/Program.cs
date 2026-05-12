using System;
using System.Collections.Generic;
using Npgsql;

class Program {
    static void Main() {
        string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
        using var conn = new NpgsqlConnection(connString);
        conn.Open();

        // 1. Get Categories
        var categories = new Dictionary<string, Guid>();
        using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"Name\" FROM \"ServiceCategories\";", conn))
        using (var reader = cmd.ExecuteReader()) {
            while (reader.Read()) {
                categories[reader.GetString(1)] = reader.GetGuid(0);
            }
        }

        Console.WriteLine($"Found {categories.Count} categories.");

        // Define SKUs
        var skusToInsert = new List<SkuDef>();

        // Global Overhead
        if (categories.TryGetValue("Global Questions", out var globalId)) {
            skusToInsert.Add(new SkuDef(globalId, "GEN-001", "Site Prep & Protection", "Preparation, protection, and logistics.", 50, "Flat"));
            skusToInsert.Add(new SkuDef(globalId, "GEN-002", "Final Cleaning", "Complete final cleaning after works.", 2, "sqm"));
            skusToInsert.Add(new SkuDef(globalId, "GEN-003", "Daily Cleaning", "Daily site cleaning.", 30, "Flat"));
        }

        // Electrical
        var elecKey = "Електрическа Инсталация";
        if (categories.TryGetValue(elecKey, out var elecId)) {
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CABLE-LAY", "Полагане на силов кабел", "Издърпване и фиксиране на кабел.", 1.5m, "m"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CABLE-HEAVY", "Полагане на мощен кабел", "Дебел кабел за проточни бойлери.", 3m, "m"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CHASE-CONC", "Къртене на канал в бетон", "Изкопаване на канал в бетон.", 12m, "m"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-LAY-TUBE", "Полагане на гофре", "Полагане на гофрирана тръба.", 3m, "m"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-PANEL-MOD", "Сглобяване на табло (на модул)", "Подреждане на предпазители.", 12m, "module"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-STD", "Изграждане на излазна точка", "Труд за 1 брой контакт/ключ.", 30m, "pcs"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-LV", "Слаботокова точка", "LAN/TV/СОТ.", 25m, "pcs"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-DEV", "Девиаторна точка", "Девиаторни ключове.", 45m, "pcs"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-SPEC", "Извод за щори/вентилатор", "Вентилатори или щори.", 35m, "pcs"));
        }

        // Painting
        var pantKey = "Бояджийски и шпакловъчни услуги (Painting)";
        if (categories.TryGetValue(pantKey, out var pantId)) {
            skusToInsert.Add(new SkuDef(pantId, "PANT-001", "Грундиране", "Грундиране на стени и тавани.", 2m, "sqm"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-002", "Шпакловка", "Цялостна шпакловка.", 7m, "sqm"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-003", "Боядисване", "Боядисване с латекс (2 ръце).", 6m, "sqm"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-004", "Шлайфане", "Шлайфане на стени.", 2m, "sqm"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-005", "Боядисване на врати/первази", "Боядисване на декоративни елементи.", 15m, "pcs"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-006", "Перфектна шпакловка Q5", "Шпакловка Q5.", 12m, "sqm"));
        }

        // Drywall
        var drywKey = "Сухо строителство (Drywall)";
        if (categories.TryGetValue(drywKey, out var drywId)) {
            skusToInsert.Add(new SkuDef(drywId, "DRYW-001", "Окачен таван", "Монтаж на окачен таван.", 23m, "sqm"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-002", "Преградна стена", "Изграждане на преградна стена.", 23m, "sqm"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-003", "Предстенна обшивка", "Монтаж на предстенна обшивка.", 18m, "sqm"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-004", "Сложни форми", "Овални стени, арки.", 35m, "m"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-005", "Куфари", "Обличане на тръби.", 15m, "m"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-006", "Влагоустойчив картон", "За мокри помещения.", 5m, "sqm"));
        }

        // Tiling
        var tileKey = "Подови и стенни настилки (Tiling)";
        if (categories.TryGetValue(tileKey, out var tileId)) {
            skusToInsert.Add(new SkuDef(tileId, "TILE-001", "Стандартни плочки", "Лепене на стандартни плочки.", 32m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-002", "Цокъл", "Монтаж на цокъл.", 7m, "m"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-003", "Гранитогрес (голям формат)", "Лепене на голям формат плочки.", 45m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-004", "Ламинат", "Редене на ламинат.", 7m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-005", "Хидроизолация", "Полагане на хидроизолация.", 12m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-006", "Епоксидна фуга", "Използване на епоксидна фуга.", 10m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-007", "Сложно редене", "Рибена кост, диагонал.", 15m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-008", "Облицовка с камък/тухлички", "Декоративна облицовка.", 40m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-009", "Саморазливна замазка", "Нивелиране на под.", 12m, "sqm"));
        }

        // Microcement
        var micoKey = "Микроцимент (Microcement)";
        if (categories.TryGetValue(micoKey, out var micoId)) {
            skusToInsert.Add(new SkuDef(micoId, "MICO-001", "Микроцимент", "Полагане на микроцимент.", 65m, "sqm"));
            skusToInsert.Add(new SkuDef(micoId, "MICO-002", "Микроцимент (Мокри помещения)", "Микроцимент за баня.", 80m, "sqm"));
            skusToInsert.Add(new SkuDef(micoId, "MICO-003", "Подготовка върху стари плочки", "Подготовка на основата.", 15m, "sqm"));
            skusToInsert.Add(new SkuDef(micoId, "MICO-004", "Хидроизолация", "Допълнителна хидроизолация.", 12m, "sqm"));
        }

        // Plumbing
        var plmbKey = "ВиК Услуги (Plumbing)";
        if (categories.TryGetValue(plmbKey, out var plmbId)) {
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-001", "Водопровод", "Подмяна на водопровод.", 290m, "flat"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-002", "Канализация", "Подмяна на канални тръби.", 275m, "flat"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-003", "Монтаж санитария", "Монтаж на мивки, душове и др.", 60m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-005", "Местене на точка", "Изместване на ВиК точка.", 80m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-006", "Вградена структура", "Монтаж на конзолна тоалетна.", 200m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-007", "Скрити тръби (къртене)", "Къртене на канал за тръби.", 15m, "m"));
        }

        // Demolition
        var demoKey = "Къртене и извозване (Demolition)";
        if (categories.TryGetValue(demoKey, out var demoId)) {
            skusToInsert.Add(new SkuDef(demoId, "DEMO-001", "Къртене на плочки", "Къртене на фаянс/теракота.", 15m, "sqm"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-002", "Къртене на бетон", "Къртене на бетонни стени.", 65m, "cubic_m"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-003", "Извозване", "Контейнер за строителни отпадъци.", 45m, "pcs"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-004", "Сваляне на тапети", "Премахване на тапети.", 3m, "sqm"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-005", "Къртене на замазка", "Премахване на подова замазка.", 12m, "sqm"));
        }

        int inserted = 0;
        foreach (var sku in skusToInsert) {
            // Check if exists
            bool exists = false;
            using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"ServiceSkus\" WHERE \"SkuCode\" = @code;", conn)) {
                checkCmd.Parameters.AddWithValue("code", sku.SkuCode);
                exists = Convert.ToInt64(checkCmd.ExecuteScalar()) > 0;
            }

            if (!exists) {
                using (var insCmd = new NpgsqlCommand(
                    "INSERT INTO \"ServiceSkus\" (\"Id\", \"ServiceCategoryId\", \"SkuCode\", \"Name\", \"Description\", \"BasePrice\", \"UnitType\", \"CreatedAt\", \"UpdatedAt\") " +
                    "VALUES (@id, @catId, @code, @name, @desc, @price, @unit, @time, @time);", conn)) {
                    insCmd.Parameters.AddWithValue("id", Guid.NewGuid());
                    insCmd.Parameters.AddWithValue("catId", sku.CategoryId);
                    insCmd.Parameters.AddWithValue("code", sku.SkuCode);
                    insCmd.Parameters.AddWithValue("name", sku.Name);
                    insCmd.Parameters.AddWithValue("desc", sku.Description);
                    insCmd.Parameters.AddWithValue("price", sku.Price);
                    insCmd.Parameters.AddWithValue("unit", sku.Unit);
                    insCmd.Parameters.AddWithValue("time", DateTime.UtcNow);
                    insCmd.ExecuteNonQuery();
                    inserted++;
                }
            }
        }

        Console.WriteLine($"Successfully inserted {inserted} new SKUs.");
    }

    class SkuDef {
        public Guid CategoryId { get; }
        public string SkuCode { get; }
        public string Name { get; }
        public string Description { get; }
        public decimal Price { get; }
        public string Unit { get; }

        public SkuDef(Guid categoryId, string skuCode, string name, string description, decimal price, string unit) {
            CategoryId = categoryId;
            SkuCode = skuCode;
            Name = name;
            Description = description;
            Price = price;
            Unit = unit;
        }
    }
}