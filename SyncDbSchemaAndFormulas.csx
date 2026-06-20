#r "nuget: Npgsql, 7.0.4"
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Npgsql;

// Determine connection string
string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
if (Args.Count > 0 && !string.IsNullOrWhiteSpace(Args[0])) {
    connString = Args[0];
    Console.WriteLine($"Using custom database connection string: {connString}");
} else {
    Console.WriteLine($"Using default local connection string: {connString}");
}

// 1. Sync Category templates from Categories_Seed_Templates.json
string jsonPath = @"Categories_Seed_Templates.json";
if (!File.Exists(jsonPath)) {
    Console.WriteLine($"Error: {jsonPath} not found!");
    Environment.Exit(1);
}

using (var conn = new NpgsqlConnection(connString)) {
    try {
        conn.Open();
        Console.WriteLine("Database connection opened successfully.");

        // Clean up suffix categories if any exist and merge them
        Console.WriteLine("Running category suffix cleanup and merge...");
        var suffixMap = new Dictionary<string, string> {
            { "Electrical", "Електрическа Инсталация" },
            { "Painting", "Бояджийски и шпакловъчни услуги" },
            { "Plumbing", "ВиК Услуги" },
            { "Demolition", "Къртене и извозване" },
            { "Drywall", "Сухо строителство" },
            { "Tiling", "Подови и стенни настилки" },
            { "Microcement", "Микроцимент" },
            { "ВиК Услуги (Plumbing)", "ВиК Услуги" },
            { "Бояджийски и шпакловъчни услуги (Painting)", "Бояджийски и шпакловъчни услуги" },
            { "Къртене и извозване (Demolition)", "Къртене и извозване" },
            { "Сухо строителство (Drywall)", "Сухо строителство" },
            { "Подови и стенни настилки (Tiling)", "Подови и стенни настилки" },
            { "Микроцимент (Microcement)", "Микроцимент" }
        };

        foreach (var entry in suffixMap) {
            var suffixName = entry.Key;
            var cleanName = entry.Value;

            Guid suffixId = Guid.Empty;
            Guid cleanId = Guid.Empty;

            using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"Name\" FROM \"ServiceCategories\" WHERE \"Name\" IN (@suffix, @clean);", conn)) {
                cmd.Parameters.AddWithValue("suffix", suffixName);
                cmd.Parameters.AddWithValue("clean", cleanName);
                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        var id = reader.GetGuid(0);
                        var name = reader.GetString(1);
                        if (name == suffixName) suffixId = id;
                        else if (name == cleanName) cleanId = id;
                    }
                }
            }

            if (suffixId != Guid.Empty) {
                if (cleanId == Guid.Empty) {
                    // Rename suffix to clean
                    using (var cmd = new NpgsqlCommand("UPDATE \"ServiceCategories\" SET \"Name\" = @clean, \"UpdatedAt\" = now() WHERE \"Id\" = @id;", conn)) {
                        cmd.Parameters.AddWithValue("clean", cleanName);
                        cmd.Parameters.AddWithValue("id", suffixId);
                        cmd.ExecuteNonQuery();
                    }
                    Console.WriteLine($"Renamed category '{suffixName}' to '{cleanName}'.");
                } else {
                    // Merge relations
                    Console.WriteLine($"Merging category '{suffixName}' into '{cleanName}'...");

                    // Update ServiceSkus
                    using (var cmd = new NpgsqlCommand("UPDATE \"ServiceSkus\" SET \"ServiceCategoryId\" = @cleanId, \"UpdatedAt\" = now() WHERE \"ServiceCategoryId\" = @suffixId;", conn)) {
                        cmd.Parameters.AddWithValue("cleanId", cleanId);
                        cmd.Parameters.AddWithValue("suffixId", suffixId);
                        cmd.ExecuteNonQuery();
                    }

                    // Update TradesmanSkills
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE \"TradesmanSkills\" SET \"ServiceCategoryId\" = @cleanId WHERE \"ServiceCategoryId\" = @suffixId " +
                        "AND \"TradesmanProfileId\" NOT IN (SELECT \"TradesmanProfileId\" FROM \"TradesmanSkills\" WHERE \"ServiceCategoryId\" = @cleanId);", conn)) {
                        cmd.Parameters.AddWithValue("cleanId", cleanId);
                        cmd.Parameters.AddWithValue("suffixId", suffixId);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new NpgsqlCommand("DELETE FROM \"TradesmanSkills\" WHERE \"ServiceCategoryId\" = @suffixId;", conn)) {
                        cmd.Parameters.AddWithValue("suffixId", suffixId);
                        cmd.ExecuteNonQuery();
                    }

                    // Update TradesmanMedia
                    using (var cmd = new NpgsqlCommand("UPDATE \"TradesmanMedia\" SET \"ServiceCategoryId\" = @cleanId, \"UpdatedAt\" = now() WHERE \"ServiceCategoryId\" = @suffixId;", conn)) {
                        cmd.Parameters.AddWithValue("cleanId", cleanId);
                        cmd.Parameters.AddWithValue("suffixId", suffixId);
                        cmd.ExecuteNonQuery();
                    }

                    // Update JobPosts
                    using (var cmd = new NpgsqlCommand("UPDATE \"JobPosts\" SET \"ServiceCategoryId\" = @cleanId, \"UpdatedAt\" = now() WHERE \"ServiceCategoryId\" = @suffixId;", conn)) {
                        cmd.Parameters.AddWithValue("cleanId", cleanId);
                        cmd.Parameters.AddWithValue("suffixId", suffixId);
                        cmd.ExecuteNonQuery();
                    }

                    // Delete suffix category
                    using (var cmd = new NpgsqlCommand("DELETE FROM \"ServiceCategories\" WHERE \"Id\" = @suffixId;", conn)) {
                        cmd.Parameters.AddWithValue("suffixId", suffixId);
                        cmd.ExecuteNonQuery();
                    }
                    Console.WriteLine($"Merged and deleted duplicate category '{suffixName}'.");
                }
            }
        }

        // Process json templates
        string jsonContent = File.ReadAllText(jsonPath);
        using (var doc = JsonDocument.Parse(jsonContent)) {
            foreach (var category in doc.RootElement.EnumerateObject()) {
                string catKey = category.Name;
                string catName = category.Value.GetProperty("name").GetString();
                bool isGlobal = catKey == "global_category";
                string templateStructure = category.Value.GetProperty("templateStructure").GetRawText();

                // Check if category exists
                Guid catId = Guid.Empty;
                using (var cmd = new NpgsqlCommand("SELECT \"Id\" FROM \"ServiceCategories\" WHERE \"Name\" = @name;", conn)) {
                    cmd.Parameters.AddWithValue("name", catName);
                    var val = cmd.ExecuteScalar();
                    if (val != null) catId = (Guid)val;
                }

                if (catId == Guid.Empty) {
                    catId = Guid.NewGuid();
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO \"ServiceCategories\" (\"Id\", \"Name\", \"Status\", \"IsGlobal\", \"TemplateStructure\", \"CreatedAt\", \"UpdatedAt\") " +
                        "VALUES (@id, @name, 1, @isGlobal, @template::jsonb, now(), now());", conn)) {
                        cmd.Parameters.AddWithValue("id", catId);
                        cmd.Parameters.AddWithValue("name", catName);
                        cmd.Parameters.AddWithValue("isGlobal", isGlobal);
                        cmd.Parameters.AddWithValue("template", templateStructure);
                        cmd.ExecuteNonQuery();
                    }
                    Console.WriteLine($"Inserted category template: '{catName}'");
                } else {
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE \"ServiceCategories\" SET \"TemplateStructure\" = @template::jsonb, \"IsGlobal\" = @isGlobal, \"UpdatedAt\" = now() WHERE \"Id\" = @id;", conn)) {
                        cmd.Parameters.AddWithValue("template", templateStructure);
                        cmd.Parameters.AddWithValue("isGlobal", isGlobal);
                        cmd.Parameters.AddWithValue("id", catId);
                        cmd.ExecuteNonQuery();
                    }
                    Console.WriteLine($"Updated category template: '{catName}'");
                }
            }
        }

        // 2. Fetch all updated categories
        var categories = new Dictionary<string, Guid>();
        using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"Name\" FROM \"ServiceCategories\";", conn))
        using (var reader = cmd.ExecuteReader()) {
            while (reader.Read()) {
                categories[reader.GetString(1)] = reader.GetGuid(0);
            }
        }

        // Define SKUs (aligned with clean names)
        var skusToInsert = new List<SkuDef>();

        // Global Overhead
        if (categories.TryGetValue("Global Questions", out var globalId)) {
            skusToInsert.Add(new SkuDef(globalId, "GEN-001", "Site Prep & Protection", "Preparation, protection, and logistics.", 50, "Flat"));
            skusToInsert.Add(new SkuDef(globalId, "GEN-002", "Final Cleaning", "Complete final cleaning after works.", 2, "sqm"));
            skusToInsert.Add(new SkuDef(globalId, "GEN-003", "Daily Cleaning", "Daily site cleaning.", 30, "Flat"));
        }

        // Electrical
        if (categories.TryGetValue("Електрическа Инсталация", out var elecId)) {
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CABLE-LAY", "Полагане на силов кабел", "Издърпване и фиксиране на кабел.", 2m, "m", "if(Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, if(Contains(elec_scope, 'Частичен'), global_total_sqm * 1.0, 0))"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CABLE-HEAVY", "Полагане на мощен кабел", "Дебел кабел за проточни бойлери.", 5m, "m", "(Count(elec_heavy_appliances) + elec_ac_count) * 10"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CHASE-CONC", "Къртене на канал в бетон", "Изкопаване на канал в бетон.", 15m, "m", "if(Contains(global_wall_material, 'Бетон') && Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-CHASE-BRICK", "Къртене на канал в тухла", "Изкопаване на канал в тухла.", 8m, "m", "if(Contains(global_wall_material, 'Тухла') && Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-LAY-TUBE", "Полагане на гофре", "Полагане на гофрирана тръба.", 4m, "m", "if(Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-PANEL-MOD", "Сглобяване на табло (на модул)", "Подреждане на предпазители.", 15m, "module", "12 + Count(elec_heavy_appliances) + elec_ac_count"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-PANEL-NICHE", "Изкопаване на ниша за вградено табло", "Скрит монтаж.", 95m, "pcs", "if(Contains(elec_panel, 'скрито'), 1, 0)"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-STD", "Изграждане на излазна точка", "Труд за 1 брой контакт/ключ.", 35m, "pcs", "if(Contains(elec_outlets_comfort, 'Базово'), (global_room_count * 3) + 4, if(Contains(elec_outlets_comfort, 'Комфорт'), (global_room_count * 5) + 6, if(Contains(elec_outlets_comfort, 'Премиум'), (global_room_count * 8) + 10, 0)))"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-LV", "Слаботокова точка", "LAN/TV/СОТ.", 30m, "pcs", "elec_lv_count"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-DEV", "Девиаторна точка", "Девиаторни ключове.", 55m, "pcs", "elec_dev_count"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-POINT-SPEC", "Извод за щори/вентилатор", "Вентилатори или щори.", 40m, "pcs", "elec_spec_count"));
            skusToInsert.Add(new SkuDef(elecId, "ELEC-LED-TRAFO", "Монтаж на захранващ блок (Траф) за LED", "Трансформатор.", 30m, "pcs", "if(Contains(elec_lighting, 'LED'), 1, 0)"));
        }

        // Painting
        if (categories.TryGetValue("Бояджийски и шпакловъчни услуги", out var pantId)) {
            skusToInsert.Add(new SkuDef(pantId, "PANT-PRIMER", "Дълбокопроникващ грунд", "Грундиране на стени и тавани.", 3m, "sqm", "if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-SPACKLE-STD", "Шпакловка (Стандартна 2 ръце)", "Цялостна шпакловка.", 14m, "sqm", "if(Contains(paint_tasks, 'Цялостна шпакловка') || Contains(paint_tasks, 'Сваляне на тапети'), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5), 0)"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-SPACKLE-Q5", "Фина шпакловка (Перфектна Q5)", "Шитрок за идеално гладка повърхност.", 20m, "sqm", "if(Contains(paint_finish_level, 'Q5') || Contains(paint_finish_level, 'Перфектно'), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5), 0)"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-PAINT-WHITE", "Боядисване в бяло (2 ръце)", "Боядисване с бял латекс.", 6.50m, "sqm", "if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-PAINT-COLOR", "Боядисване в цвят (2 ръце)", "Боядисване с цветен латекс.", 8.50m, "sqm", "if(Contains(paint_colors, 'цвят') && !Contains(paint_colors, 'бяло'), if(paint_sqm > 0, paint_sqm * 0.5, global_total_sqm * 1.2), 0)"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-TAPE-CORNER", "Поставяне на ъглохранители", "Алуминиеви или PVC ъгли.", 6m, "m", "if(paint_sqm > 0, paint_sqm * 0.1, global_total_sqm * 0.25)"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-TRIM", "Боядисване на врати / первази", "Боядисване на декоративни елементи.", 45m, "pcs", "if(Contains(paint_trim_doors_count, '4+'), 4, if(Contains(paint_trim_doors_count, '3'), 3, if(Contains(paint_trim_doors_count, '2'), 2, if(Contains(paint_trim_doors_count, '1'), 1, 0))))"));
            skusToInsert.Add(new SkuDef(pantId, "PANT-WALLPAPER-REMOVE", "Сваляне на стари тапети", "Сваляне на стари тапети.", 4.50m, "sqm", "if(Contains(paint_tasks, 'Сваляне на тапети'), if(paint_sqm > 0, paint_sqm * 0.5, global_total_sqm), 0)"));
        }

        // Drywall
        if (categories.TryGetValue("Сухо строителство", out var drywId)) {
            skusToInsert.Add(new SkuDef(drywId, "DRYW-CEILING-STD", "Окачен таван (Едно ниво)", "Монтаж на окачен таван.", 45m, "sqm", "dryw_ceiling_sqm"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-WALL-PARTITION", "Преградна стена (Двуслойна)", "Изграждане на преградна стена.", 65m, "sqm", "dryw_partition_sqm"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-WALL-LINING", "Предстенна обшивка", "Монтаж на предстенна обшивка.", 40m, "sqm", "dryw_lining_sqm"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-BOX", "Изграждане на куфари (Кутии)", "Обличане на тръби.", 40m, "m", "dryw_box_m"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-INSUL-CEILING", "Монтаж на вата (Тавани)", "Поставяне на минерална или каменна вата in окачен таван.", 10m, "sqm", "if(Contains(drywall_insulation, 'Да') && Contains(dryw_insulation_areas, 'тавани'), dryw_ceiling_sqm, 0)"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-INSUL-PARTITION", "Монтаж на вата (Преградни стени)", "Поставяне на минерална или каменна вата in преградни стени.", 10m, "sqm", "if(Contains(drywall_insulation, 'Да') && Contains(dryw_insulation_areas, 'стените'), dryw_partition_sqm, 0)"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-INSUL-LINING", "Монтаж на вата (Предстенна обшивка)", "Поставяне на минерална или каменна вата in предстенни обшивки.", 10m, "sqm", "if(Contains(drywall_insulation, 'Да') && Contains(dryw_insulation_areas, 'стените'), dryw_lining_sqm, 0)"));
            skusToInsert.Add(new SkuDef(drywId, "DRYW-INSUL-BOX", "Монтаж на вата (Куфари)", "Поставяне на минерална или каменна вата in куфари.", 10m, "m", "if(Contains(drywall_insulation, 'Да') && Contains(dryw_insulation_areas, 'куфарите'), dryw_box_m, 0)"));
        }

        // Tiling
        if (categories.TryGetValue("Подови и стенни настилки", out var tileId)) {
            skusToInsert.Add(new SkuDef(tileId, "TILE-STD", "Лепене на стандартни плочки", "Полагане на фаянс или теракот.", 60m, "sqm", "tile_std_sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-LARGE", "Лепене на голямоформатен гранитогрес", "Плочи над 60х120 см.", 95m, "sqm", "tile_large_sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-PREP-LEVEL", "Саморазливна замазка", "Изравняване на пода.", 22m, "sqm", "tile_prep_level_sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-PREP-HYDRO", "Полагане на хидроизолация (с лента)", "Запечатване на мокри помещения.", 30m, "sqm", "tile_prep_hydro_sqm"));
            skusToInsert.Add(new SkuDef(tileId, "TILE-LAMINATE", "Монтаж на ламинат", "Полагане на ламиниран паркет.", 6m, "sqm", "tile_laminate_sqm"));
        }

        // Microcement
        if (categories.TryGetValue("Микроцимент", out var micoId)) {
            skusToInsert.Add(new SkuDef(micoId, "MICRO-STD", "Полагане на микроцимент (сухи зони)", "Полагане на микроцимент.", 140m, "sqm", "if(Contains(mico_area, 'Сухи зони'), if(Contains(mico_rooms, '1-2 стаи'), 30, global_total_sqm * 0.8), 0)"));
            skusToInsert.Add(new SkuDef(micoId, "MICRO-BATH", "Полагане на микроцимент в мокри зони (Баня)", "Микроцимент за баня.", 180m, "sqm", "if(Contains(mico_area, 'Мокри зони'), global_bathroom_count * 20, 0)"));
        }

        // Plumbing
        if (categories.TryGetValue("ВиК Услуги", out var plmbId)) {
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-POINT-NEW", "Изграждане на нова ВиК точка", "Тръби за топла, студена вода и канал.", 70m, "pcs", "if(Contains(plumb_scope, 'Цялостна'), (global_bathroom_count * 5) + 3, if(Contains(plumb_scope, 'извеждане'), 3, 0))"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-RISER-REPLACE", "Смяна на вертикален щранг", "Подмяна на основните метални тръби.", 240m, "pcs", "if(Contains(plumb_riser, 'Да'), global_bathroom_count, 0)"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-SINK-INSTALL", "Монтаж на мивка със смесител и сифон", "Монтаж на мивки.", 90m, "pcs", "if(Contains(plumb_sink_count, '3+'), 3, if(Contains(plumb_sink_count, '2'), 2, if(Contains(plumb_sink_count, '1'), 1, 0)))"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-WC-STD", "Монтаж на стандартна тоалетна (моноблок)", "Монтаж на тоалетна.", 140m, "pcs", "if(Contains(plumb_wc_type, 'Стандартна'), global_bathroom_count, 0)"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-WC-BUILTIN", "Монтаж на структура за вграждане", "Конзолна тоалетна.", 190m, "pcs", "if(Contains(plumb_wc_type, 'Вградена'), global_bathroom_count, 0)"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-SHOWER-CABIN", "Монтаж на душ кабина или стъклен параван", "Душ кабина.", 330m, "pcs", "if(Contains(plumb_shower_type, 'кабина') || Contains(plumb_shower_type, 'Вана'), global_bathroom_count, 0)"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-SHOWER-FIXTURE", "Монтаж на душ система", "Душ батерия.", 70m, "pcs", "if(Contains(plumb_shower_type, 'Само') || Contains(plumb_shower_type, 'кабина') || Contains(plumb_shower_type, 'Вана'), global_bathroom_count, 0)"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-BOILER", "Монтаж на електрически бойлер", "Бойлер до 100л.", 140m, "pcs", "if(Contains(plumb_appliances, 'бойлер'), 1, 0)"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-APPLIANCE", "Свързване на пералня / съдомиялна", "Уреди.", 80m, "pcs", "if(Contains(plumb_appliances, 'Пералня') && Contains(plumb_appliances, 'Съдомиялна'), 2, if(Contains(plumb_appliances, 'Пералня') || Contains(plumb_appliances, 'Съдомиялна'), 1, 0))"));
            skusToInsert.Add(new SkuDef(plmbId, "PLMB-METER-REPLACE", "Смяна на водомер", "Нов водомер.", 60m, "pcs", "global_bathroom_count * 2"));
        }

        // Demolition
        if (categories.TryGetValue("Къртене и извозване", out var demoId)) {
            skusToInsert.Add(new SkuDef(demoId, "DEMO-BATH-FULL", "Цялостно къртене на баня", "Къртене на баня.", 750m, "pcs", "if(Contains(demo_what, 'Цяла баня'), global_bathroom_count, 0)"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-WALL-BRICK", "Къртене на тухлена стена", "Събаряне на тухлени стени.", 20m, "sqm", "demo_brick_sqm"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-WALL-CONC", "Къртене на бетонна стена/панел", "Къртене на бетон.", 50m, "sqm", "demo_conc_sqm"));  
            skusToInsert.Add(new SkuDef(demoId, "DEMO-FLOOR-TILE", "Къртене на подови настилки/замазка", "Премахване на настилки.", 15m, "sqm", "demo_floor_sqm"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-DISPOSAL", "Контейнер за строителни отпадъци", "Наемане на строителен контейнер и такса смет.", 150m, "pcs", "if(Contains(demo_disposal, 'Да'), Ceiling((if(Contains(demo_what, 'Цяла баня'), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)), 0)"));
            skusToInsert.Add(new SkuDef(demoId, "DEMO-LABOR-STAIRS", "Сваляне на отпадъци по стълби", "Ръчен труд при липса на асансьор (цена на етаж за всеки контейнер).", 10m, "floors", "if(Contains(demo_disposal, 'Да') && Contains(global_logistics, 'Няма асансьор'), Ceiling((if(Contains(demo_what, 'Цяла баня'), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)) * global_floor, 0)"));
        }

        // Sync SKUs
        int processedCount = 0;
        foreach (var sku in skusToInsert) {
            bool exists = false;
            using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"ServiceSkus\" WHERE \"SkuCode\" = @code;", conn)) {
                checkCmd.Parameters.AddWithValue("code", sku.SkuCode);
                exists = Convert.ToInt64(checkCmd.ExecuteScalar()) > 0;
            }

            if (!exists) {
                using (var insCmd = new NpgsqlCommand(
                    "INSERT INTO \"ServiceSkus\" (\"Id\", \"ServiceCategoryId\", \"SkuCode\", \"Name\", \"Description\", \"BasePrice\", \"UnitType\", \"CalculationFormula\", \"CreatedAt\", \"UpdatedAt\") " +
                    "VALUES (@id, @catId, @code, @name, @desc, @price, @unit, @formula, now(), now());", conn)) {
                    insCmd.Parameters.AddWithValue("id", Guid.NewGuid());
                    insCmd.Parameters.AddWithValue("catId", sku.CategoryId);
                    insCmd.Parameters.AddWithValue("code", sku.SkuCode);
                    insCmd.Parameters.AddWithValue("name", sku.Name);
                    insCmd.Parameters.AddWithValue("desc", sku.Description);
                    insCmd.Parameters.AddWithValue("price", sku.Price);
                    insCmd.Parameters.AddWithValue("unit", sku.Unit);
                    insCmd.Parameters.AddWithValue("formula", sku.Formula);
                    insCmd.ExecuteNonQuery();
                }
                Console.WriteLine($"Inserted SKU: '{sku.SkuCode}' - '{sku.Name}'");
            } else {
                using (var updCmd = new NpgsqlCommand(
                    "UPDATE \"ServiceSkus\" SET \"ServiceCategoryId\" = @catId, \"Name\" = @name, \"Description\" = @desc, \"BasePrice\" = @price, \"UnitType\" = @unit, \"CalculationFormula\" = @formula, \"UpdatedAt\" = now() WHERE \"SkuCode\" = @code;", conn)) {
                    updCmd.Parameters.AddWithValue("catId", sku.CategoryId);
                    updCmd.Parameters.AddWithValue("name", sku.Name);
                    updCmd.Parameters.AddWithValue("desc", sku.Description);
                    updCmd.Parameters.AddWithValue("price", sku.Price);
                    updCmd.Parameters.AddWithValue("unit", sku.Unit);
                    updCmd.Parameters.AddWithValue("formula", sku.Formula);
                    updCmd.Parameters.AddWithValue("code", sku.SkuCode);
                    updCmd.ExecuteNonQuery();
                }
                Console.WriteLine($"Updated SKU: '{sku.SkuCode}' - '{sku.Name}'");
            }
            processedCount++;
        }

        Console.WriteLine($"\nSuccessfully synced {processedCount} SKUs and formulas.");

    } catch (Exception ex) {
        Console.WriteLine($"Error occurred: {ex.Message}");
        Environment.Exit(1);
    }
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
