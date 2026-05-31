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
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CABLE-LAY", "Полагане на силов кабел", "Издърпване и фиксиране на кабел.", 2m, "m", "if(Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, if(Contains(elec_scope, 'Частичен'), global_total_sqm * 1.0, 0))"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CABLE-HEAVY", "Полагане на мощен кабел", "Дебел кабел за проточни бойлери.", 5m, "m", "(Count(elec_heavy_appliances) + elec_ac_count) * 10"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CHASE-CONC", "Къртене на канал в бетон", "Изкопаване на канал в бетон.", 15m, "m", "if(Contains(elec_walls, 'Бетон') && Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CHASE-BRICK", "Къртене на канал в тухла", "Изкопаване на канал в тухла.", 8m, "m", "if(Contains(elec_walls, 'Тухла') && Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-LAY-TUBE", "Полагане на гофре", "Полагане на гофрирана тръба.", 4m, "m", "if(Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-PANEL-MOD", "Сглобяване на табло (на модул)", "Подреждане на предпазители.", 15m, "module", "12 + Count(elec_heavy_appliances) + elec_ac_count"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-PANEL-NICHE", "Изкопаване на ниша за вградено табло", "Скрит монтаж.", 95m, "pcs", "if(Contains(elec_panel, 'скрито'), 1, 0)"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-STD", "Изграждане на излазна точка", "Труд за 1 брой контакт/ключ.", 35m, "pcs", "if(Contains(elec_outlets_comfort, 'Базово'), (global_room_count * 3) + 4, if(Contains(elec_outlets_comfort, 'Комфорт'), (global_room_count * 5) + 6, if(Contains(elec_outlets_comfort, 'Премиум'), (global_room_count * 8) + 10, 0)))"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-LV", "Слаботокова точка", "LAN/TV/СОТ.", 30m, "pcs", "1"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-DEV", "Девиаторна точка", "Девиаторни ключове.", 55m, "pcs", "1"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-SPEC", "Извод за щори/вентилатор", "Вентилатори или щори.", 40m, "pcs", "1"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-LED-TRAFO", "Монтаж на захранващ блок (Траф) за LED", "Трансформатор.", 30m, "pcs", "if(Contains(elec_lighting, 'LED'), 1, 0)"));
        }

        // Painting
        var pantKey = "Бояджийски и шпакловъчни услуги (Painting)";
        if (categories.TryGetValue(pantKey, out var pantId)) {
            skusToInsert.Add(new SkuDef(pantId, "PANT-PRIMER", "Дълбокопроникващ грунд", "Грундиране на стени и тавани.", 3m, "sqm"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-SPACKLE-STD", "Шпакловка (Стандартна 2 ръце)", "Цялостна шпакловка.", 14m, "sqm"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-SPACKLE-Q5", "Фина шпакловка (Перфектна Q5)", "Шитрок за идеално гладка повърхност.", 20m, "sqm"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-PAINT-WHITE", "Боядисване в бяло (2 ръце)", "Боядисване с бял латекс.", 6.50m, "sqm"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-PAINT-COLOR", "Боядисване в цвят (2 ръце)", "Боядисване с цветен латекс.", 8.50m, "sqm"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-TAPE-CORNER", "Поставяне на ъглохранители", "Алуминиеви или PVC ъгли.", 6m, "m"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-TRIM", "Боядисване на врати / первази", "Боядисване на декоративни елементи.", 45m, "pcs"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-WALLPAPER-REMOVE", "Сваляне на стари тапети", "Сваляне на стари тапети.", 4.50m, "sqm"));
        }

        // Drywall
        var drywKey = "Сухо строителство (Drywall)";
        if (categories.TryGetValue(drywKey, out var drywId)) {
            skusToInsert.Add(new SkuDef(drywId, "DRYW-CEILING-STD", "Окачен таван (Едно ниво)", "Монтаж на окачен таван.", 45m, "sqm"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-WALL-PARTITION", "Преградна стена (Двуслойна)", "Изграждане на преградна стена.", 65m, "sqm"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-WALL-LINING", "Предстенна обшивка", "Монтаж на предстенна обшивка.", 40m, "sqm"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-BOX", "Изграждане на куфари (Кутии)", "Обличане на тръби.", 40m, "m"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-INSULATION", "Монтаж на вата (Топло/Шумо)", "Поставяне на минерална или каменна вата.", 10m, "sqm"));
        }

        // Tiling
        var tileKey = "Подови и стенни настилки (Tiling)";
        if (categories.TryGetValue(tileKey, out var tileId)) {
            skusToInsert.Add(new SkuDef(tileId, "TILE-STD", "Лепене на стандартни плочки", "Полагане на фаянс или теракот.", 60m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-LARGE", "Лепене на голямоформатен гранитогрес", "Плочи над 60х120 см.", 95m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-PREP-LEVEL", "Саморазливна замазка", "Изравняване на пода.", 22m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-PREP-HYDRO", "Полагане на хидроизолация (с лента)", "Запечатване на мокри помещения.", 30m, "sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-LAMINATE", "Монтаж на ламинат", "Полагане на ламиниран паркет.", 12m, "sqm"));
        }

        // Microcement
        var micoKey = "Микроцимент (Microcement)";
        if (categories.TryGetValue(micoKey, out var micoId)) {
            skusToInsert.Add(new SkuDef(micoId, "MICRO-STD", "Полагане на микроцимент (сухи зони)", "Полагане на микроцимент.", 140m, "sqm"));
            skusToInsert.Add(new SkuDef(micoId, "MICRO-BATH", "Полагане на микроцимент в мокри зони (Баня)", "Микроцимент за баня.", 180m, "sqm"));
        }

        // Plumbing
        var plmbKey = "ВиК Услуги (Plumbing)";
        if (categories.TryGetValue(plmbKey, out var plmbId)) {
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-POINT-NEW", "Изграждане на нова ВиК точка", "Тръби за топла, студена вода и канал.", 70m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-RISER-REPLACE", "Смяна на вертикален щранг", "Подмяна на основните метални тръби.", 240m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-SINK-INSTALL", "Монтаж на мивка със смесител и сифон", "Монтаж на мивки.", 90m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-WC-STD", "Монтаж на стандартна тоалетна (моноблок)", "Монтаж на тоалетна.", 140m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-WC-BUILTIN", "Монтаж на структура за вграждане", "Конзолна тоалетна.", 190m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-SHOWER-CABIN", "Монтаж на душ кабина или стъклен параван", "Душ кабина.", 330m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-SHOWER-FIXTURE", "Монтаж на душ система", "Душ батерия.", 70m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-BOILER", "Монтаж на електрически бойлер", "Бойлер до 100л.", 140m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-APPLIANCE", "Свързване на пералня / съдомиялна", "Уреди.", 80m, "pcs"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-METER-REPLACE", "Смяна на водомер", "Нов водомер.", 60m, "pcs"));
        }

        // Demolition
        var demoKey = "Къртене и извозване (Demolition)";
        if (categories.TryGetValue(demoKey, out var demoId)) {
            skusToInsert.Add(new SkuDef(demoId, "DEMO-BATH-FULL", "Цялостно къртене на баня", "Къртене на баня.", 750m, "pcs"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-WALL-BRICK", "Къртене на тухлена стена", "Събаряне на тухлени стени.", 20m, "sqm"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-WALL-CONC", "Къртене на бетонна стена/панел", "Къртене на бетон.", 50m, "sqm"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-FLOOR-TILE", "Къртене на подови настилки/замазка", "Премахване на настилки.", 15m, "sqm"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-DISPOSAL", "Извозване с контейнер", "Наемане на строителен контейнер.", 250m, "pcs"));
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
                    "INSERT INTO \"ServiceSkus\" (\"Id\", \"ServiceCategoryId\", \"SkuCode\", \"Name\", \"Description\", \"BasePrice\", \"UnitType\", \"CalculationFormula\", \"CreatedAt\", \"UpdatedAt\") " +
                    "VALUES (@id, @catId, @code, @name, @desc, @price, @unit, @formula, @time, @time);", conn)) {
                    insCmd.Parameters.AddWithValue("id", Guid.NewGuid());
                    insCmd.Parameters.AddWithValue("catId", sku.CategoryId);
                    insCmd.Parameters.AddWithValue("code", sku.SkuCode);
                    insCmd.Parameters.AddWithValue("name", sku.Name);
                    insCmd.Parameters.AddWithValue("desc", sku.Description);
                    insCmd.Parameters.AddWithValue("price", sku.Price);
                    insCmd.Parameters.AddWithValue("unit", sku.Unit);
                    insCmd.Parameters.AddWithValue("formula", sku.Formula);
                    insCmd.Parameters.AddWithValue("time", DateTime.UtcNow);
                    insCmd.ExecuteNonQuery();
                    inserted++;
                }
            } else {
                using (var updCmd = new NpgsqlCommand(
                    "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = @formula, \"BasePrice\" = @price, \"UpdatedAt\" = @time WHERE \"SkuCode\" = @code;", conn)) {
                    updCmd.Parameters.AddWithValue("formula", sku.Formula);
                    updCmd.Parameters.AddWithValue("price", sku.Price);
                    updCmd.Parameters.AddWithValue("time", DateTime.UtcNow);
                    updCmd.Parameters.AddWithValue("code", sku.SkuCode);
                    updCmd.ExecuteNonQuery();
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
        public string Formula { get; }

        public SkuDef(Guid categoryId, string skuCode, string name, string description, decimal price, string unit, string formula = "1") {
            CategoryId = categoryId;
            SkuCode = skuCode;
            Name = name;
            Description = description;
            Price = price;
            Unit = unit;
            Formula = formula;
        }
    }
}