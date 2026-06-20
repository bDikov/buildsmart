-- BUILD SMART LIVE DATABASE SYNC SCRIPT
-- Run this file in pgAdmin or any SQL client connected to your live database.


-- 1. CLEANUP AND MERGE CATEGORIES WITH SUFFIXES
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Electrical';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Електрическа Инсталация', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Electrical', 'Електрическа Инсталация';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Electrical', 'Електрическа Инсталация';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Painting';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Бояджийски и шпакловъчни услуги', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Painting', 'Бояджийски и шпакловъчни услуги';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Painting', 'Бояджийски и шпакловъчни услуги';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Plumbing';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'ВиК Услуги', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Plumbing', 'ВиК Услуги';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Plumbing', 'ВиК Услуги';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Demolition';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Къртене и извозване', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Demolition', 'Къртене и извозване';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Demolition', 'Къртене и извозване';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Drywall';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Сухо строителство', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Drywall', 'Сухо строителство';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Drywall', 'Сухо строителство';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Tiling';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Подови и стенни настилки', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Tiling', 'Подови и стенни настилки';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Tiling', 'Подови и стенни настилки';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Microcement';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Микроцимент', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Microcement', 'Микроцимент';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Microcement', 'Микроцимент';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги (Plumbing)';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'ВиК Услуги', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'ВиК Услуги (Plumbing)', 'ВиК Услуги';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'ВиК Услуги (Plumbing)', 'ВиК Услуги';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги (Painting)';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Бояджийски и шпакловъчни услуги', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Бояджийски и шпакловъчни услуги (Painting)', 'Бояджийски и шпакловъчни услуги';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Бояджийски и шпакловъчни услуги (Painting)', 'Бояджийски и шпакловъчни услуги';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване (Demolition)';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Къртене и извозване', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Къртене и извозване (Demolition)', 'Къртене и извозване';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Къртене и извозване (Demolition)', 'Къртене и извозване';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство (Drywall)';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Сухо строителство', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Сухо строителство (Drywall)', 'Сухо строителство';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Сухо строителство (Drywall)', 'Сухо строителство';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки (Tiling)';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Подови и стенни настилки', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Подови и стенни настилки (Tiling)', 'Подови и стенни настилки';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Подови и стенни настилки (Tiling)', 'Подови и стенни настилки';
        END IF;
    END IF;
END $$;
DO $$
DECLARE
    suffix_id UUID;
    clean_id UUID;
BEGIN
    SELECT "Id" INTO suffix_id FROM "ServiceCategories" WHERE "Name" = 'Микроцимент (Microcement)';
    SELECT "Id" INTO clean_id FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    
    IF suffix_id IS NOT NULL THEN
        IF clean_id IS NULL THEN
            -- Rename suffix to clean
            UPDATE "ServiceCategories" SET "Name" = 'Микроцимент', "UpdatedAt" = now() WHERE "Id" = suffix_id;
            RAISE NOTICE 'Renamed category % to %', 'Микроцимент (Microcement)', 'Микроцимент';
        ELSE
            -- Merge relations
            UPDATE "ServiceSkus" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            
            -- Merge TradesmanSkills (ignore duplicates)
            INSERT INTO "TradesmanSkills" ("TradesmanProfileId", "ServiceCategoryId")
            SELECT "TradesmanProfileId", clean_id FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id
            ON CONFLICT DO NOTHING;
            
            DELETE FROM "TradesmanSkills" WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "TradesmanMedia" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            UPDATE "JobPosts" SET "ServiceCategoryId" = clean_id, "UpdatedAt" = now() WHERE "ServiceCategoryId" = suffix_id;
            DELETE FROM "ServiceCategories" WHERE "Id" = suffix_id;
            RAISE NOTICE 'Merged duplicate category % into %', 'Микроцимент (Microcement)', 'Микроцимент';
        END IF;
    END IF;
END $$;

-- 2. SYNC CATEGORIES AND TEMPLATE STRUCTURES
DO $$
DECLARE
    cat_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Global Questions';
    IF cat_id IS NULL THEN
        INSERT INTO "ServiceCategories" ("Id", "Name", "Status", "IsGlobal", "TemplateStructure", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), 'Global Questions', 1, true, '{
      "questions": [
        {
          "id": "global_property_type",
          "text": "Какъв е типът на имота?",
          "type": "choice",
          "required": true,
          "options": [
            "Апартамент",
            "Къща / Вила",
            "Офис / Търговско помещение"
          ]
        },
        {
          "id": "global_total_sqm",
          "text": "Каква е общата квадратура (подова площ) на обекта в кв.м.?",
          "type": "number",
          "required": true
        },
        {
          "id": "global_ceiling_height",
          "text": "Каква е височината на таваните?",
          "type": "choice",
          "required": true,
          "options": [
            "Стандартна (между 2.50м и 2.70м)",
            "Висока (над 2.70м)"
          ]
        },
        {
          "id": "global_room_count",
          "text": "Общ брой сухи помещения (спални, хол, кухня, кабинет)?",
          "type": "number",
          "required": true
        },
        {
          "id": "global_bathroom_count",
          "text": "Колко на брой са мокрите помещения (бани и тоалетни)?",
          "type": "number",
          "required": true
        },
        {
          "id": "global_current_state",
          "text": "Какво е текущото състояние на обекта?",
          "type": "choice",
          "required": true,
          "options": [
            "Ново строителство (на шпакловка и замазка / БДС)",
            "Празно жилище за основен ремонт",
            "Обзаведено жилище (изисква местене и покриване)"
          ]
        },
        {
          "id": "global_logistics",
          "text": "Има ли осигурен достъп и паркомясто за бус/контейнер, както и работещ асансьор за качване на материали?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, има лесен достъп и асансьор",
            "Няма асансьор (качване по стълби)",
            "Труден достъп/Няма паркинг"
          ]
        },
        {
          "id": "global_materials_supply",
          "text": "Кой ще осигури видимите материали (плочки, санитария, ламинат, осветителни тела)?",
          "type": "choice",
          "required": true,
          "options": [
            "Аз ще ги купя (търся само труд)",
            "Искам майсторът да ги достави (по каталог)",
            "Смесено (ще се уговорим допълнително)"
          ]
        },
        {
          "id": "global_protection",
          "text": "Изисква ли се ежедневно почистване и специално покриване/защита на общите части на сградата?",
          "type": "boolean",
          "required": true
        },
        {
          "id": "global_floor",
          "text": "На кой етаж се намира обекта?",
          "type": "number",
          "required": true
        },
        {
          "id": "global_wall_material",
          "text": "Какъв е основният материал на стените?",
          "type": "choice",
          "required": true,
          "options": [
            "Тухла",
            "Бетон / Панел",
            "Гипсокартон"
          ]
        }
      ]
    }'::jsonb, now(), now());
    ELSE
        UPDATE "ServiceCategories"
        SET "TemplateStructure" = '{
      "questions": [
        {
          "id": "global_property_type",
          "text": "Какъв е типът на имота?",
          "type": "choice",
          "required": true,
          "options": [
            "Апартамент",
            "Къща / Вила",
            "Офис / Търговско помещение"
          ]
        },
        {
          "id": "global_total_sqm",
          "text": "Каква е общата квадратура (подова площ) на обекта в кв.м.?",
          "type": "number",
          "required": true
        },
        {
          "id": "global_ceiling_height",
          "text": "Каква е височината на таваните?",
          "type": "choice",
          "required": true,
          "options": [
            "Стандартна (между 2.50м и 2.70м)",
            "Висока (над 2.70м)"
          ]
        },
        {
          "id": "global_room_count",
          "text": "Общ брой сухи помещения (спални, хол, кухня, кабинет)?",
          "type": "number",
          "required": true
        },
        {
          "id": "global_bathroom_count",
          "text": "Колко на брой са мокрите помещения (бани и тоалетни)?",
          "type": "number",
          "required": true
        },
        {
          "id": "global_current_state",
          "text": "Какво е текущото състояние на обекта?",
          "type": "choice",
          "required": true,
          "options": [
            "Ново строителство (на шпакловка и замазка / БДС)",
            "Празно жилище за основен ремонт",
            "Обзаведено жилище (изисква местене и покриване)"
          ]
        },
        {
          "id": "global_logistics",
          "text": "Има ли осигурен достъп и паркомясто за бус/контейнер, както и работещ асансьор за качване на материали?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, има лесен достъп и асансьор",
            "Няма асансьор (качване по стълби)",
            "Труден достъп/Няма паркинг"
          ]
        },
        {
          "id": "global_materials_supply",
          "text": "Кой ще осигури видимите материали (плочки, санитария, ламинат, осветителни тела)?",
          "type": "choice",
          "required": true,
          "options": [
            "Аз ще ги купя (търся само труд)",
            "Искам майсторът да ги достави (по каталог)",
            "Смесено (ще се уговорим допълнително)"
          ]
        },
        {
          "id": "global_protection",
          "text": "Изисква ли се ежедневно почистване и специално покриване/защита на общите части на сградата?",
          "type": "boolean",
          "required": true
        },
        {
          "id": "global_floor",
          "text": "На кой етаж се намира обекта?",
          "type": "number",
          "required": true
        },
        {
          "id": "global_wall_material",
          "text": "Какъв е основният материал на стените?",
          "type": "choice",
          "required": true,
          "options": [
            "Тухла",
            "Бетон / Панел",
            "Гипсокартон"
          ]
        }
      ]
    }'::jsonb, "IsGlobal" = true, "UpdatedAt" = now()
        WHERE "Id" = cat_id;
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NULL THEN
        INSERT INTO "ServiceCategories" ("Id", "Name", "Status", "IsGlobal", "TemplateStructure", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), 'Електрическа Инсталация', 1, false, '{
      "questions": [
        {
          "id": "elec_scope",
          "text": "Какъв е мащабът на ремонта?",
          "type": "choice",
          "required": true,
          "options": [
            "Цялостна подмяна (всичко се изгражда наново)",
            "Частичен ремонт (добавяне/местене на контакти и лампи)",
            "Само монтаж (на ключове, контакти и осветителни тела)"
          ],
          "hintText": "💡 Ако инсталацията ви е над 20 години, препоръчваме цялостна подмяна."
        },
        {
          "id": "elec_heavy_appliances",
          "text": "Кои мощни уреди ще имате? (Изберете всички)",
          "type": "multiselect",
          "required": true,
          "options": [
            "Фурна",
            "Индукционен котлон",
            "Съдомиялна",
            "Пералня",
            "Сушилня",
            "Проточен бойлер"
          ]
        },
        {
          "id": "elec_ac_count",
          "text": "Колко климатика ще се захранват?",
          "type": "choice",
          "required": true,
          "options": [
            "0",
            "1",
            "2",
            "3",
            "4+"
          ]
        },
        {
          "id": "elec_outlets_comfort",
          "text": "Колко контакти желаете във всяка стая?",
          "type": "choice",
          "required": true,
          "options": [
            "Базово (по 3-4 на стая)",
            "Комфорт (по 5-6 на стая)",
            "Премиум (над 8 на стая)"
          ],
          "hintText": "💡 Повечето ни клиенти избират ''Комфорт'', за да избегнат разклонители."
        },
        {
          "id": "elec_lighting",
          "text": "Какъв тип ще е основното осветление?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Стандартно (полилеи/плафони)",
            "Вградени лунички",
            "Скрито LED осветление"
          ]
        },
        {
          "id": "elec_panel",
          "text": "Главното ел. табло ще се подменя ли?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, искам ново скрито (вградено) табло",
            "Да, искам ново външно табло",
            "Не, остава старото"
          ]
        },
        {
          "id": "elec_rcd_needed",
          "text": "Желаете ли монтаж на дефектнотокови защити (ДТЗ) за максимална безопасност?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, за всички кръгове",
            "Не"
          ]
        },
        {
          "id": "elec_lv_count",
          "text": "Колко телевизионни/интернет (слаботокови) розетки желаете?",
          "type": "number",
          "required": true
        },
        {
          "id": "elec_dev_count",
          "text": "Колко девиаторни ключа (светване/изгасване от две различни места) желаете?",
          "type": "number",
          "required": true
        },
        {
          "id": "elec_spec_count",
          "text": "Колко извода за вентилатори или електрически щори са нужни?",
          "type": "number",
          "required": true
        }
      ]
    }'::jsonb, now(), now());
    ELSE
        UPDATE "ServiceCategories"
        SET "TemplateStructure" = '{
      "questions": [
        {
          "id": "elec_scope",
          "text": "Какъв е мащабът на ремонта?",
          "type": "choice",
          "required": true,
          "options": [
            "Цялостна подмяна (всичко се изгражда наново)",
            "Частичен ремонт (добавяне/местене на контакти и лампи)",
            "Само монтаж (на ключове, контакти и осветителни тела)"
          ],
          "hintText": "💡 Ако инсталацията ви е над 20 години, препоръчваме цялостна подмяна."
        },
        {
          "id": "elec_heavy_appliances",
          "text": "Кои мощни уреди ще имате? (Изберете всички)",
          "type": "multiselect",
          "required": true,
          "options": [
            "Фурна",
            "Индукционен котлон",
            "Съдомиялна",
            "Пералня",
            "Сушилня",
            "Проточен бойлер"
          ]
        },
        {
          "id": "elec_ac_count",
          "text": "Колко климатика ще се захранват?",
          "type": "choice",
          "required": true,
          "options": [
            "0",
            "1",
            "2",
            "3",
            "4+"
          ]
        },
        {
          "id": "elec_outlets_comfort",
          "text": "Колко контакти желаете във всяка стая?",
          "type": "choice",
          "required": true,
          "options": [
            "Базово (по 3-4 на стая)",
            "Комфорт (по 5-6 на стая)",
            "Премиум (над 8 на стая)"
          ],
          "hintText": "💡 Повечето ни клиенти избират ''Комфорт'', за да избегнат разклонители."
        },
        {
          "id": "elec_lighting",
          "text": "Какъв тип ще е основното осветление?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Стандартно (полилеи/плафони)",
            "Вградени лунички",
            "Скрито LED осветление"
          ]
        },
        {
          "id": "elec_panel",
          "text": "Главното ел. табло ще се подменя ли?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, искам ново скрито (вградено) табло",
            "Да, искам ново външно табло",
            "Не, остава старото"
          ]
        },
        {
          "id": "elec_rcd_needed",
          "text": "Желаете ли монтаж на дефектнотокови защити (ДТЗ) за максимална безопасност?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, за всички кръгове",
            "Не"
          ]
        },
        {
          "id": "elec_lv_count",
          "text": "Колко телевизионни/интернет (слаботокови) розетки желаете?",
          "type": "number",
          "required": true
        },
        {
          "id": "elec_dev_count",
          "text": "Колко девиаторни ключа (светване/изгасване от две различни места) желаете?",
          "type": "number",
          "required": true
        },
        {
          "id": "elec_spec_count",
          "text": "Колко извода за вентилатори или електрически щори са нужни?",
          "type": "number",
          "required": true
        }
      ]
    }'::jsonb, "IsGlobal" = false, "UpdatedAt" = now()
        WHERE "Id" = cat_id;
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NULL THEN
        INSERT INTO "ServiceCategories" ("Id", "Name", "Status", "IsGlobal", "TemplateStructure", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), 'ВиК Услуги', 1, false, '{
      "questions": [
        {
          "id": "plumb_scope",
          "text": "Какъв е мащабът на ВиК ремонта?",
          "type": "choice",
          "required": true,
          "options": [
            "Цялостна подмяна (нови тръби и канали)",
            "Само извеждане на нови ВиК изводи (точки)",
            "Само монтаж (на мивки, душове, тоалетни)"
          ],
          "hintText": "💡 Ако тръбите ви са стари (метални), препоръчваме цялостна подмяна с полипропилен."
        },
        {
          "id": "plumb_rooms",
          "text": "В кои помещения ще се извършват ВиК дейности?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Баня",
            "Кухня",
            "Мокро помещение / Перално",
            "Втора тоалетна"
          ]
        },
        {
          "id": "plumb_wc_type",
          "text": "Какъв тип тоалетна ще монтираме?",
          "type": "choice",
          "required": true,
          "options": [
            "Стандартна (моноблок)",
            "Вградена структура (конзолна)",
            "Няма да се монтира тоалетна"
          ]
        },
        {
          "id": "plumb_shower_type",
          "text": "Каква ще бъде душ зоната?",
          "type": "choice",
          "required": true,
          "options": [
            "Само душ батерия / окачване",
            "Душ кабина или стъклен параван",
            "Вана",
            "Няма да има душ"
          ]
        },
        {
          "id": "plumb_sink_count",
          "text": "Колко мивки (за баня и кухня) общо ще се монтират?",
          "type": "choice",
          "required": true,
          "options": [
            "0",
            "1",
            "2",
            "3+"
          ]
        },
        {
          "id": "plumb_appliances",
          "text": "Какви други уреди ще свързваме към ВиК мрежата?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Пералня",
            "Съдомиялна",
            "Електрически бойлер (до 100л)"
          ]
        },
        {
          "id": "plumb_riser",
          "text": "Ще подменяме ли главния вертикален щранг (общите тръби)?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, искам подмяна",
            "Не, остават старите",
            "Не знам (ще се реши на място)"
          ]
        }
      ]
    }'::jsonb, now(), now());
    ELSE
        UPDATE "ServiceCategories"
        SET "TemplateStructure" = '{
      "questions": [
        {
          "id": "plumb_scope",
          "text": "Какъв е мащабът на ВиК ремонта?",
          "type": "choice",
          "required": true,
          "options": [
            "Цялостна подмяна (нови тръби и канали)",
            "Само извеждане на нови ВиК изводи (точки)",
            "Само монтаж (на мивки, душове, тоалетни)"
          ],
          "hintText": "💡 Ако тръбите ви са стари (метални), препоръчваме цялостна подмяна с полипропилен."
        },
        {
          "id": "plumb_rooms",
          "text": "В кои помещения ще се извършват ВиК дейности?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Баня",
            "Кухня",
            "Мокро помещение / Перално",
            "Втора тоалетна"
          ]
        },
        {
          "id": "plumb_wc_type",
          "text": "Какъв тип тоалетна ще монтираме?",
          "type": "choice",
          "required": true,
          "options": [
            "Стандартна (моноблок)",
            "Вградена структура (конзолна)",
            "Няма да се монтира тоалетна"
          ]
        },
        {
          "id": "plumb_shower_type",
          "text": "Каква ще бъде душ зоната?",
          "type": "choice",
          "required": true,
          "options": [
            "Само душ батерия / окачване",
            "Душ кабина или стъклен параван",
            "Вана",
            "Няма да има душ"
          ]
        },
        {
          "id": "plumb_sink_count",
          "text": "Колко мивки (за баня и кухня) общо ще се монтират?",
          "type": "choice",
          "required": true,
          "options": [
            "0",
            "1",
            "2",
            "3+"
          ]
        },
        {
          "id": "plumb_appliances",
          "text": "Какви други уреди ще свързваме към ВиК мрежата?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Пералня",
            "Съдомиялна",
            "Електрически бойлер (до 100л)"
          ]
        },
        {
          "id": "plumb_riser",
          "text": "Ще подменяме ли главния вертикален щранг (общите тръби)?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, искам подмяна",
            "Не, остават старите",
            "Не знам (ще се реши на място)"
          ]
        }
      ]
    }'::jsonb, "IsGlobal" = false, "UpdatedAt" = now()
        WHERE "Id" = cat_id;
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    IF cat_id IS NULL THEN
        INSERT INTO "ServiceCategories" ("Id", "Name", "Status", "IsGlobal", "TemplateStructure", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), 'Бояджийски и шпакловъчни услуги', 1, false, '{
      "questions": [
        {
          "id": "paint_scope",
          "text": "Какво е текущото състояние на стените и какво желаете да се направи?",
          "type": "choice",
          "required": true,
          "options": [
            "Освежаване (Само боядисване върху здрава основа)",
            "Стандартен ремонт (Шпакловка и боядисване)",
            "Сваляне на тапети, шпакловка и боядисване"
          ],
          "hintText": "💡 Ако имате пукнатини или грапавини, изберете ''Стандартен ремонт'', за да се изгладят."
        },
        {
          "id": "paint_rooms",
          "text": "Кои помещения ще се боядисват?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Всички стаи",
            "Хол / Всекидневна",
            "Спални",
            "Коридор / Антре",
            "Само тавани (в мокри помещения)"
          ]
        },
        {
          "id": "paint_colors",
          "text": "Какви цветове ще използвате?",
          "type": "choice",
          "required": true,
          "options": [
            "Всичко в бяло (най-бързо и бюджетно)",
            "Светли цветове",
            "Тъмни или наситени цветове",
            "Смесено (бял таван, цветни стени)"
          ]
        },
        {
          "id": "paint_finish_level",
          "text": "Какво е очакваното ниво на гладкост?",
          "type": "choice",
          "required": true,
          "options": [
            "Стандартно (добро за матови и светли бои)",
            "Перфектно гладко (Q5 - задължително за тъмни бои и силно осветление)"
          ],
          "dependsOn": "paint_scope",
          "dependsOnValue": "Стандартен ремонт (Шпакловка и боядисване)|Сваляне на тапети, шпакловка и боядисване",
          "hintText": "💡 Q5 изисква специални готови смеси и перфектно машинно шлайфане."
        },
        {
          "id": "paint_trim_doors_count",
          "text": "Имате ли стари интериорни врати, които искате майсторът да реставрира и пребоядиса?",
          "type": "choice",
          "required": true,
          "options": [
            "0",
            "1",
            "2",
            "3",
            "4+"
          ]
        },
        {
          "id": "paint_plaster_needed",
          "text": "Стените имат ли големи отклонения и нужда от изправяне с мазилка?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, цялостно изправяне",
            "Само на определени места",
            "Не, само шпакловка"
          ]
        }
      ]
    }'::jsonb, now(), now());
    ELSE
        UPDATE "ServiceCategories"
        SET "TemplateStructure" = '{
      "questions": [
        {
          "id": "paint_scope",
          "text": "Какво е текущото състояние на стените и какво желаете да се направи?",
          "type": "choice",
          "required": true,
          "options": [
            "Освежаване (Само боядисване върху здрава основа)",
            "Стандартен ремонт (Шпакловка и боядисване)",
            "Сваляне на тапети, шпакловка и боядисване"
          ],
          "hintText": "💡 Ако имате пукнатини или грапавини, изберете ''Стандартен ремонт'', за да се изгладят."
        },
        {
          "id": "paint_rooms",
          "text": "Кои помещения ще се боядисват?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Всички стаи",
            "Хол / Всекидневна",
            "Спални",
            "Коридор / Антре",
            "Само тавани (в мокри помещения)"
          ]
        },
        {
          "id": "paint_colors",
          "text": "Какви цветове ще използвате?",
          "type": "choice",
          "required": true,
          "options": [
            "Всичко в бяло (най-бързо и бюджетно)",
            "Светли цветове",
            "Тъмни или наситени цветове",
            "Смесено (бял таван, цветни стени)"
          ]
        },
        {
          "id": "paint_finish_level",
          "text": "Какво е очакваното ниво на гладкост?",
          "type": "choice",
          "required": true,
          "options": [
            "Стандартно (добро за матови и светли бои)",
            "Перфектно гладко (Q5 - задължително за тъмни бои и силно осветление)"
          ],
          "dependsOn": "paint_scope",
          "dependsOnValue": "Стандартен ремонт (Шпакловка и боядисване)|Сваляне на тапети, шпакловка и боядисване",
          "hintText": "💡 Q5 изисква специални готови смеси и перфектно машинно шлайфане."
        },
        {
          "id": "paint_trim_doors_count",
          "text": "Имате ли стари интериорни врати, които искате майсторът да реставрира и пребоядиса?",
          "type": "choice",
          "required": true,
          "options": [
            "0",
            "1",
            "2",
            "3",
            "4+"
          ]
        },
        {
          "id": "paint_plaster_needed",
          "text": "Стените имат ли големи отклонения и нужда от изправяне с мазилка?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, цялостно изправяне",
            "Само на определени места",
            "Не, само шпакловка"
          ]
        }
      ]
    }'::jsonb, "IsGlobal" = false, "UpdatedAt" = now()
        WHERE "Id" = cat_id;
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    IF cat_id IS NULL THEN
        INSERT INTO "ServiceCategories" ("Id", "Name", "Status", "IsGlobal", "TemplateStructure", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), 'Къртене и извозване', 1, false, '{
      "questions": [
        {
          "id": "demo_what",
          "text": "Какво точно трябва да се кърти?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Цяла баня (стари плочки и санитария)",
            "Вътрешни тухлени стени",
            "Бетонни/Панелни стени",
            "Стари подови настилки (замазка/мозайка)"
          ]
        },
        {
          "id": "demo_brick_sqm",
          "text": "Колко кв.м. приблизително са тухлените стени за събаряне?",
          "type": "number",
          "required": true,
          "dependsOn": "demo_what",
          "dependsOnValue": "Вътрешни тухлени стени"
        },
        {
          "id": "demo_conc_sqm",
          "text": "Колко кв.м. приблизително са бетонните/панелните стени за къртене?",
          "type": "number",
          "required": true,
          "dependsOn": "demo_what",
          "dependsOnValue": "Бетонни/Панелни стени"
        },
        {
          "id": "demo_floor_sqm",
          "text": "Колко кв.м. приблизително са старите подови настилки за премахване?",
          "type": "number",
          "required": true,
          "dependsOn": "demo_what",
          "dependsOnValue": "Стари подови настилки (замазка/мозайка)"
        },
        {
          "id": "demo_disposal",
          "text": "Желаете ли извозване на строителните отпадъци?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, искам контейнер и извозване",
            "Не, ще се справя сам"
          ]
        }
      ]
    }'::jsonb, now(), now());
    ELSE
        UPDATE "ServiceCategories"
        SET "TemplateStructure" = '{
      "questions": [
        {
          "id": "demo_what",
          "text": "Какво точно трябва да се кърти?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Цяла баня (стари плочки и санитария)",
            "Вътрешни тухлени стени",
            "Бетонни/Панелни стени",
            "Стари подови настилки (замазка/мозайка)"
          ]
        },
        {
          "id": "demo_brick_sqm",
          "text": "Колко кв.м. приблизително са тухлените стени за събаряне?",
          "type": "number",
          "required": true,
          "dependsOn": "demo_what",
          "dependsOnValue": "Вътрешни тухлени стени"
        },
        {
          "id": "demo_conc_sqm",
          "text": "Колко кв.м. приблизително са бетонните/панелните стени за къртене?",
          "type": "number",
          "required": true,
          "dependsOn": "demo_what",
          "dependsOnValue": "Бетонни/Панелни стени"
        },
        {
          "id": "demo_floor_sqm",
          "text": "Колко кв.м. приблизително са старите подови настилки за премахване?",
          "type": "number",
          "required": true,
          "dependsOn": "demo_what",
          "dependsOnValue": "Стари подови настилки (замазка/мозайка)"
        },
        {
          "id": "demo_disposal",
          "text": "Желаете ли извозване на строителните отпадъци?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, искам контейнер и извозване",
            "Не, ще се справя сам"
          ]
        }
      ]
    }'::jsonb, "IsGlobal" = false, "UpdatedAt" = now()
        WHERE "Id" = cat_id;
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    IF cat_id IS NULL THEN
        INSERT INTO "ServiceCategories" ("Id", "Name", "Status", "IsGlobal", "TemplateStructure", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), 'Сухо строителство', 1, false, '{
      "questions": [
        {
          "id": "drywall_type",
          "text": "Какво ще се изгражда от гипсокартон?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Окачен таван",
            "Преградни стени",
            "Предстенна обшивка (на конструкция)",
            "Куфари (обличане на тръби)",
            "Скрито осветление (L-образни/U-образни куфари)"
          ]
        },
        {
          "id": "dryw_ceiling_sqm",
          "text": "Колко кв.м. приблизително ще бъдат окачените тавани?",
          "type": "number",
          "required": true,
          "dependsOn": "drywall_type",
          "dependsOnValue": "Окачен таван"
        },
        {
          "id": "dryw_partition_sqm",
          "text": "Колко кв.м. приблизително ще са преградните стени?",
          "type": "number",
          "required": true,
          "dependsOn": "drywall_type",
          "dependsOnValue": "Преградни стени"
        },
        {
          "id": "dryw_lining_sqm",
          "text": "Колко кв.м. приблизително е предстенната обшивка?",
          "type": "number",
          "required": true,
          "dependsOn": "drywall_type",
          "dependsOnValue": "Предстенна обшивка (на конструкция)"
        },
        {
          "id": "dryw_box_m",
          "text": "Колко линейни метра (л.м.) ще бъдат куфарите/скритото осветление?",
          "type": "number",
          "required": true,
          "dependsOn": "drywall_type",
          "dependsOnValue": "Куфари (обличане на тръби) | Скрито осветление (L-образни/U-образни куфари)"
        },
        {
          "id": "drywall_insulation",
          "text": "Желаете ли поставяне на изолация (вата) зад картона?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, стандартна вата",
            "Да, специална шумоизолация",
            "Не"
          ]
        },
        {
          "id": "dryw_insulation_areas",
          "text": "Къде ще се поставя изолацията?",
          "type": "multiselect",
          "required": true,
          "dependsOn": "drywall_insulation",
          "dependsOnValue": "Да, стандартна вата | Да, специална шумоизолация",
          "options": [
            "В окачените тавани",
            "В стените (преградни/обшивки)",
            "В куфарите"
          ]
        }
      ]
    }'::jsonb, now(), now());
    ELSE
        UPDATE "ServiceCategories"
        SET "TemplateStructure" = '{
      "questions": [
        {
          "id": "drywall_type",
          "text": "Какво ще се изгражда от гипсокартон?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Окачен таван",
            "Преградни стени",
            "Предстенна обшивка (на конструкция)",
            "Куфари (обличане на тръби)",
            "Скрито осветление (L-образни/U-образни куфари)"
          ]
        },
        {
          "id": "dryw_ceiling_sqm",
          "text": "Колко кв.м. приблизително ще бъдат окачените тавани?",
          "type": "number",
          "required": true,
          "dependsOn": "drywall_type",
          "dependsOnValue": "Окачен таван"
        },
        {
          "id": "dryw_partition_sqm",
          "text": "Колко кв.м. приблизително ще са преградните стени?",
          "type": "number",
          "required": true,
          "dependsOn": "drywall_type",
          "dependsOnValue": "Преградни стени"
        },
        {
          "id": "dryw_lining_sqm",
          "text": "Колко кв.м. приблизително е предстенната обшивка?",
          "type": "number",
          "required": true,
          "dependsOn": "drywall_type",
          "dependsOnValue": "Предстенна обшивка (на конструкция)"
        },
        {
          "id": "dryw_box_m",
          "text": "Колко линейни метра (л.м.) ще бъдат куфарите/скритото осветление?",
          "type": "number",
          "required": true,
          "dependsOn": "drywall_type",
          "dependsOnValue": "Куфари (обличане на тръби) | Скрито осветление (L-образни/U-образни куфари)"
        },
        {
          "id": "drywall_insulation",
          "text": "Желаете ли поставяне на изолация (вата) зад картона?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, стандартна вата",
            "Да, специална шумоизолация",
            "Не"
          ]
        },
        {
          "id": "dryw_insulation_areas",
          "text": "Къде ще се поставя изолацията?",
          "type": "multiselect",
          "required": true,
          "dependsOn": "drywall_insulation",
          "dependsOnValue": "Да, стандартна вата | Да, специална шумоизолация",
          "options": [
            "В окачените тавани",
            "В стените (преградни/обшивки)",
            "В куфарите"
          ]
        }
      ]
    }'::jsonb, "IsGlobal" = false, "UpdatedAt" = now()
        WHERE "Id" = cat_id;
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    IF cat_id IS NULL THEN
        INSERT INTO "ServiceCategories" ("Id", "Name", "Status", "IsGlobal", "TemplateStructure", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), 'Подови и стенни настилки', 1, false, '{
      "questions": [
        {
          "id": "tile_type",
          "text": "Какъв тип настилки ще се полагат?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Стандартни плочки (до 60х60)",
            "Голямоформатен гранитогрес (над 60х120)",
            "Ламиниран паркет"
          ]
        },
        {
          "id": "tile_std_sqm",
          "text": "Колко кв.м. са стандартните плочки (до 60х60)?",
          "type": "number",
          "required": true,
          "dependsOn": "tile_type",
          "dependsOnValue": "Стандартни плочки (до 60х60)"
        },
        {
          "id": "tile_large_sqm",
          "text": "Колко кв.м. е голямоформатният гранитогрес?",
          "type": "number",
          "required": true,
          "dependsOn": "tile_type",
          "dependsOnValue": "Голямоформатен гранитогрес (над 60х120)"
        },
        {
          "id": "tile_laminate_sqm",
          "text": "Колко кв.м. е ламинираният паркет?",
          "type": "number",
          "required": true,
          "dependsOn": "tile_type",
          "dependsOnValue": "Ламиниран паркет"
        },
        {
          "id": "tile_rooms",
          "text": "Къде ще се полагат настилките?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Баня / Мокри помещения",
            "Кухня / Коридор",
            "Спални / Хол"
          ]
        },
        {
          "id": "tile_prep",
          "text": "Каква подготовка на пода е нужна?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Саморазливна замазка (за изравняване)",
            "Хидроизолация (за бани)",
            "Не знам (майсторът да прецени)"
          ]
        },
        {
          "id": "tile_prep_level_sqm",
          "text": "Колко кв.м. саморазливна замазка ще бъде нужна приблизително?",
          "type": "number",
          "required": true,
          "dependsOn": "tile_prep",
          "dependsOnValue": "Саморазливна замазка (за изравняване)"
        },
        {
          "id": "tile_prep_hydro_sqm",
          "text": "Колко кв.м. хидроизолация ще бъде нужна приблизително?",
          "type": "number",
          "required": true,
          "dependsOn": "tile_prep",
          "dependsOnValue": "Хидроизолация (за бани)"
        },
        {
          "id": "tile_gerung",
          "text": "Желаете ли 45-градусово изрязване на ъглите (Герунг)?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, за всички външни ъгли",
            "Не, ще ползвам лайсни"
          ]
        }
      ]
    }'::jsonb, now(), now());
    ELSE
        UPDATE "ServiceCategories"
        SET "TemplateStructure" = '{
      "questions": [
        {
          "id": "tile_type",
          "text": "Какъв тип настилки ще се полагат?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Стандартни плочки (до 60х60)",
            "Голямоформатен гранитогрес (над 60х120)",
            "Ламиниран паркет"
          ]
        },
        {
          "id": "tile_std_sqm",
          "text": "Колко кв.м. са стандартните плочки (до 60х60)?",
          "type": "number",
          "required": true,
          "dependsOn": "tile_type",
          "dependsOnValue": "Стандартни плочки (до 60х60)"
        },
        {
          "id": "tile_large_sqm",
          "text": "Колко кв.м. е голямоформатният гранитогрес?",
          "type": "number",
          "required": true,
          "dependsOn": "tile_type",
          "dependsOnValue": "Голямоформатен гранитогрес (над 60х120)"
        },
        {
          "id": "tile_laminate_sqm",
          "text": "Колко кв.м. е ламинираният паркет?",
          "type": "number",
          "required": true,
          "dependsOn": "tile_type",
          "dependsOnValue": "Ламиниран паркет"
        },
        {
          "id": "tile_rooms",
          "text": "Къде ще се полагат настилките?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Баня / Мокри помещения",
            "Кухня / Коридор",
            "Спални / Хол"
          ]
        },
        {
          "id": "tile_prep",
          "text": "Каква подготовка на пода е нужна?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Саморазливна замазка (за изравняване)",
            "Хидроизолация (за бани)",
            "Не знам (майсторът да прецени)"
          ]
        },
        {
          "id": "tile_prep_level_sqm",
          "text": "Колко кв.м. саморазливна замазка ще бъде нужна приблизително?",
          "type": "number",
          "required": true,
          "dependsOn": "tile_prep",
          "dependsOnValue": "Саморазливна замазка (за изравняване)"
        },
        {
          "id": "tile_prep_hydro_sqm",
          "text": "Колко кв.м. хидроизолация ще бъде нужна приблизително?",
          "type": "number",
          "required": true,
          "dependsOn": "tile_prep",
          "dependsOnValue": "Хидроизолация (за бани)"
        },
        {
          "id": "tile_gerung",
          "text": "Желаете ли 45-градусово изрязване на ъглите (Герунг)?",
          "type": "choice",
          "required": true,
          "options": [
            "Да, за всички външни ъгли",
            "Не, ще ползвам лайсни"
          ]
        }
      ]
    }'::jsonb, "IsGlobal" = false, "UpdatedAt" = now()
        WHERE "Id" = cat_id;
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    IF cat_id IS NULL THEN
        INSERT INTO "ServiceCategories" ("Id", "Name", "Status", "IsGlobal", "TemplateStructure", "CreatedAt", "UpdatedAt")
        VALUES (gen_random_uuid(), 'Микроцимент', 1, false, '{
      "questions": [
        {
          "id": "mico_area",
          "text": "Къде ще се полага микроциментът?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Сухи зони (подове и стени в стаи)",
            "Мокри зони (Баня)"
          ]
        },
        {
          "id": "mico_rooms",
          "text": "В колко помещения?",
          "type": "choice",
          "required": true,
          "options": [
            "Само в банята",
            "В 1-2 стаи",
            "В целия обект"
          ]
        }
      ]
    }'::jsonb, now(), now());
    ELSE
        UPDATE "ServiceCategories"
        SET "TemplateStructure" = '{
      "questions": [
        {
          "id": "mico_area",
          "text": "Къде ще се полага микроциментът?",
          "type": "multiselect",
          "required": true,
          "options": [
            "Сухи зони (подове и стени в стаи)",
            "Мокри зони (Баня)"
          ]
        },
        {
          "id": "mico_rooms",
          "text": "В колко помещения?",
          "type": "choice",
          "required": true,
          "options": [
            "Само в банята",
            "В 1-2 стаи",
            "В целия обект"
          ]
        }
      ]
    }'::jsonb, "IsGlobal" = false, "UpdatedAt" = now()
        WHERE "Id" = cat_id;
    END IF;
END $$;

-- 3. SYNC SERVICE SKUS AND CALCULATION FORMULAS
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Global Questions';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'GEN-001';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'GEN-001', 'Site Prep & Protection', 'Preparation, protection, and logistics.', 50, 'Flat', '1', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Site Prep & Protection', "Description" = 'Preparation, protection, and logistics.', "BasePrice" = 50, "UnitType" = 'Flat', "CalculationFormula" = '1', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Global Questions', 'GEN-001';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Global Questions';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'GEN-002';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'GEN-002', 'Final Cleaning', 'Complete final cleaning after works.', 2, 'sqm', '1', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Final Cleaning', "Description" = 'Complete final cleaning after works.', "BasePrice" = 2, "UnitType" = 'sqm', "CalculationFormula" = '1', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Global Questions', 'GEN-002';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Global Questions';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'GEN-003';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'GEN-003', 'Daily Cleaning', 'Daily site cleaning.', 30, 'Flat', '1', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Daily Cleaning', "Description" = 'Daily site cleaning.', "BasePrice" = 30, "UnitType" = 'Flat', "CalculationFormula" = '1', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Global Questions', 'GEN-003';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CABLE-LAY';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-CABLE-LAY', 'Полагане на силов кабел', 'Издърпване и фиксиране на кабел.', 2, 'm', 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, if(Contains(elec_scope, ''Частичен''), global_total_sqm * 1.0, 0))', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Полагане на силов кабел', "Description" = 'Издърпване и фиксиране на кабел.', "BasePrice" = 2, "UnitType" = 'm', "CalculationFormula" = 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, if(Contains(elec_scope, ''Частичен''), global_total_sqm * 1.0, 0))', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-CABLE-LAY';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CABLE-HEAVY';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-CABLE-HEAVY', 'Полагане на мощен кабел', 'Дебел кабел за проточни бойлери.', 5, 'm', '(Count(elec_heavy_appliances) + elec_ac_count) * 10', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Полагане на мощен кабел', "Description" = 'Дебел кабел за проточни бойлери.', "BasePrice" = 5, "UnitType" = 'm', "CalculationFormula" = '(Count(elec_heavy_appliances) + elec_ac_count) * 10', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-CABLE-HEAVY';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CHASE-CONC';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-CHASE-CONC', 'Къртене на канал в бетон', 'Изкопаване на канал в бетон.', 15, 'm', 'if(Contains(global_wall_material, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Къртене на канал в бетон', "Description" = 'Изкопаване на канал в бетон.', "BasePrice" = 15, "UnitType" = 'm', "CalculationFormula" = 'if(Contains(global_wall_material, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-CHASE-CONC';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CHASE-BRICK';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-CHASE-BRICK', 'Къртене на канал в тухла', 'Изкопаване на канал в тухла.', 8, 'm', 'if(Contains(global_wall_material, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Къртене на канал в тухла', "Description" = 'Изкопаване на канал в тухла.', "BasePrice" = 8, "UnitType" = 'm', "CalculationFormula" = 'if(Contains(global_wall_material, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-CHASE-BRICK';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-LAY-TUBE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-LAY-TUBE', 'Полагане на гофре', 'Полагане на гофрирана тръба.', 4, 'm', 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Полагане на гофре', "Description" = 'Полагане на гофрирана тръба.', "BasePrice" = 4, "UnitType" = 'm', "CalculationFormula" = 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-LAY-TUBE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-PANEL-MOD';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-PANEL-MOD', 'Сглобяване на табло (на модул)', 'Подреждане на предпазители.', 15, 'module', '12 + Count(elec_heavy_appliances) + elec_ac_count', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Сглобяване на табло (на модул)', "Description" = 'Подреждане на предпазители.', "BasePrice" = 15, "UnitType" = 'module', "CalculationFormula" = '12 + Count(elec_heavy_appliances) + elec_ac_count', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-PANEL-MOD';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-PANEL-NICHE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-PANEL-NICHE', 'Изкопаване на ниша за вградено табло', 'Скрит монтаж.', 95, 'pcs', 'if(Contains(elec_panel, ''скрито''), 1, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Изкопаване на ниша за вградено табло', "Description" = 'Скрит монтаж.', "BasePrice" = 95, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(elec_panel, ''скрито''), 1, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-PANEL-NICHE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-STD';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-POINT-STD', 'Изграждане на излазна точка', 'Труд за 1 брой контакт/ключ.', 35, 'pcs', 'if(Contains(elec_outlets_comfort, ''Базово''), (global_room_count * 3) + 4, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, 0)))', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Изграждане на излазна точка', "Description" = 'Труд за 1 брой контакт/ключ.', "BasePrice" = 35, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(elec_outlets_comfort, ''Базово''), (global_room_count * 3) + 4, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, 0)))', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-POINT-STD';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-LV';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-POINT-LV', 'Слаботокова точка', 'LAN/TV/СОТ.', 30, 'pcs', 'elec_lv_count', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Слаботокова точка', "Description" = 'LAN/TV/СОТ.', "BasePrice" = 30, "UnitType" = 'pcs', "CalculationFormula" = 'elec_lv_count', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-POINT-LV';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-DEV';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-POINT-DEV', 'Девиаторна точка', 'Девиаторни ключове.', 55, 'pcs', 'elec_dev_count', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Девиаторна точка', "Description" = 'Девиаторни ключове.', "BasePrice" = 55, "UnitType" = 'pcs', "CalculationFormula" = 'elec_dev_count', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-POINT-DEV';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-SPEC';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-POINT-SPEC', 'Извод за щори/вентилатор', 'Вентилатори или щори.', 40, 'pcs', 'elec_spec_count', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Извод за щори/вентилатор', "Description" = 'Вентилатори или щори.', "BasePrice" = 40, "UnitType" = 'pcs', "CalculationFormula" = 'elec_spec_count', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-POINT-SPEC';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-LED-TRAFO';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'ELEC-LED-TRAFO', 'Монтаж на захранващ блок (Траф) за LED', 'Трансформатор.', 30, 'pcs', 'if(Contains(elec_lighting, ''LED''), 1, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на захранващ блок (Траф) за LED', "Description" = 'Трансформатор.', "BasePrice" = 30, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(elec_lighting, ''LED''), 1, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Електрическа Инсталация', 'ELEC-LED-TRAFO';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-PRIMER';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PANT-PRIMER', 'Дълбокопроникващ грунд', 'Грундиране на стени и тавани.', 3, 'sqm', 'if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Дълбокопроникващ грунд', "Description" = 'Грундиране на стени и тавани.', "BasePrice" = 3, "UnitType" = 'sqm', "CalculationFormula" = 'if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Бояджийски и шпакловъчни услуги', 'PANT-PRIMER';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-SPACKLE-STD';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PANT-SPACKLE-STD', 'Шпакловка (Стандартна 2 ръце)', 'Цялостна шпакловка.', 14, 'sqm', 'if(Contains(paint_tasks, ''Цялостна шпакловка'') || Contains(paint_tasks, ''Сваляне на тапети''), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5), 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Шпакловка (Стандартна 2 ръце)', "Description" = 'Цялостна шпакловка.', "BasePrice" = 14, "UnitType" = 'sqm', "CalculationFormula" = 'if(Contains(paint_tasks, ''Цялостна шпакловка'') || Contains(paint_tasks, ''Сваляне на тапети''), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5), 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Бояджийски и шпакловъчни услуги', 'PANT-SPACKLE-STD';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-SPACKLE-Q5';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PANT-SPACKLE-Q5', 'Фина шпакловка (Перфектна Q5)', 'Шитрок за идеално гладка повърхност.', 20, 'sqm', 'if(Contains(paint_finish_level, ''Q5'') || Contains(paint_finish_level, ''Перфектно''), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5), 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Фина шпакловка (Перфектна Q5)', "Description" = 'Шитрок за идеално гладка повърхност.', "BasePrice" = 20, "UnitType" = 'sqm', "CalculationFormula" = 'if(Contains(paint_finish_level, ''Q5'') || Contains(paint_finish_level, ''Перфектно''), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5), 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Бояджийски и шпакловъчни услуги', 'PANT-SPACKLE-Q5';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-PAINT-WHITE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PANT-PAINT-WHITE', 'Боядисване в бяло (2 ръце)', 'Боядисване с бял латекс.', 6.50, 'sqm', 'if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Боядисване в бяло (2 ръце)', "Description" = 'Боядисване с бял латекс.', "BasePrice" = 6.50, "UnitType" = 'sqm', "CalculationFormula" = 'if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Бояджийски и шпакловъчни услуги', 'PANT-PAINT-WHITE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-PAINT-COLOR';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PANT-PAINT-COLOR', 'Боядисване в цвят (2 ръце)', 'Боядисване с цветен латекс.', 8.50, 'sqm', 'if(Contains(paint_colors, ''цвят'') && !Contains(paint_colors, ''бяло''), if(paint_sqm > 0, paint_sqm * 0.5, global_total_sqm * 1.2), 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Боядисване в цвят (2 ръце)', "Description" = 'Боядисване с цветен латекс.', "BasePrice" = 8.50, "UnitType" = 'sqm', "CalculationFormula" = 'if(Contains(paint_colors, ''цвят'') && !Contains(paint_colors, ''бяло''), if(paint_sqm > 0, paint_sqm * 0.5, global_total_sqm * 1.2), 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Бояджийски и шпакловъчни услуги', 'PANT-PAINT-COLOR';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-TAPE-CORNER';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PANT-TAPE-CORNER', 'Поставяне на ъглохранители', 'Алуминиеви или PVC ъгли.', 6, 'm', 'if(paint_sqm > 0, paint_sqm * 0.1, global_total_sqm * 0.25)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Поставяне на ъглохранители', "Description" = 'Алуминиеви или PVC ъгли.', "BasePrice" = 6, "UnitType" = 'm', "CalculationFormula" = 'if(paint_sqm > 0, paint_sqm * 0.1, global_total_sqm * 0.25)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Бояджийски и шпакловъчни услуги', 'PANT-TAPE-CORNER';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-TRIM';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PANT-TRIM', 'Боядисване на врати / первази', 'Боядисване на декоративни елементи.', 45, 'pcs', 'if(Contains(paint_trim_doors_count, ''4+''), 4, if(Contains(paint_trim_doors_count, ''3''), 3, if(Contains(paint_trim_doors_count, ''2''), 2, if(Contains(paint_trim_doors_count, ''1''), 1, 0))))', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Боядисване на врати / первази', "Description" = 'Боядисване на декоративни елементи.', "BasePrice" = 45, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(paint_trim_doors_count, ''4+''), 4, if(Contains(paint_trim_doors_count, ''3''), 3, if(Contains(paint_trim_doors_count, ''2''), 2, if(Contains(paint_trim_doors_count, ''1''), 1, 0))))', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Бояджийски и шпакловъчни услуги', 'PANT-TRIM';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-WALLPAPER-REMOVE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PANT-WALLPAPER-REMOVE', 'Сваляне на стари тапети', 'Сваляне на стари тапети.', 4.50, 'sqm', 'if(Contains(paint_tasks, ''Сваляне на тапети''), if(paint_sqm > 0, paint_sqm * 0.5, global_total_sqm), 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Сваляне на стари тапети', "Description" = 'Сваляне на стари тапети.', "BasePrice" = 4.50, "UnitType" = 'sqm', "CalculationFormula" = 'if(Contains(paint_tasks, ''Сваляне на тапети''), if(paint_sqm > 0, paint_sqm * 0.5, global_total_sqm), 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Бояджийски и шпакловъчни услуги', 'PANT-WALLPAPER-REMOVE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-CEILING-STD';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DRYW-CEILING-STD', 'Окачен таван (Едно ниво)', 'Монтаж на окачен таван.', 45, 'sqm', 'dryw_ceiling_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Окачен таван (Едно ниво)', "Description" = 'Монтаж на окачен таван.', "BasePrice" = 45, "UnitType" = 'sqm', "CalculationFormula" = 'dryw_ceiling_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Сухо строителство', 'DRYW-CEILING-STD';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-WALL-PARTITION';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DRYW-WALL-PARTITION', 'Преградна стена (Двуслойна)', 'Изграждане на преградна стена.', 65, 'sqm', 'dryw_partition_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Преградна стена (Двуслойна)', "Description" = 'Изграждане на преградна стена.', "BasePrice" = 65, "UnitType" = 'sqm', "CalculationFormula" = 'dryw_partition_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Сухо строителство', 'DRYW-WALL-PARTITION';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-WALL-LINING';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DRYW-WALL-LINING', 'Предстенна обшивка', 'Монтаж на предстенна обшивка.', 40, 'sqm', 'dryw_lining_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Предстенна обшивка', "Description" = 'Монтаж на предстенна обшивка.', "BasePrice" = 40, "UnitType" = 'sqm', "CalculationFormula" = 'dryw_lining_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Сухо строителство', 'DRYW-WALL-LINING';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-BOX';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DRYW-BOX', 'Изграждане на куфари (Кутии)', 'Обличане на тръби.', 40, 'm', 'dryw_box_m', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Изграждане на куфари (Кутии)', "Description" = 'Обличане на тръби.', "BasePrice" = 40, "UnitType" = 'm', "CalculationFormula" = 'dryw_box_m', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Сухо строителство', 'DRYW-BOX';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-INSUL-CEILING';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DRYW-INSUL-CEILING', 'Монтаж на вата (Тавани)', 'Поставяне на минерална или каменна вата в окачен таван.', 10, 'sqm', 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''тавани''), dryw_ceiling_sqm, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на вата (Тавани)', "Description" = 'Поставяне на минерална или каменна вата в окачен таван.', "BasePrice" = 10, "UnitType" = 'sqm', "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''тавани''), dryw_ceiling_sqm, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Сухо строителство', 'DRYW-INSUL-CEILING';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-INSUL-PARTITION';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DRYW-INSUL-PARTITION', 'Монтаж на вата (Преградни стени)', 'Поставяне на минерална или каменна вата в преградни стени.', 10, 'sqm', 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''стените''), dryw_partition_sqm, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на вата (Преградни стени)', "Description" = 'Поставяне на минерална или каменна вата в преградни стени.', "BasePrice" = 10, "UnitType" = 'sqm', "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''стените''), dryw_partition_sqm, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Сухо строителство', 'DRYW-INSUL-PARTITION';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-INSUL-LINING';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DRYW-INSUL-LINING', 'Монтаж на вата (Предстенна обшивка)', 'Поставяне на минерална или каменна вата в предстенни обшивки.', 10, 'sqm', 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''стените''), dryw_lining_sqm, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на вата (Предстенна обшивка)', "Description" = 'Поставяне на минерална или каменна вата в предстенни обшивки.', "BasePrice" = 10, "UnitType" = 'sqm', "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''стените''), dryw_lining_sqm, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Сухо строителство', 'DRYW-INSUL-LINING';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-INSUL-BOX';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DRYW-INSUL-BOX', 'Монтаж на вата (Куфари)', 'Поставяне на минерална или каменна вата в куфари.', 10, 'm', 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''куфарите''), dryw_box_m, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на вата (Куфари)', "Description" = 'Поставяне на минерална или каменна вата в куфари.', "BasePrice" = 10, "UnitType" = 'm', "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''куфарите''), dryw_box_m, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Сухо строителство', 'DRYW-INSUL-BOX';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-STD';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'TILE-STD', 'Лепене на стандартни плочки', 'Полагане на фаянс или теракот.', 60, 'sqm', 'tile_std_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Лепене на стандартни плочки', "Description" = 'Полагане на фаянс или теракот.', "BasePrice" = 60, "UnitType" = 'sqm', "CalculationFormula" = 'tile_std_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Подови и стенни настилки', 'TILE-STD';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-LARGE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'TILE-LARGE', 'Лепене на голямоформатен гранитогрес', 'Плочи над 60х120 см.', 95, 'sqm', 'tile_large_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Лепене на голямоформатен гранитогрес', "Description" = 'Плочи над 60х120 см.', "BasePrice" = 95, "UnitType" = 'sqm', "CalculationFormula" = 'tile_large_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Подови и стенни настилки', 'TILE-LARGE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-PREP-LEVEL';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'TILE-PREP-LEVEL', 'Саморазливна замазка', 'Изравняване на пода.', 22, 'sqm', 'tile_prep_level_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Саморазливна замазка', "Description" = 'Изравняване на пода.', "BasePrice" = 22, "UnitType" = 'sqm', "CalculationFormula" = 'tile_prep_level_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Подови и стенни настилки', 'TILE-PREP-LEVEL';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-PREP-HYDRO';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'TILE-PREP-HYDRO', 'Полагане на хидроизолация (с лента)', 'Запечатване на мокри помещения.', 30, 'sqm', 'tile_prep_hydro_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Полагане на хидроизолация (с лента)', "Description" = 'Запечатване на мокри помещения.', "BasePrice" = 30, "UnitType" = 'sqm', "CalculationFormula" = 'tile_prep_hydro_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Подови и стенни настилки', 'TILE-PREP-HYDRO';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-LAMINATE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'TILE-LAMINATE', 'Монтаж на ламинат', 'Полагане на ламиниран паркет.', 6, 'sqm', 'tile_laminate_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на ламинат', "Description" = 'Полагане на ламиниран паркет.', "BasePrice" = 6, "UnitType" = 'sqm', "CalculationFormula" = 'tile_laminate_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Подови и стенни настилки', 'TILE-LAMINATE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'MICRO-STD';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'MICRO-STD', 'Полагане на микроцимент (сухи зони)', 'Полагане на микроцимент.', 140, 'sqm', 'if(Contains(mico_area, ''Сухи зони''), if(Contains(mico_rooms, ''1-2 стаи''), 30, global_total_sqm * 0.8), 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Полагане на микроцимент (сухи зони)', "Description" = 'Полагане на микроцимент.', "BasePrice" = 140, "UnitType" = 'sqm', "CalculationFormula" = 'if(Contains(mico_area, ''Сухи зони''), if(Contains(mico_rooms, ''1-2 стаи''), 30, global_total_sqm * 0.8), 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Микроцимент', 'MICRO-STD';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'MICRO-BATH';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'MICRO-BATH', 'Полагане на микроцимент в мокри зони (Баня)', 'Микроцимент за баня.', 180, 'sqm', 'if(Contains(mico_area, ''Мокри зони''), global_bathroom_count * 20, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Полагане на микроцимент в мокри зони (Баня)', "Description" = 'Микроцимент за баня.', "BasePrice" = 180, "UnitType" = 'sqm', "CalculationFormula" = 'if(Contains(mico_area, ''Мокри зони''), global_bathroom_count * 20, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Микроцимент', 'MICRO-BATH';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-POINT-NEW';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PLMB-POINT-NEW', 'Изграждане на нова ВиК точка', 'Тръби за топла, студена вода и канал.', 70, 'pcs', 'if(Contains(plumb_scope, ''Цялостна''), (global_bathroom_count * 5) + 3, if(Contains(plumb_scope, ''извеждане''), 3, 0))', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Изграждане на нова ВиК точка', "Description" = 'Тръби за топла, студена вода и канал.', "BasePrice" = 70, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(plumb_scope, ''Цялостна''), (global_bathroom_count * 5) + 3, if(Contains(plumb_scope, ''извеждане''), 3, 0))', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'ВиК Услуги', 'PLMB-POINT-NEW';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-RISER-REPLACE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PLMB-RISER-REPLACE', 'Смяна на вертикален щранг', 'Подмяна на основните метални тръби.', 240, 'pcs', 'if(Contains(plumb_riser, ''Да''), global_bathroom_count, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Смяна на вертикален щранг', "Description" = 'Подмяна на основните метални тръби.', "BasePrice" = 240, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(plumb_riser, ''Да''), global_bathroom_count, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'ВиК Услуги', 'PLMB-RISER-REPLACE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-SINK-INSTALL';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PLMB-SINK-INSTALL', 'Монтаж на мивка със смесител и сифон', 'Монтаж на мивки.', 90, 'pcs', 'if(Contains(plumb_sink_count, ''3+''), 3, if(Contains(plumb_sink_count, ''2''), 2, if(Contains(plumb_sink_count, ''1''), 1, 0)))', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на мивка със смесител и сифон', "Description" = 'Монтаж на мивки.', "BasePrice" = 90, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(plumb_sink_count, ''3+''), 3, if(Contains(plumb_sink_count, ''2''), 2, if(Contains(plumb_sink_count, ''1''), 1, 0)))', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'ВиК Услуги', 'PLMB-SINK-INSTALL';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-WC-STD';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PLMB-WC-STD', 'Монтаж на стандартна тоалетна (моноблок)', 'Монтаж на тоалетна.', 140, 'pcs', 'if(Contains(plumb_wc_type, ''Стандартна''), global_bathroom_count, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на стандартна тоалетна (моноблок)', "Description" = 'Монтаж на тоалетна.', "BasePrice" = 140, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(plumb_wc_type, ''Стандартна''), global_bathroom_count, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'ВиК Услуги', 'PLMB-WC-STD';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-WC-BUILTIN';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PLMB-WC-BUILTIN', 'Монтаж на структура за вграждане', 'Конзолна тоалетна.', 190, 'pcs', 'if(Contains(plumb_wc_type, ''Вградена''), global_bathroom_count, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на структура за вграждане', "Description" = 'Конзолна тоалетна.', "BasePrice" = 190, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(plumb_wc_type, ''Вградена''), global_bathroom_count, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'ВиК Услуги', 'PLMB-WC-BUILTIN';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-SHOWER-CABIN';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PLMB-SHOWER-CABIN', 'Монтаж на душ кабина или стъклен параван', 'Душ кабина.', 330, 'pcs', 'if(Contains(plumb_shower_type, ''кабина'') || Contains(plumb_shower_type, ''Вана''), global_bathroom_count, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на душ кабина или стъклен параван', "Description" = 'Душ кабина.', "BasePrice" = 330, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(plumb_shower_type, ''кабина'') || Contains(plumb_shower_type, ''Вана''), global_bathroom_count, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'ВиК Услуги', 'PLMB-SHOWER-CABIN';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-SHOWER-FIXTURE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PLMB-SHOWER-FIXTURE', 'Монтаж на душ система', 'Душ батерия.', 70, 'pcs', 'if(Contains(plumb_shower_type, ''Само'') || Contains(plumb_shower_type, ''кабина'') || Contains(plumb_shower_type, ''Вана''), global_bathroom_count, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на душ система', "Description" = 'Душ батерия.', "BasePrice" = 70, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(plumb_shower_type, ''Само'') || Contains(plumb_shower_type, ''кабина'') || Contains(plumb_shower_type, ''Вана''), global_bathroom_count, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'ВиК Услуги', 'PLMB-SHOWER-FIXTURE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-BOILER';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PLMB-BOILER', 'Монтаж на електрически бойлер', 'Бойлер до 100л.', 140, 'pcs', 'if(Contains(plumb_appliances, ''бойлер''), 1, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Монтаж на електрически бойлер', "Description" = 'Бойлер до 100л.', "BasePrice" = 140, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(plumb_appliances, ''бойлер''), 1, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'ВиК Услуги', 'PLMB-BOILER';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-APPLIANCE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PLMB-APPLIANCE', 'Свързване на пералня / съдомиялна', 'Уреди.', 80, 'pcs', 'if(Contains(plumb_appliances, ''Пералня'') && Contains(plumb_appliances, ''Съдомиялна''), 2, if(Contains(plumb_appliances, ''Пералня'') || Contains(plumb_appliances, ''Съдомиялна''), 1, 0))', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Свързване на пералня / съдомиялна', "Description" = 'Уреди.', "BasePrice" = 80, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(plumb_appliances, ''Пералня'') && Contains(plumb_appliances, ''Съдомиялна''), 2, if(Contains(plumb_appliances, ''Пералня'') || Contains(plumb_appliances, ''Съдомиялна''), 1, 0))', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'ВиК Услуги', 'PLMB-APPLIANCE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-METER-REPLACE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'PLMB-METER-REPLACE', 'Смяна на водомер', 'Нов водомер.', 60, 'pcs', 'global_bathroom_count * 2', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Смяна на водомер', "Description" = 'Нов водомер.', "BasePrice" = 60, "UnitType" = 'pcs', "CalculationFormula" = 'global_bathroom_count * 2', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'ВиК Услуги', 'PLMB-METER-REPLACE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-BATH-FULL';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DEMO-BATH-FULL', 'Цялостно къртене на баня', 'Къртене на баня.', 750, 'pcs', 'if(Contains(demo_what, ''Цяла баня''), global_bathroom_count, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Цялостно къртене на баня', "Description" = 'Къртене на баня.', "BasePrice" = 750, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(demo_what, ''Цяла баня''), global_bathroom_count, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Къртене и извозване', 'DEMO-BATH-FULL';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-WALL-BRICK';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DEMO-WALL-BRICK', 'Къртене на тухлена стена', 'Събаряне на тухлени стени.', 20, 'sqm', 'demo_brick_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Къртене на тухлена стена', "Description" = 'Събаряне на тухлени стени.', "BasePrice" = 20, "UnitType" = 'sqm', "CalculationFormula" = 'demo_brick_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Къртене и извозване', 'DEMO-WALL-BRICK';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-WALL-CONC';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DEMO-WALL-CONC', 'Къртене на бетонна стена/панел', 'Къртене на бетон.', 50, 'sqm', 'demo_conc_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Къртене на бетонна стена/панел', "Description" = 'Къртене на бетон.', "BasePrice" = 50, "UnitType" = 'sqm', "CalculationFormula" = 'demo_conc_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Къртене и извозване', 'DEMO-WALL-CONC';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-FLOOR-TILE';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DEMO-FLOOR-TILE', 'Къртене на подови настилки/замазка', 'Премахване на настилки.', 15, 'sqm', 'demo_floor_sqm', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Къртене на подови настилки/замазка', "Description" = 'Премахване на настилки.', "BasePrice" = 15, "UnitType" = 'sqm', "CalculationFormula" = 'demo_floor_sqm', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Къртене и извозване', 'DEMO-FLOOR-TILE';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-DISPOSAL';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DEMO-DISPOSAL', 'Контейнер за строителни отпадъци', 'Наемане на строителен контейнер и такса смет.', 150, 'pcs', 'if(Contains(demo_disposal, ''Да''), Ceiling((if(Contains(demo_what, ''Цяла баня''), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)), 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Контейнер за строителни отпадъци', "Description" = 'Наемане на строителен контейнер и такса смет.', "BasePrice" = 150, "UnitType" = 'pcs', "CalculationFormula" = 'if(Contains(demo_disposal, ''Да''), Ceiling((if(Contains(demo_what, ''Цяла баня''), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)), 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Къртене и извозване', 'DEMO-DISPOSAL';
    END IF;
END $$;
DO $$
DECLARE
    cat_id UUID;
    sku_id UUID;
BEGIN
    SELECT "Id" INTO cat_id FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    IF cat_id IS NOT NULL THEN
        SELECT "Id" INTO sku_id FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-LABOR-STAIRS';
        IF sku_id IS NULL THEN
            INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
            VALUES (gen_random_uuid(), cat_id, 'DEMO-LABOR-STAIRS', 'Сваляне на отпадъци по стълби', 'Ръчен труд при липса на асансьор (цена на етаж за всеки контейнер).', 10, 'floors', 'if(Contains(demo_disposal, ''Да'') && Contains(global_logistics, ''Няма асансьор''), Ceiling((if(Contains(demo_what, ''Цяла баня''), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)) * global_floor, 0)', now(), now());
        ELSE
            UPDATE "ServiceSkus"
            SET "ServiceCategoryId" = cat_id, "Name" = 'Сваляне на отпадъци по стълби', "Description" = 'Ръчен труд при липса на асансьор (цена на етаж за всеки контейнер).', "BasePrice" = 10, "UnitType" = 'floors', "CalculationFormula" = 'if(Contains(demo_disposal, ''Да'') && Contains(global_logistics, ''Няма асансьор''), Ceiling((if(Contains(demo_what, ''Цяла баня''), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)) * global_floor, 0)', "UpdatedAt" = now()
            WHERE "Id" = sku_id;
        END IF;
    ELSE
        RAISE WARNING 'Category % not found when inserting SKU %', 'Къртене и извозване', 'DEMO-LABOR-STAIRS';
    END IF;
END $$;
