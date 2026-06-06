-- =========================================================================
-- BUILDSMART MASTER LIVE SYNC - THE ULTIMATE RATIONAL PATCH (2026)
-- =========================================================================
-- This script performs a 100% synchronization of your Live database:
-- 1. Injects latest technical questions into the blueprint.
-- 2. Inserts new technical SKUs (Gerung, Mesh, RCD, etc.) using safe checks.
-- 3. Fixes ALL calculation formulas (Electrical, Drywall, Tiling, Painting).
-- 4. Corrects Unit Types and locks-in Euro pricing.
-- 5. Normalizes all currency symbols to '€'.
-- =========================================================================

BEGIN;

-- ---------------------------------------------------------
-- 0. BLUEPRINT SCHEMAS (Injecting New Questions)
-- ---------------------------------------------------------

-- Update Global Questions (Add Floor & Wall Material)
UPDATE "ServiceCategories" 
SET "TemplateStructure" = '{"questions":[{"id":"global_property_type","text":"Какъв е типът на имота?","type":"choice","required":true,"options":["Апартамент","Къща / Вила","Офис / Търговско помещение"]},{"id":"global_total_sqm","text":"Каква е общата квадратура (подова площ) на обекта в кв.м.?","type":"number","required":true},{"id":"global_ceiling_height","text":"Каква е височината на таваните?","type":"choice","required":true,"options":["Стандартна (между 2.50м и 2.70м)","Висока (над 2.70м)"]},{"id":"global_room_count","text":"Общ брой сухи помещения (спални, хол, кухня, кабинет)?","type":"number","required":true},{"id":"global_bathroom_count","text":"Колко на брой са мокрите помещения (бани и тоалетни)?","type":"number","required":true},{"id":"global_current_state","text":"Какво е текущото състояние на обекта?","type":"choice","required":true,"options":["Ново строителство (на шпакловка и замазка / БДС)","Празно жилище за основен ремонт","Обзаведено жилище (изисква местене и покриване)"]},{"id":"global_logistics","text":"Има ли осигурен достъп и паркомясто за бус/контейнер, както и работещ асансьор за качване на материали?","type":"choice","required":true,"options":["Да, има лесен достъп и асансьор","Няма асансьор (качване по стълби)","Труден достъп/Няма паркинг"]},{"id":"global_materials_supply","text":"Кой ще осигури видимите материали (плочки, санитария, ламинат, осветителни тела)?","type":"choice","required":true,"options":["Аз ще ги купя (търся само труд)","Искам майсторът да ги достави (по каталог)","Смесено (ще се уговорим допълнително)"]},{"id":"global_protection","text":"Изисква ли се ежедневно почистване и специално покриване/защита на общите части на сградата?","type":"boolean","required":true},{"id":"global_floor","text":"На кой етаж се намира обекта?","type":"number","required":true},{"id":"global_wall_material","text":"Какъв е основният материал на стените?","type":"choice","required":true,"options":["Тухла","Бетон / Панел","Гипсокартон"]}]}'
WHERE "Name" = 'Global Questions';

-- Update Electrical Questions
UPDATE "ServiceCategories" 
SET "TemplateStructure" = '{"questions":[{"id":"elec_scope","text":"Какъв е мащабът на ремонта?","type":"choice","required":true,"options":["Цялостна подмяна (всичко се изгражда наново)","Частичен ремонт (добавяне/местене на контакти и лампи)","Само монтаж (на ключове, контакти и осветителни тела)"],"hintText":"💡 Ако инсталацията ви е над 20 години, препоръчваме цялостна подмяна."},{"id":"elec_heavy_appliances","text":"Кои мощни уреди ще имате? (Изберете всички)","type":"multiselect","required":true,"options":["Фурна","Индукционен котлон","Съдомиялна","Пералня","Сушилня","Проточен бойлер"]},{"id":"elec_ac_count","text":"Колко климатика ще се захранват?","type":"choice","required":true,"options":["0","1","2","3","4+"]},{"id":"elec_outlets_comfort","text":"Колко контакти желаете във всяка стая?","type":"choice","required":true,"options":["Базово (по 3-4 на стая)","Комфорт (по 5-6 на стая)","Премиум (над 8 на стая)"],"hintText":"💡 Повечето ни клиенти избират ''Комфорт'', за да избегнат разклонители."},{"id":"elec_lighting","text":"Какъв тип ще е основното осветление?","type":"multiselect","required":true,"options":["Стандартно (полилеи/плафони)","Вградени лунички","Скрито LED осветление"]},{"id":"elec_panel","text":"Главното ел. табло ще се подменя ли?","type":"choice","required":true,"options":["Да, искам ново скрито (вградено) табло","Да, искам ново външно табло","Не, остава старото"]},{"id":"elec_rcd_needed","text":"Желаете ли монтаж на дефектнотокови защити (ДТЗ)?","type":"choice","required":true,"options":["Да, за всички кръгове","Не"]}]}'
WHERE "Name" = 'Електрическа Инсталация';


-- ---------------------------------------------------------
-- 1. INSERT NEW TECHNICAL SKUs (Safe Check)
-- ---------------------------------------------------------

-- Tiling: Gerung (45-degree cut)
INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), "Id", 'TILE-GERUNG', '45-градусово рязане (Герунг)', 'Прецизно рязане на ъгли на плочки.', 15.00, 'm', 'global_bathroom_count * 6.0', now(), now()
FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки'
AND NOT EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-GERUNG');

-- Painting: Glass Fiber Mesh
INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), "Id", 'PANT-MESH', 'Полагане на стъклофибърна мрежа', 'Армиране против пукнатини (за стара основа).', 2.50, 'sqm', 'if(!Contains(global_current_state, ''Ново строителство''), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.0), 0)', now(), now()
FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги'
AND NOT EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-MESH');

-- Painting: Filling Electrical Channels
INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), "Id", 'PANT-FILL-CHANNELS', 'Запълване на улеи от кабели', 'Запълване и заглаждане на ел. канали.', 5.00, 'm', 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 0.6, if(Contains(elec_scope, ''Частичен''), global_total_sqm * 0.2, 0))', now(), now()
FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги'
AND NOT EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-FILL-CHANNELS');

-- Painting: Corner Rectification
INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), "Id", 'PANT-CORNER-FIX', 'Изправяне на вътрешни ъгли (триъгълници)', 'Фиксиране на криви снадки между стени.', 6.50, 'm', 'if(Contains(paint_scope, ''Ремонт''), (global_total_sqm * 2.5) * 0.1, 0)', now(), now()
FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги'
AND NOT EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-CORNER-FIX');

-- Electrical: RCD (DTTZ)
INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), "Id", 'ELEC-RCD', 'Монтаж на ДТЗ', 'Дефектнотокова защита за безопасност.', 10.00, 'pcs', 'if(Contains(elec_rcd_needed, ''Да''), global_room_count + 2, 0)', now(), now()
FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация'
AND NOT EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-RCD');


-- ---------------------------------------------------------
-- 2. FIX FORMULAS & UNIT TYPES (The Logic)
-- ---------------------------------------------------------

-- NORMALIZE UNIT TYPES (Remove any currency symbols from unit names)
UPDATE "ServiceSkus" SET "UnitType" = 'sqm' WHERE "UnitType" IN ('€/кв.м', 'sq.m', 'sq m', 'кв.м');
UPDATE "ServiceSkus" SET "UnitType" = 'm' WHERE "UnitType" IN ('€/лин.м', 'лин.м', 'l.m', 'linear meter');

-- PAINTING FIX: Fix Sanding & Priming (PANT-004 & PANT-001) which were stuck at 1.00
UPDATE "ServiceSkus" SET 
    "UnitType" = 'sqm',
    "BasePrice" = 1.53,
    "CalculationFormula" = 'if(Contains(paint_scope, ''Ремонт'') || Contains(paint_scope, ''Тапети''), global_total_sqm * 2.5, 0)'
WHERE "SkuCode" IN ('PANT-001', 'PANT-004');

UPDATE "ServiceSkus" SET 
    "CalculationFormula" = 'if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)'
WHERE "SkuCode" IN ('PANT-PRIMER', 'PANT-PAINT-WHITE', 'PANT-SPACKLE-STD');

-- ELECTRICAL FIX: Chasing Multipliers (0.6x) and Tube (2.0x)
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 2.5, if(Contains(elec_scope, ''Частичен''), global_total_sqm * 1.0, 0))' WHERE "SkuCode" = 'ELEC-CABLE-LAY';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(global_wall_material, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 0.6, 0)' WHERE "SkuCode" = 'ELEC-CHASE-CONC';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(global_wall_material, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 0.6, 0)' WHERE "SkuCode" = 'ELEC-CHASE-BRICK';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 2.0, 0)' WHERE "SkuCode" = 'ELEC-LAY-TUBE';

-- DRYWALL FIX: Intelligent Insulation Summing
UPDATE "ServiceSkus" SET "CalculationFormula" = 
'if(Contains(drywall_insulation, ''Не''), 0, ' || 
    '(if(Contains(drywall_type, ''Окачен таван''), if(Contains(drywall_rooms, ''1 стая''), 15, if(Contains(drywall_rooms, ''2-3 стаи''), 40, global_total_sqm)), 0) + ' ||
    'if(Contains(drywall_type, ''Преградни стени''), if(Contains(drywall_rooms, ''1 стая''), 12, if(Contains(drywall_rooms, ''2-3 стаи''), 25, global_total_sqm * 0.2)), 0) + ' ||
    'if(Contains(drywall_type, ''Предстенна обшивка''), if(Contains(drywall_rooms, ''1 стая''), 15, if(Contains(drywall_rooms, ''2-3 стаи''), 35, global_total_sqm * 0.8)), 0))' ||
')'
WHERE "SkuCode" = 'DRYW-INSULATION';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_type, ''Куфари''), global_bathroom_count * 2.6, 0)' WHERE "SkuCode" = 'DRYW-BOX';

-- TILING FIX: Restore smart sqm (18sqm per bathroom + 15% kitchen/entry)
UPDATE "ServiceSkus" SET "CalculationFormula" = 
    'if(Contains(tile_type, ''плочки''), (global_bathroom_count * 18.0) + (global_total_sqm * 0.15), 0)'
WHERE "SkuCode" IN ('TILE-STD', 'TILE-LARGE');

-- DEMOLITION FIX: Floor Multiplier (Transport cost increases if no elevator)
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(demo_disposal, ''Да''), if(Contains(global_logistics, ''Няма асансьор''), 1 + (global_floor * 0.15), 1), 0)' WHERE "SkuCode" = 'DEMO-DISPOSAL';


UPDATE "ServiceSkus" SET "UpdatedAt" = now();

COMMIT;
