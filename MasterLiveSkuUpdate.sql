-- RUN THIS ENTIRE SCRIPT ON THE LIVE (PRODUCTION) DATABASE --

-- ==========================================
-- 1. TILING SKUS
-- ==========================================
UPDATE "ServiceSkus" SET "CalculationFormula" = 'tile_std_sqm', "BasePrice" = 30.00, "UpdatedAt" = now() WHERE "SkuCode" = 'TILE-STD';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'tile_large_sqm', "BasePrice" = 45.00, "UpdatedAt" = now() WHERE "SkuCode" = 'TILE-LARGE';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'tile_laminate_sqm', "BasePrice" = 6.00, "UpdatedAt" = now() WHERE "SkuCode" = 'TILE-LAMINATE';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'tile_prep_level_sqm', "BasePrice" = 22.00, "UpdatedAt" = now() WHERE "SkuCode" = 'TILE-PREP-LEVEL';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'tile_prep_hydro_sqm', "BasePrice" = 30.00, "UpdatedAt" = now() WHERE "SkuCode" = 'TILE-PREP-HYDRO';

-- ==========================================
-- 2. DRYWALL SKUS
-- ==========================================
UPDATE "ServiceSkus" SET "CalculationFormula" = 'dryw_ceiling_sqm', "BasePrice" = 45.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-CEILING-STD';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'dryw_partition_sqm', "BasePrice" = 65.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-WALL-PARTITION';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'dryw_lining_sqm', "BasePrice" = 40.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-WALL-LINING';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'dryw_box_m', "BasePrice" = 40.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-BOX';

-- Disable old combined drywall insulation
UPDATE "ServiceSkus" SET "CalculationFormula" = '0', "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-INSULATION';

-- Insert or Update new split insulation SKUs
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-INSUL-CEILING') THEN
        UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''тавани''), dryw_ceiling_sqm, 0)', "BasePrice" = 10.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-INSUL-CEILING';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") SELECT gen_random_uuid(), "Id", 'DRYW-INSUL-CEILING', 'Монтаж на вата (Тавани)', 'Поставяне на минерална или каменна вата в окачен таван.', 10.00, 'sqm', 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''тавани''), dryw_ceiling_sqm, 0)', now(), now() FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;

    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-INSUL-PARTITION') THEN
        UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''стените''), dryw_partition_sqm, 0)', "BasePrice" = 10.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-INSUL-PARTITION';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") SELECT gen_random_uuid(), "Id", 'DRYW-INSUL-PARTITION', 'Монтаж на вата (Преградни стени)', 'Поставяне на минерална или каменна вата в преградни стени.', 10.00, 'sqm', 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''стените''), dryw_partition_sqm, 0)', now(), now() FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;

    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-INSUL-LINING') THEN
        UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''стените''), dryw_lining_sqm, 0)', "BasePrice" = 10.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-INSUL-LINING';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") SELECT gen_random_uuid(), "Id", 'DRYW-INSUL-LINING', 'Монтаж на вата (Предстенна обшивка)', 'Поставяне на минерална или каменна вата в предстенни обшивки.', 10.00, 'sqm', 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''стените''), dryw_lining_sqm, 0)', now(), now() FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;

    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-INSUL-BOX') THEN
        UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''куфарите''), dryw_box_m, 0)', "BasePrice" = 10.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-INSUL-BOX';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") SELECT gen_random_uuid(), "Id", 'DRYW-INSUL-BOX', 'Монтаж на вата (Куфари)', 'Поставяне на минерална или каменна вата в куфари.', 10.00, 'm', 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''куфарите''), dryw_box_m, 0)', now(), now() FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

-- ==========================================
-- 3. DEMOLITION SKUS
-- ==========================================
UPDATE "ServiceSkus" SET "CalculationFormula" = 'demo_brick_sqm', "BasePrice" = 20.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DEMO-WALL-BRICK';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'demo_conc_sqm', "BasePrice" = 50.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DEMO-WALL-CONC';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'demo_floor_sqm', "BasePrice" = 15.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DEMO-FLOOR-TILE';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(demo_disposal, ''Да''), Ceiling((if(Contains(demo_what, ''Цяла баня''), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)), 0)', "BasePrice" = 150.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DEMO-DISPOSAL';

-- Insert or Update Labor Stairs
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-LABOR-STAIRS') THEN
        UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(demo_disposal, ''Да'') && Contains(global_logistics, ''Няма асансьор''), Ceiling((if(Contains(demo_what, ''Цяла баня''), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)) * global_floor, 0)', "BasePrice" = 10.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DEMO-LABOR-STAIRS';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") SELECT gen_random_uuid(), "Id", 'DEMO-LABOR-STAIRS', 'Сваляне на отпадъци по стълби', 'Ръчен труд при липса на асансьор (цена на етаж за всеки контейнер).', 10.00, 'floors', 'if(Contains(demo_disposal, ''Да'') && Contains(global_logistics, ''Няма асансьор''), Ceiling((if(Contains(demo_what, ''Цяла баня''), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)) * global_floor, 0)', now(), now() FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;