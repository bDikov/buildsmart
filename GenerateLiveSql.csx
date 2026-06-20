using System;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;

string jsonPath = @"Categories_Seed_Templates.json";
if (!File.Exists(jsonPath)) {
    Console.WriteLine("Error: Categories_Seed_Templates.json not found!");
    Environment.Exit(1);
}

var sqlBuilder = new StringBuilder();
sqlBuilder.AppendLine("-- BUILD SMART LIVE DATABASE SYNC SCRIPT");
sqlBuilder.AppendLine("-- Run this file in pgAdmin or any SQL client connected to your live database.\n");

// 1. Rename/Merge Categories
sqlBuilder.AppendLine("\n-- 1. CLEANUP AND MERGE CATEGORIES WITH SUFFIXES");
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
    var suffix = entry.Key;
    var clean = entry.Value;
    
    sqlBuilder.AppendLine($"DO $$");
    sqlBuilder.AppendLine($"DECLARE");
    sqlBuilder.AppendLine($"    suffix_id UUID;");
    sqlBuilder.AppendLine($"    clean_id UUID;");
    sqlBuilder.AppendLine($"BEGIN");
    sqlBuilder.AppendLine($"    SELECT \"Id\" INTO suffix_id FROM \"ServiceCategories\" WHERE \"Name\" = '{suffix}';");
    sqlBuilder.AppendLine($"    SELECT \"Id\" INTO clean_id FROM \"ServiceCategories\" WHERE \"Name\" = '{clean}';");
    sqlBuilder.AppendLine($"    ");
    sqlBuilder.AppendLine($"    IF suffix_id IS NOT NULL THEN");
    sqlBuilder.AppendLine($"        IF clean_id IS NULL THEN");
    sqlBuilder.AppendLine($"            -- Rename suffix to clean");
    sqlBuilder.AppendLine($"            UPDATE \"ServiceCategories\" SET \"Name\" = '{clean}', \"UpdatedAt\" = now() WHERE \"Id\" = suffix_id;");
    sqlBuilder.AppendLine($"            RAISE NOTICE 'Renamed category % to %', '{suffix}', '{clean}';");
    sqlBuilder.AppendLine($"        ELSE");
    sqlBuilder.AppendLine($"            -- Merge relations");
    sqlBuilder.AppendLine($"            UPDATE \"ServiceSkus\" SET \"ServiceCategoryId\" = clean_id, \"UpdatedAt\" = now() WHERE \"ServiceCategoryId\" = suffix_id;");
    sqlBuilder.AppendLine($"            ");
    sqlBuilder.AppendLine($"            -- Merge TradesmanSkills (ignore duplicates)");
    sqlBuilder.AppendLine($"            INSERT INTO \"TradesmanSkills\" (\"TradesmanProfileId\", \"ServiceCategoryId\")");
    sqlBuilder.AppendLine($"            SELECT \"TradesmanProfileId\", clean_id FROM \"TradesmanSkills\" WHERE \"ServiceCategoryId\" = suffix_id");
    sqlBuilder.AppendLine($"            ON CONFLICT DO NOTHING;");
    sqlBuilder.AppendLine($"            ");
    sqlBuilder.AppendLine($"            DELETE FROM \"TradesmanSkills\" WHERE \"ServiceCategoryId\" = suffix_id;");
    sqlBuilder.AppendLine($"            UPDATE \"TradesmanMedia\" SET \"ServiceCategoryId\" = clean_id, \"UpdatedAt\" = now() WHERE \"ServiceCategoryId\" = suffix_id;");
    sqlBuilder.AppendLine($"            UPDATE \"JobPosts\" SET \"ServiceCategoryId\" = clean_id, \"UpdatedAt\" = now() WHERE \"ServiceCategoryId\" = suffix_id;");
    sqlBuilder.AppendLine($"            DELETE FROM \"ServiceCategories\" WHERE \"Id\" = suffix_id;");
    sqlBuilder.AppendLine($"            RAISE NOTICE 'Merged duplicate category % into %', '{suffix}', '{clean}';");
    sqlBuilder.AppendLine($"        END IF;");
    sqlBuilder.AppendLine($"    END IF;");
    sqlBuilder.AppendLine($"END $$;");
}

// 2. Insert/Update category templates
sqlBuilder.AppendLine("\n-- 2. SYNC CATEGORIES AND TEMPLATE STRUCTURES");
string jsonContent = File.ReadAllText(jsonPath);
using (var doc = JsonDocument.Parse(jsonContent)) {
    foreach (var category in doc.RootElement.EnumerateObject()) {
        string catKey = category.Name;
        string catName = category.Value.GetProperty("name").GetString();
        bool isGlobal = catKey == "global_category";
        string templateStructure = category.Value.GetProperty("templateStructure").GetRawText();
        
        // Escape single quotes for SQL
        string escapedTemplate = templateStructure.Replace("'", "''");
        
        sqlBuilder.AppendLine($"DO $$");
        sqlBuilder.AppendLine($"DECLARE");
        sqlBuilder.AppendLine($"    cat_id UUID;");
        sqlBuilder.AppendLine($"BEGIN");
        sqlBuilder.AppendLine($"    SELECT \"Id\" INTO cat_id FROM \"ServiceCategories\" WHERE \"Name\" = '{catName}';");
        sqlBuilder.AppendLine($"    IF cat_id IS NULL THEN");
        sqlBuilder.AppendLine($"        INSERT INTO \"ServiceCategories\" (\"Id\", \"Name\", \"Status\", \"IsGlobal\", \"TemplateStructure\", \"CreatedAt\", \"UpdatedAt\")");
        sqlBuilder.AppendLine($"        VALUES (gen_random_uuid(), '{catName}', 1, {isGlobal.ToString().ToLower()}, '{escapedTemplate}'::jsonb, now(), now());");
        sqlBuilder.AppendLine($"    ELSE");
        sqlBuilder.AppendLine($"        UPDATE \"ServiceCategories\"");
        sqlBuilder.AppendLine($"        SET \"TemplateStructure\" = '{escapedTemplate}'::jsonb, \"IsGlobal\" = {isGlobal.ToString().ToLower()}, \"UpdatedAt\" = now()");
        sqlBuilder.AppendLine($"        WHERE \"Id\" = cat_id;");
        sqlBuilder.AppendLine($"    END IF;");
        sqlBuilder.AppendLine($"END $$;");
    }
}

// 3. Sync SKUs and Formulas
sqlBuilder.AppendLine("\n-- 3. SYNC SERVICE SKUS AND CALCULATION FORMULAS");

// Helper to write a SKU insertion block
void WriteSkuBlock(string catName, string code, string name, string desc, decimal price, string unit, string formula) {
    string escName = name.Replace("'", "''");
    string escDesc = desc.Replace("'", "''");
    string escFormula = formula.Replace("'", "''");
    
    sqlBuilder.AppendLine($"DO $$");
    sqlBuilder.AppendLine($"DECLARE");
    sqlBuilder.AppendLine($"    cat_id UUID;");
    sqlBuilder.AppendLine($"    sku_id UUID;");
    sqlBuilder.AppendLine($"BEGIN");
    sqlBuilder.AppendLine($"    SELECT \"Id\" INTO cat_id FROM \"ServiceCategories\" WHERE \"Name\" = '{catName}';");
    sqlBuilder.AppendLine($"    IF cat_id IS NOT NULL THEN");
    sqlBuilder.AppendLine($"        SELECT \"Id\" INTO sku_id FROM \"ServiceSkus\" WHERE \"SkuCode\" = '{code}';");
    sqlBuilder.AppendLine($"        IF sku_id IS NULL THEN");
    sqlBuilder.AppendLine($"            INSERT INTO \"ServiceSkus\" (\"Id\", \"ServiceCategoryId\", \"SkuCode\", \"Name\", \"Description\", \"BasePrice\", \"UnitType\", \"CalculationFormula\", \"CreatedAt\", \"UpdatedAt\")");
    sqlBuilder.AppendLine($"            VALUES (gen_random_uuid(), cat_id, '{code}', '{escName}', '{escDesc}', {price}, '{unit}', '{escFormula}', now(), now());");
    sqlBuilder.AppendLine($"        ELSE");
    sqlBuilder.AppendLine($"            UPDATE \"ServiceSkus\"");
    sqlBuilder.AppendLine($"            SET \"ServiceCategoryId\" = cat_id, \"Name\" = '{escName}', \"Description\" = '{escDesc}', \"BasePrice\" = {price}, \"UnitType\" = '{unit}', \"CalculationFormula\" = '{escFormula}', \"UpdatedAt\" = now()");
    sqlBuilder.AppendLine($"            WHERE \"Id\" = sku_id;");
    sqlBuilder.AppendLine($"        END IF;");
    sqlBuilder.AppendLine($"    ELSE");
    sqlBuilder.AppendLine($"        RAISE WARNING 'Category % not found when inserting SKU %', '{catName}', '{code}';");
    sqlBuilder.AppendLine($"    END IF;");
    sqlBuilder.AppendLine($"END $$;");
}

// Global Questions
WriteSkuBlock("Global Questions", "GEN-001", "Site Prep & Protection", "Preparation, protection, and logistics.", 50, "Flat", "1");
WriteSkuBlock("Global Questions", "GEN-002", "Final Cleaning", "Complete final cleaning after works.", 2, "sqm", "1");
WriteSkuBlock("Global Questions", "GEN-003", "Daily Cleaning", "Daily site cleaning.", 30, "Flat", "1");

// Electrical
WriteSkuBlock("Електрическа Инсталация", "ELEC-CABLE-LAY", "Полагане на силов кабел", "Издърпване и фиксиране на кабел.", 2, "m", "if(Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, if(Contains(elec_scope, 'Частичен'), global_total_sqm * 1.0, 0))");
WriteSkuBlock("Електрическа Инсталация", "ELEC-CABLE-HEAVY", "Полагане на мощен кабел", "Дебел кабел за проточни бойлери.", 5, "m", "(Count(elec_heavy_appliances) + elec_ac_count) * 10");
WriteSkuBlock("Електрическа Инсталация", "ELEC-CHASE-CONC", "Къртене на канал в бетон", "Изкопаване на канал в бетон.", 15, "m", "if(Contains(global_wall_material, 'Бетон') && Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)");
WriteSkuBlock("Електрическа Инсталация", "ELEC-CHASE-BRICK", "Къртене на канал в тухла", "Изкопаване на канал в тухла.", 8, "m", "if(Contains(global_wall_material, 'Тухла') && Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)");
WriteSkuBlock("Електрическа Инсталация", "ELEC-LAY-TUBE", "Полагане на гофре", "Полагане на гофрирана тръба.", 4, "m", "if(Contains(elec_scope, 'Цялостна'), global_total_sqm * 3.5, 0)");
WriteSkuBlock("Електрическа Инсталация", "ELEC-PANEL-MOD", "Сглобяване на табло (на модул)", "Подреждане на предпазители.", 15, "module", "12 + Count(elec_heavy_appliances) + elec_ac_count");
WriteSkuBlock("Електрическа Инсталация", "ELEC-PANEL-NICHE", "Изкопаване на ниша за вградено табло", "Скрит монтаж.", 95, "pcs", "if(Contains(elec_panel, 'скрито'), 1, 0)");
WriteSkuBlock("Електрическа Инсталация", "ELEC-POINT-STD", "Изграждане на излазна точка", "Труд за 1 брой контакт/ключ.", 35, "pcs", "if(Contains(elec_outlets_comfort, 'Базово'), (global_room_count * 3) + 4, if(Contains(elec_outlets_comfort, 'Комфорт'), (global_room_count * 5) + 6, if(Contains(elec_outlets_comfort, 'Премиум'), (global_room_count * 8) + 10, 0)))");
WriteSkuBlock("Електрическа Инсталация", "ELEC-POINT-LV", "Слаботокова точка", "LAN/TV/СОТ.", 30, "pcs", "elec_lv_count");
WriteSkuBlock("Електрическа Инсталация", "ELEC-POINT-DEV", "Девиаторна точка", "Девиаторни ключове.", 55, "pcs", "elec_dev_count");
WriteSkuBlock("Електрическа Инсталация", "ELEC-POINT-SPEC", "Извод за щори/вентилатор", "Вентилатори или щори.", 40, "pcs", "elec_spec_count");
WriteSkuBlock("Електрическа Инсталация", "ELEC-LED-TRAFO", "Монтаж на захранващ блок (Траф) за LED", "Трансформатор.", 30, "pcs", "if(Contains(elec_lighting, 'LED'), 1, 0)");

// Painting
WriteSkuBlock("Бояджийски и шпакловъчни услуги", "PANT-PRIMER", "Дълбокопроникващ грунд", "Грундиране на стени и тавани.", 3, "sqm", "if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)");
WriteSkuBlock("Бояджийски и шпакловъчни услуги", "PANT-SPACKLE-STD", "Шпакловка (Стандартна 2 ръце)", "Цялостна шпакловка.", 14, "sqm", "if(Contains(paint_tasks, 'Цялостна шпакловка') || Contains(paint_tasks, 'Сваляне на тапети'), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5), 0)");
WriteSkuBlock("Бояджийски и шпакловъчни услуги", "PANT-SPACKLE-Q5", "Фина шпакловка (Перфектна Q5)", "Шитрок за идеално гладка повърхност.", 20, "sqm", "if(Contains(paint_finish_level, 'Q5') || Contains(paint_finish_level, 'Перфектно'), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5), 0)");
WriteSkuBlock("Бояджийски и шпакловъчни услуги", "PANT-PAINT-WHITE", "Боядисване в бяло (2 ръце)", "Боядисване с бял латекс.", 6.50m, "sqm", "if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)");
WriteSkuBlock("Бояджийски и шпакловъчни услуги", "PANT-PAINT-COLOR", "Боядисване в цвят (2 ръце)", "Боядисване с цветен латекс.", 8.50m, "sqm", "if(Contains(paint_colors, 'цвят') && !Contains(paint_colors, 'бяло'), if(paint_sqm > 0, paint_sqm * 0.5, global_total_sqm * 1.2), 0)");
WriteSkuBlock("Бояджийски и шпакловъчни услуги", "PANT-TAPE-CORNER", "Поставяне на ъглохранители", "Алуминиеви или PVC ъгли.", 6, "m", "if(paint_sqm > 0, paint_sqm * 0.1, global_total_sqm * 0.25)");
WriteSkuBlock("Бояджийски и шпакловъчни услуги", "PANT-TRIM", "Боядисване на врати / первази", "Боядисване на декоративни елементи.", 45, "pcs", "if(Contains(paint_trim_doors_count, '4+'), 4, if(Contains(paint_trim_doors_count, '3'), 3, if(Contains(paint_trim_doors_count, '2'), 2, if(Contains(paint_trim_doors_count, '1'), 1, 0))))");
WriteSkuBlock("Бояджийски и шпакловъчни услуги", "PANT-WALLPAPER-REMOVE", "Сваляне на стари тапети", "Сваляне на стари тапети.", 4.50m, "sqm", "if(Contains(paint_tasks, 'Сваляне на тапети'), if(paint_sqm > 0, paint_sqm * 0.5, global_total_sqm), 0)");

// Drywall
WriteSkuBlock("Сухо строителство", "DRYW-CEILING-STD", "Окачен таван (Едно ниво)", "Монтаж на окачен таван.", 45, "sqm", "dryw_ceiling_sqm");
WriteSkuBlock("Сухо строителство", "DRYW-WALL-PARTITION", "Преградна стена (Двуслойна)", "Изграждане на преградна стена.", 65, "sqm", "dryw_partition_sqm");
WriteSkuBlock("Сухо строителство", "DRYW-WALL-LINING", "Предстенна обшивка", "Монтаж на предстенна обшивка.", 40, "sqm", "dryw_lining_sqm");
WriteSkuBlock("Сухо строителство", "DRYW-BOX", "Изграждане на куфари (Кутии)", "Обличане на тръби.", 40, "m", "dryw_box_m");
WriteSkuBlock("Сухо строителство", "DRYW-INSUL-CEILING", "Монтаж на вата (Тавани)", "Поставяне на минерална или каменна вата в окачен таван.", 10, "sqm", "if(Contains(drywall_insulation, 'Да') && Contains(dryw_insulation_areas, 'тавани'), dryw_ceiling_sqm, 0)");
WriteSkuBlock("Сухо строителство", "DRYW-INSUL-PARTITION", "Монтаж на вата (Преградни стени)", "Поставяне на минерална или каменна вата в преградни стени.", 10, "sqm", "if(Contains(drywall_insulation, 'Да') && Contains(dryw_insulation_areas, 'стените'), dryw_partition_sqm, 0)");
WriteSkuBlock("Сухо строителство", "DRYW-INSUL-LINING", "Монтаж на вата (Предстенна обшивка)", "Поставяне на минерална или каменна вата в предстенни обшивки.", 10, "sqm", "if(Contains(drywall_insulation, 'Да') && Contains(dryw_insulation_areas, 'стените'), dryw_lining_sqm, 0)");
WriteSkuBlock("Сухо строителство", "DRYW-INSUL-BOX", "Монтаж на вата (Куфари)", "Поставяне на минерална или каменна вата в куфари.", 10, "m", "if(Contains(drywall_insulation, 'Да') && Contains(dryw_insulation_areas, 'куфарите'), dryw_box_m, 0)");

// Tiling
WriteSkuBlock("Подови и стенни настилки", "TILE-STD", "Лепене на стандартни плочки", "Полагане на фаянс или теракот.", 60, "sqm", "tile_std_sqm");
WriteSkuBlock("Подови и стенни настилки", "TILE-LARGE", "Лепене на голямоформатен гранитогрес", "Плочи над 60х120 см.", 95, "sqm", "tile_large_sqm");
WriteSkuBlock("Подови и стенни настилки", "TILE-PREP-LEVEL", "Саморазливна замазка", "Изравняване на пода.", 22, "sqm", "tile_prep_level_sqm");
WriteSkuBlock("Подови и стенни настилки", "TILE-PREP-HYDRO", "Полагане на хидроизолация (с лента)", "Запечатване на мокри помещения.", 30, "sqm", "tile_prep_hydro_sqm");
WriteSkuBlock("Подови и стенни настилки", "TILE-LAMINATE", "Монтаж на ламинат", "Полагане на ламиниран паркет.", 6, "sqm", "tile_laminate_sqm");

// Microcement
WriteSkuBlock("Микроцимент", "MICRO-STD", "Полагане на микроцимент (сухи зони)", "Полагане на микроцимент.", 140, "sqm", "if(Contains(mico_area, 'Сухи зони'), if(Contains(mico_rooms, '1-2 стаи'), 30, global_total_sqm * 0.8), 0)");
WriteSkuBlock("Микроцимент", "MICRO-BATH", "Полагане на микроцимент в мокри зони (Баня)", "Микроцимент за баня.", 180, "sqm", "if(Contains(mico_area, 'Мокри зони'), global_bathroom_count * 20, 0)");

// Plumbing
WriteSkuBlock("ВиК Услуги", "PLMB-POINT-NEW", "Изграждане на нова ВиК точка", "Тръби за топла, студена вода и канал.", 70, "pcs", "if(Contains(plumb_scope, 'Цялостна'), (global_bathroom_count * 5) + 3, if(Contains(plumb_scope, 'извеждане'), 3, 0))");
WriteSkuBlock("ВиК Услуги", "PLMB-RISER-REPLACE", "Смяна на вертикален щранг", "Подмяна на основните метални тръби.", 240, "pcs", "if(Contains(plumb_riser, 'Да'), global_bathroom_count, 0)");
WriteSkuBlock("ВиК Услуги", "PLMB-SINK-INSTALL", "Монтаж на мивка със смесител и сифон", "Монтаж на мивки.", 90, "pcs", "if(Contains(plumb_sink_count, '3+'), 3, if(Contains(plumb_sink_count, '2'), 2, if(Contains(plumb_sink_count, '1'), 1, 0)))");
WriteSkuBlock("ВиК Услуги", "PLMB-WC-STD", "Монтаж на стандартна тоалетна (моноблок)", "Монтаж на тоалетна.", 140, "pcs", "if(Contains(plumb_wc_type, 'Стандартна'), global_bathroom_count, 0)");
WriteSkuBlock("ВиК Услуги", "PLMB-WC-BUILTIN", "Монтаж на структура за вграждане", "Конзолна тоалетна.", 190, "pcs", "if(Contains(plumb_wc_type, 'Вградена'), global_bathroom_count, 0)");
WriteSkuBlock("ВиК Услуги", "PLMB-SHOWER-CABIN", "Монтаж на душ кабина или стъклен параван", "Душ кабина.", 330, "pcs", "if(Contains(plumb_shower_type, 'кабина') || Contains(plumb_shower_type, 'Вана'), global_bathroom_count, 0)");
WriteSkuBlock("ВиК Услуги", "PLMB-SHOWER-FIXTURE", "Монтаж на душ система", "Душ батерия.", 70, "pcs", "if(Contains(plumb_shower_type, 'Само') || Contains(plumb_shower_type, 'кабина') || Contains(plumb_shower_type, 'Вана'), global_bathroom_count, 0)");
WriteSkuBlock("ВиК Услуги", "PLMB-BOILER", "Монтаж на електрически бойлер", "Бойлер до 100л.", 140, "pcs", "if(Contains(plumb_appliances, 'бойлер'), 1, 0)");
WriteSkuBlock("ВиК Услуги", "PLMB-APPLIANCE", "Свързване на пералня / съдомиялна", "Уреди.", 80, "pcs", "if(Contains(plumb_appliances, 'Пералня') && Contains(plumb_appliances, 'Съдомиялна'), 2, if(Contains(plumb_appliances, 'Пералня') || Contains(plumb_appliances, 'Съдомиялна'), 1, 0))");
WriteSkuBlock("ВиК Услуги", "PLMB-METER-REPLACE", "Смяна на водомер", "Нов водомер.", 60, "pcs", "global_bathroom_count * 2");

// Demolition
WriteSkuBlock("Къртене и извозване", "DEMO-BATH-FULL", "Цялостно къртене на баня", "Къртене на баня.", 750, "pcs", "if(Contains(demo_what, 'Цяла баня'), global_bathroom_count, 0)");
WriteSkuBlock("Къртене и извозване", "DEMO-WALL-BRICK", "Къртене на тухлена стена", "Събаряне на тухлени стени.", 20, "sqm", "demo_brick_sqm");
WriteSkuBlock("Къртене и извозване", "DEMO-WALL-CONC", "Къртене на бетонна стена/панел", "Къртене на бетон.", 50, "sqm", "demo_conc_sqm");
WriteSkuBlock("Къртене и извозване", "DEMO-FLOOR-TILE", "Къртене на подови настилки/замазка", "Премахване на настилки.", 15, "sqm", "demo_floor_sqm");
WriteSkuBlock("Къртене и извозване", "DEMO-DISPOSAL", "Контейнер за строителни отпадъци", "Наемане на строителен контейнер и такса смет.", 150, "pcs", "if(Contains(demo_disposal, 'Да'), Ceiling((if(Contains(demo_what, 'Цяла баня'), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)), 0)");
WriteSkuBlock("Къртене и извозване", "DEMO-LABOR-STAIRS", "Сваляне на отпадъци по стълби", "Ръчен труд при липса на асансьор (цена на етаж за всеки контейнер).", 10, "floors", "if(Contains(demo_disposal, 'Да') && Contains(global_logistics, 'Няма асансьор'), Ceiling((if(Contains(demo_what, 'Цяла баня'), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)) * global_floor, 0)");

string targetFile = @"SyncLiveDb.sql";
File.WriteAllText(targetFile, sqlBuilder.ToString());
Console.WriteLine($"SQL sync file generated successfully at {targetFile}");
