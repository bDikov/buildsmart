
-- =========================================================================
-- BUILDSMART MASTER LIVE DATABASE UPDATE (2026 EDITION) - THE ULTIMATE FIX
-- =========================================================================

-- 1. STRIP ENGLISH TAGS
UPDATE "ServiceCategories" SET "Name" = REPLACE("Name", ' (Electrical)', '');
UPDATE "ServiceCategories" SET "Name" = REPLACE("Name", ' (Plumbing)', '');
UPDATE "ServiceCategories" SET "Name" = REPLACE("Name", ' (Painting)', '');
UPDATE "ServiceCategories" SET "Name" = REPLACE("Name", ' (Demolition)', '');
UPDATE "ServiceCategories" SET "Name" = REPLACE("Name", ' (Drywall)', '');
UPDATE "ServiceCategories" SET "Name" = REPLACE("Name", ' (Tiling)', '');
UPDATE "ServiceCategories" SET "Name" = REPLACE("Name", ' (Microcement)', '');

UPDATE "ServiceCategoryTranslations" SET "Name" = REPLACE("Name", ' (Electrical)', '');
UPDATE "ServiceCategoryTranslations" SET "Name" = REPLACE("Name", ' (Plumbing)', '');
UPDATE "ServiceCategoryTranslations" SET "Name" = REPLACE("Name", ' (Painting)', '');
UPDATE "ServiceCategoryTranslations" SET "Name" = REPLACE("Name", ' (Demolition)', '');
UPDATE "ServiceCategoryTranslations" SET "Name" = REPLACE("Name", ' (Drywall)', '');
UPDATE "ServiceCategoryTranslations" SET "Name" = REPLACE("Name", ' (Tiling)', '');
UPDATE "ServiceCategoryTranslations" SET "Name" = REPLACE("Name", ' (Microcement)', '');

-- 2. UPSERT SKUS WITH EURO PRICING & ROBUST MATH FORMULAS

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CABLE-LAY') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.02, 
            "CalculationFormula" = 'global_total_sqm * 3.5', 
            "UnitType" = 'm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-CABLE-LAY';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-CABLE-LAY', 'Полагане на силов кабел', 'Издърпване и фиксиране на кабел.', 1.02, 'm', 'global_total_sqm * 3.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CABLE-HEAVY') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.56, 
            "CalculationFormula" = '(elec_ac_count * 1) * 10 + 20', 
            "UnitType" = 'm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-CABLE-HEAVY';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-CABLE-HEAVY', 'Полагане на мощен кабел', 'Дебел кабел за проточни бойлери.', 2.56, 'm', '(elec_ac_count * 1) * 10 + 20', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CHASE-CONC') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.67, 
            "CalculationFormula" = 'global_total_sqm * 3.5', 
            "UnitType" = 'm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-CHASE-CONC';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-CHASE-CONC', 'Къртене на канал в бетон', 'Изкопаване на канал в бетон.', 7.67, 'm', 'global_total_sqm * 3.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CHASE-BRICK') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.09, 
            "CalculationFormula" = 'global_total_sqm * 3.5', 
            "UnitType" = 'm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-CHASE-BRICK';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-CHASE-BRICK', 'Къртене на канал в тухла', 'Изкопаване на канал в тухла.', 4.09, 'm', 'global_total_sqm * 3.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-LAY-TUBE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.05, 
            "CalculationFormula" = 'global_total_sqm * 3.5', 
            "UnitType" = 'm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-LAY-TUBE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-LAY-TUBE', 'Полагане на гофре', 'Полагане на гофрирана тръба.', 2.05, 'm', 'global_total_sqm * 3.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-PANEL-MOD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.67, 
            "CalculationFormula" = '12 + elec_ac_count', 
            "UnitType" = 'module', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-PANEL-MOD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-PANEL-MOD', 'Сглобяване на табло (на модул)', 'Подреждане на предпазители.', 7.67, 'module', '12 + elec_ac_count', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-PANEL-NICHE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 48.57, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-PANEL-NICHE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-PANEL-NICHE', 'Изкопаване на ниша за вградено табло', 'Скрит монтаж.', 48.57, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 17.90, 
            "CalculationFormula" = '(global_room_count * 5) + 6', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-POINT-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-POINT-STD', 'Изграждане на излазна точка', 'Труд за 1 брой контакт/ключ.', 17.90, 'pcs', '(global_room_count * 5) + 6', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-LV') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 15.34, 
            "CalculationFormula" = 'global_room_count', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-POINT-LV';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-POINT-LV', 'Слаботокова точка', 'LAN/TV/СОТ.', 15.34, 'pcs', 'global_room_count', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-DEV') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 28.12, 
            "CalculationFormula" = '2', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-POINT-DEV';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-POINT-DEV', 'Девиаторна точка', 'Девиаторни ключове.', 28.12, 'pcs', '2', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-SPEC') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 20.45, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-POINT-SPEC';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-POINT-SPEC', 'Извод за щори/вентилатор', 'Вентилатори или щори.', 20.45, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-LED-TRAFO') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 15.34, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-LED-TRAFO';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-LED-TRAFO', 'Монтаж на захранващ блок (Траф) за LED', 'Трансформатор.', 15.34, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-PRIMER') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.53, 
            "CalculationFormula" = 'global_total_sqm * 2.5', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-PRIMER';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-PRIMER', 'Дълбокопроникващ грунд', 'Грундиране.', 1.53, 'sqm', 'global_total_sqm * 2.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-SPACKLE-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.16, 
            "CalculationFormula" = 'global_total_sqm * 2.5', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-SPACKLE-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-SPACKLE-STD', 'Шпакловка (Стандартна 2 ръце)', 'Цялостна шпакловка.', 7.16, 'sqm', 'global_total_sqm * 2.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-SPACKLE-Q5') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 10.23, 
            "CalculationFormula" = 'global_total_sqm * 2.5', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-SPACKLE-Q5';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-SPACKLE-Q5', 'Фина шпакловка (Перфектна Q5)', 'Шитрок за идеално гладка повърхност.', 10.23, 'sqm', 'global_total_sqm * 2.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-PAINT-WHITE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.32, 
            "CalculationFormula" = 'global_total_sqm * 2.5', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-PAINT-WHITE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-PAINT-WHITE', 'Боядисване в бяло (2 ръце)', 'Боядисване с бял латекс.', 3.32, 'sqm', 'global_total_sqm * 2.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-PAINT-COLOR') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.35, 
            "CalculationFormula" = 'global_total_sqm * 2.5', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-PAINT-COLOR';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-PAINT-COLOR', 'Боядисване в цвят (2 ръце)', 'Боядисване с цветен латекс.', 4.35, 'sqm', 'global_total_sqm * 2.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-TAPE-CORNER') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.07, 
            "CalculationFormula" = 'global_room_count * 5', 
            "UnitType" = 'm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-TAPE-CORNER';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-TAPE-CORNER', 'Поставяне на ъглохранители', 'Алуминиеви или PVC ъгли.', 3.07, 'm', 'global_room_count * 5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-TRIM') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 23.01, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-TRIM';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-TRIM', 'Боядисване на врати / первази', 'Декоративни елементи.', 23.01, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-WALLPAPER-REMOVE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.30, 
            "CalculationFormula" = 'global_total_sqm * 2.5', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-WALLPAPER-REMOVE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-WALLPAPER-REMOVE', 'Сваляне на стари тапети', 'Сваляне на стари тапети.', 2.30, 'sqm', 'global_total_sqm * 2.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-CEILING-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 23.01, 
            "CalculationFormula" = 'global_total_sqm', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-CEILING-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-CEILING-STD', 'Окачен таван (Едно ниво)', 'Монтаж на окачен таван.', 23.01, 'sqm', 'global_total_sqm', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-WALL-PARTITION') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 33.23, 
            "CalculationFormula" = 'global_total_sqm * 0.3', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-WALL-PARTITION';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-WALL-PARTITION', 'Преградна стена (Двуслойна)', 'Изграждане на преградна стена.', 33.23, 'sqm', 'global_total_sqm * 0.3', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-WALL-LINING') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 20.45, 
            "CalculationFormula" = 'global_total_sqm * 0.3', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-WALL-LINING';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-WALL-LINING', 'Предстенна обшивка', 'Монтаж на предстенна обшивка.', 20.45, 'sqm', 'global_total_sqm * 0.3', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-BOX') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 20.45, 
            "CalculationFormula" = 'global_bathroom_count * 3', 
            "UnitType" = 'm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-BOX';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-BOX', 'Изграждане на куфари (Кутии)', 'Обличане на тръби.', 20.45, 'm', 'global_bathroom_count * 3', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-INSULATION') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 5.11, 
            "CalculationFormula" = 'global_total_sqm', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-INSULATION';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-INSULATION', 'Монтаж на вата (Топло/Шумо)', 'Поставяне на минерална или каменна вата.', 5.11, 'sqm', 'global_total_sqm', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 30.68, 
            "CalculationFormula" = 'global_bathroom_count * 25', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-STD', 'Лепене на стандартни плочки', 'Полагане на фаянс или теракот.', 30.68, 'sqm', 'global_bathroom_count * 25', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-LARGE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 48.57, 
            "CalculationFormula" = 'global_bathroom_count * 25', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-LARGE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-LARGE', 'Лепене на голямоформатен гранитогрес', 'Плочи над 60х120 см.', 48.57, 'sqm', 'global_bathroom_count * 25', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-PREP-LEVEL') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 11.25, 
            "CalculationFormula" = 'global_total_sqm * 0.5', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-PREP-LEVEL';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-PREP-LEVEL', 'Саморазливна замазка', 'Изравняване на пода.', 11.25, 'sqm', 'global_total_sqm * 0.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-PREP-HYDRO') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 15.34, 
            "CalculationFormula" = 'global_bathroom_count * 5', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-PREP-HYDRO';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-PREP-HYDRO', 'Полагане на хидроизолация', 'Запечатване на мокри помещения.', 15.34, 'sqm', 'global_bathroom_count * 5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-LAMINATE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 6.14, 
            "CalculationFormula" = 'global_total_sqm * 0.7', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-LAMINATE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-LAMINATE', 'Монтаж на ламинат', 'Полагане на ламиниран паркет.', 6.14, 'sqm', 'global_total_sqm * 0.7', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'MICRO-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 71.58, 
            "CalculationFormula" = 'global_total_sqm * 0.5', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'MICRO-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'MICRO-STD', 'Полагане на микроцимент (сухи зони)', 'Полагане на микроцимент.', 71.58, 'sqm', 'global_total_sqm * 0.5', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'MICRO-BATH') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 92.03, 
            "CalculationFormula" = 'global_bathroom_count * 25', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'MICRO-BATH';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'MICRO-BATH', 'Полагане на микроцимент в мокри зони (Баня)', 'Микроцимент за баня.', 92.03, 'sqm', 'global_bathroom_count * 25', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-POINT-NEW') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 35.79, 
            "CalculationFormula" = '(global_bathroom_count * 5) + 3', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-POINT-NEW';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-POINT-NEW', 'Изграждане на нова ВиК точка', 'Тръби за топла, студена вода и канал.', 35.79, 'pcs', '(global_bathroom_count * 5) + 3', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-RISER-REPLACE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 122.71, 
            "CalculationFormula" = 'global_bathroom_count', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-RISER-REPLACE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-RISER-REPLACE', 'Смяна на вертикален щранг', 'Подмяна на основните метални тръби.', 122.71, 'pcs', 'global_bathroom_count', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-SINK-INSTALL') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 46.02, 
            "CalculationFormula" = '2', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-SINK-INSTALL';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-SINK-INSTALL', 'Монтаж на мивка със смесител и сифон', 'Монтаж на мивки.', 46.02, 'pcs', '2', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-WC-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 71.58, 
            "CalculationFormula" = 'global_bathroom_count', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-WC-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-WC-STD', 'Монтаж на стандартна тоалетна (моноблок)', 'Монтаж на тоалетна.', 71.58, 'pcs', 'global_bathroom_count', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-WC-BUILTIN') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 97.15, 
            "CalculationFormula" = 'global_bathroom_count', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-WC-BUILTIN';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-WC-BUILTIN', 'Монтаж на структура за вграждане', 'Конзолна тоалетна.', 97.15, 'pcs', 'global_bathroom_count', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-SHOWER-CABIN') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 168.73, 
            "CalculationFormula" = 'global_bathroom_count', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-SHOWER-CABIN';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-SHOWER-CABIN', 'Монтаж на душ кабина или стъклен параван', 'Душ кабина.', 168.73, 'pcs', 'global_bathroom_count', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-SHOWER-FIXTURE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 35.79, 
            "CalculationFormula" = 'global_bathroom_count', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-SHOWER-FIXTURE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-SHOWER-FIXTURE', 'Монтаж на душ система', 'Душ батерия.', 35.79, 'pcs', 'global_bathroom_count', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-BOILER') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 71.58, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-BOILER';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-BOILER', 'Монтаж на електрически бойлер', 'Бойлер до 100л.', 71.58, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-APPLIANCE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 40.90, 
            "CalculationFormula" = '2', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-APPLIANCE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-APPLIANCE', 'Свързване на пералня / съдомиялна', 'Уреди.', 40.90, 'pcs', '2', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-METER-REPLACE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 30.68, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-METER-REPLACE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-METER-REPLACE', 'Смяна на водомер', 'Нов водомер.', 30.68, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-BATH-FULL') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 383.47, 
            "CalculationFormula" = 'global_bathroom_count', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-BATH-FULL';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-BATH-FULL', 'Цялостно къртене на баня', 'Къртене на баня.', 383.47, 'pcs', 'global_bathroom_count', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-WALL-BRICK') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 10.23, 
            "CalculationFormula" = 'global_total_sqm * 0.2', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-WALL-BRICK';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-WALL-BRICK', 'Къртене на тухлена стена', 'Събаряне на тухлени стени.', 10.23, 'sqm', 'global_total_sqm * 0.2', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-WALL-CONC') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 25.56, 
            "CalculationFormula" = 'global_total_sqm * 0.2', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-WALL-CONC';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-WALL-CONC', 'Къртене на бетонна стена/панел', 'Къртене на бетон.', 25.56, 'sqm', 'global_total_sqm * 0.2', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-FLOOR-TILE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.67, 
            "CalculationFormula" = 'global_total_sqm * 0.3', 
            "UnitType" = 'sqm', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-FLOOR-TILE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-FLOOR-TILE', 'Къртене на подови настилки/замазка', 'Премахване на настилки.', 7.67, 'sqm', 'global_total_sqm * 0.3', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-DISPOSAL') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 127.82, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-DISPOSAL';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-DISPOSAL', 'Извозване с контейнер', 'Наемане на строителен контейнер.', 127.82, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;
