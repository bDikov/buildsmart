
-- =========================================================================
-- BUILDSMART MASTER LIVE DATABASE UPDATE (2026 EDITION) - FIXED CONSTRAINTS
-- THIS SCRIPT:
-- 1. Strips English tags from Categories and Translations
-- 2. Updates the JSON Question Templates
-- 3. Upserts all 45+ SKUs safely using PL/pgSQL DO blocks (bypassing ON CONFLICT errors)
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

UPDATE "ServiceCategories" SET "TemplateStructure" = '{}', "UpdatedAt" = now() WHERE "Name" = 'Electrical';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "q2f1a2", "text": "fssq", "type": "boolean", "required": false}, {"id": "qd59b8", "text": "cdsad", "type": "choice", "options": ["Option 1", "Option 2", "Option 3"], "required": false}, {"id": "q8d38d", "text": "ghjk", "type": "multiselect", "options": ["Option 1567", "Option 2", "Option 3", "Option 4"], "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'Cat 3';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "drywall_type", "text": "Какво ще се изгражда от гипсокартон?", "type": "multiselect", "options": ["Окачен таван", "Преградни стени", "Предстенна обшивка (на конструкция)", "Куфари (обличане на тръби)"], "required": true}, {"id": "drywall_rooms", "text": "В колко помещения ще се прави гипсокартон?", "type": "choice", "options": ["Само в банята (влагоустойчив)", "В 1 стая", "В 2-3 стаи", "В целия обект"], "required": true}, {"id": "drywall_insulation", "text": "Желаете ли поставяне на изолация (вата) зад картона?", "type": "choice", "options": ["Да, стандартна вата", "Да, специална шумоизолация", "Не"], "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'Сухо строителство';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "mico_area", "text": "Къде ще се полага микроциментът?", "type": "multiselect", "options": ["Сухи зони (подове и стени в стаи)", "Мокри зони (Баня)"], "required": true}, {"id": "mico_rooms", "text": "В колко помещения?", "type": "choice", "options": ["Само в банята", "В 1-2 стаи", "В целия обект"], "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'Микроцимент';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "demo_what", "text": "Какво точно трябва да се кърти?", "type": "multiselect", "options": ["Цяла баня (стари плочки и санитария)", "Вътрешни тухлени стени", "Бетонни/Панелни стени", "Стари подови настилки (замазка/мозайка)"], "required": true}, {"id": "demo_rooms", "text": "В колко помещения ще се извършва къртене?", "type": "choice", "options": ["Само в банята", "В 1-2 стаи", "В целия обект"], "required": true}, {"id": "demo_disposal", "text": "Желаете ли извозване на строителните отпадъци?", "type": "choice", "options": ["Да, искам контейнер и извозване", "Не, ще се справя сам"], "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'Къртене и извозване';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "tile_type", "text": "Какъв тип настилки ще се полагат?", "type": "multiselect", "options": ["Стандартни плочки (до 60х60)", "Голямоформатен гранитогрес (над 60х120)", "Ламиниран паркет"], "required": true}, {"id": "tile_rooms", "text": "Къде ще се полагат настилките?", "type": "multiselect", "options": ["Баня / Мокри помещения", "Кухня / Коридор", "Спални / Хол"], "required": true}, {"id": "tile_prep", "text": "Каква подготовка на пода е нужна?", "type": "multiselect", "options": ["Саморазливна замазка (за изравняване)", "Хидроизолация (за бани)", "Не знам (майсторът да прецени)"], "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'Подови и стенни настилки';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "paint_tasks", "text": "Какви дейности са необходими?", "type": "multiselect", "options": ["Цялостна шпакловка", "Само боядисване", "Сваляне на тапети"], "required": true}, {"id": "paint_sqm", "text": "Каква е общата площ на стените и таваните (кв.м.)?", "type": "number", "required": true}, {"id": "paint_trim_doors", "text": "Ще се боядисват ли интериорни врати или декоративни первази?", "type": "boolean", "required": true}, {"id": "paint_colors", "text": "Колко различни цвята ще се използват?", "type": "choice", "options": ["Един цвят", "2-3 цвята (акцентни стени)", "Множество цветове"], "required": true}, {"id": "paint_finish_level", "text": "Какво е очакваното ниво на завършеност на стените?", "type": "choice", "options": ["Стандартно (Q3/Q4)", "Перфектно гладко (Q5 - изисква специална шпакловка)"], "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'Painting';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "global_property_type", "text": "Какъв е типът на имота?", "type": "choice", "options": ["Апартамент", "Къща / Вила", "Офис / Търговско помещение"], "required": true}, {"id": "global_total_sqm", "text": "Каква е общата квадратура (подова площ) на обекта в кв.м.?", "type": "number", "required": true}, {"id": "global_ceiling_height", "text": "Каква е височината на таваните?", "type": "choice", "options": ["Стандартна (между 2.50м и 2.70м)", "Висока (над 2.70м)"], "required": true}, {"id": "global_room_count", "text": "Общ брой сухи помещения (спални, хол, кухня, кабинет)?", "type": "number", "required": true}, {"id": "global_bathroom_count", "text": "Колко на брой са мокрите помещения (бани и тоалетни)?", "type": "number", "required": true}, {"id": "global_current_state", "text": "Какво е текущото състояние на обекта?", "type": "choice", "options": ["Ново строителство (на шпакловка и замазка / БДС)", "Празно жилище за основен ремонт", "Обзаведено жилище (изисква местене и покриване)"], "required": true}, {"id": "global_logistics", "text": "Има ли осигурен достъп и паркомясто за бус/контейнер, както и работещ асансьор за качване на материали?", "type": "choice", "options": ["Да, има лесен достъп и асансьор", "Няма асансьор (качване по стълби)", "Труден достъп/Няма паркинг"], "required": true}, {"id": "global_materials_supply", "text": "Кой ще осигури видимите материали (плочки, санитария, ламинат, осветителни тела)?", "type": "choice", "options": ["Аз ще ги купя (търся само труд)", "Искам майсторът да ги достави (по каталог)", "Смесено (ще се уговорим допълнително)"], "required": true}, {"id": "global_protection", "text": "Изисква ли се ежедневно почистване и специално покриване/защита на общите части на сградата?", "type": "boolean", "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'Global Questions';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "paint_scope", "text": "Какво е текущото състояние на стените и какво желаете да се направи?", "type": "choice", "options": ["Освежаване (Само боядисване върху здрава основа)", "Стандартен ремонт (Шпакловка и боядисване)", "Сваляне на тапети, шпакловка и боядисване"], "hintText": "💡 Ако имате пукнатини или грапавини, изберете ''Стандартен ремонт'', за да се изгладят.", "required": true}, {"id": "paint_rooms", "text": "Кои помещения ще се боядисват?", "type": "multiselect", "options": ["Всички стаи", "Хол / Всекидневна", "Спални", "Коридор / Антре", "Само тавани (в мокри помещения)"], "required": true}, {"id": "paint_colors", "text": "Какви цветове ще използвате?", "type": "choice", "options": ["Всичко в бяло (най-бързо и бюджетно)", "Светли цветове", "Тъмни или наситени цветове", "Смесено (бял таван, цветни стени)"], "required": true}, {"id": "paint_finish_level", "text": "Какво е очакваното ниво на гладкост?", "type": "choice", "options": ["Стандартно (добро за матови и светли бои)", "Перфектно гладко (Q5 - задължително за тъмни бои и силно осветление)"], "hintText": "💡 Q5 изисква специални готови смеси и перфектно машинно шлайфане.", "required": true, "dependsOn": "paint_scope", "dependsOnValue": "Стандартен ремонт (Шпакловка и боядисване)|Сваляне на тапети, шпакловка и боядисване"}, {"id": "paint_trim_doors_count", "text": "Имате ли стари интериорни врати, които искате майсторът да реставрира и пребоядиса?", "type": "choice", "options": ["0", "1", "2", "3", "4+"], "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "plumb_scope", "text": "Какъв е мащабът на ВиК ремонта?", "type": "choice", "options": ["Цялостна подмяна (нови тръби и канали)", "Само извеждане на нови ВиК изводи (точки)", "Само монтаж (на мивки, душове, тоалетни)"], "hintText": "💡 Ако тръбите ви са стари (метални), препоръчваме цялостна подмяна с полипропилен.", "required": true}, {"id": "plumb_rooms", "text": "В кои помещения ще се извършват ВиК дейности?", "type": "multiselect", "options": ["Баня", "Кухня", "Мокро помещение / Перално", "Втора тоалетна"], "required": true}, {"id": "plumb_wc_type", "text": "Какъв тип тоалетна ще монтираме?", "type": "choice", "options": ["Стандартна (моноблок)", "Вградена структура (конзолна)", "Няма да се монтира тоалетна"], "required": true}, {"id": "plumb_shower_type", "text": "Каква ще бъде душ зоната?", "type": "choice", "options": ["Само душ батерия / окачване", "Душ кабина или стъклен параван", "Вана", "Няма да има душ"], "required": true}, {"id": "plumb_sink_count", "text": "Колко мивки (за баня и кухня) общо ще се монтират?", "type": "choice", "options": ["0", "1", "2", "3+"], "required": true}, {"id": "plumb_appliances", "text": "Какви други уреди ще свързваме към ВиК мрежата?", "type": "multiselect", "options": ["Пералня", "Съдомиялна", "Електрически бойлер (до 100л)"], "required": true}, {"id": "plumb_riser", "text": "Ще подменяме ли главния вертикален щранг (общите тръби)?", "type": "choice", "options": ["Да, искам подмяна", "Не, остават старите", "Не знам (ще се реши на място)"], "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'ВиК Услуги';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "elec_walls", "text": "От какво са направени стените ви в момента?", "type": "choice", "options": ["Бетон / Панел", "Тухла", "Гипсокартон", "Смесено"], "required": true}, {"id": "elec_floor_sqm", "text": "Колко кв.м. нова замазка на пода ще правим? (Пускането на кабели по пода преди замазка пести много къртене. 0 ако няма)", "type": "number", "required": true}, {"id": "elec_heavy_appliances", "text": "Колко на брой големи уреди ще имате?", "type": "number", "required": true}, {"id": "elec_ac_count", "text": "Колко броя климатици ще имате общо?", "type": "number", "required": true}, {"id": "elec_underfloor_heating_rooms", "text": "В колко на брой помещения ще имате подово отопление?", "type": "number", "required": true}, {"id": "elec_boiler_count", "text": "Колко бойлера (стандартни или проточни) ще имате? (Захранване)", "type": "number", "required": true}, {"id": "elec_outlets", "text": "Колко общо стандартни контакти и ключове желаете?", "type": "number", "required": true}, {"id": "elec_deviators", "text": "Колко девиаторни ключа искате?", "type": "number", "required": true}, {"id": "elec_lighting", "text": "Какъв тип ще е основното осветление?", "type": "multiselect", "options": ["Полилеи и плафони (стандартно)", "Вградени лунички / LED спотове", "Скрито LED осветление (ленти в окачен таван)", "Аплици (стенно осветление)"], "required": true}, {"id": "elec_lan_tv_count", "text": "За колко на брой стаи искате интернет кабел (LAN/TV)?", "type": "number", "required": true}, {"id": "elec_security_points", "text": "Колко слаботокови точки (камери, СОТ) ще имате?", "type": "number", "required": true}, {"id": "elec_panel", "text": "Главното ел. табло ще бъде скрито или външно?", "type": "choice", "options": ["Скрито/Вградено", "Външно/Открито"], "required": true}, {"id": "elec_blinds_count", "text": "За колко прозореца ще имате ел. щори?", "type": "number", "required": true}, {"id": "elec_fans_count", "text": "Колко вентилатора за баня ще монтираме?", "type": "number", "required": true}, {"id": "elec_three_phase", "text": "Имате ли налична партида за трифазен ток?", "type": "boolean", "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'Електрическа Инсталация ';
UPDATE "ServiceCategories" SET "TemplateStructure" = '{"questions": [{"id": "elec_scope", "text": "Какъв е мащабът на ремонта?", "type": "choice", "options": ["Цялостна подмяна (всичко се изгражда наново)", "Частичен ремонт (добавяне/местене на контакти и лампи)", "Само монтаж (на ключове, контакти и осветителни тела)"], "hintText": "💡 Ако инсталацията ви е над 20 години, препоръчваме цялостна подмяна.", "required": true}, {"id": "elec_walls", "text": "От какво са направени стените ви в момента?", "type": "choice", "options": ["Бетон / Панел (най-трудно за къртене)", "Тухла", "Гипсокартон / Окачен таван (без къртене)", "Смесено"], "required": true, "dependsOn": "elec_scope", "dependsOnValue": "Цялостна подмяна (всичко се изгражда наново)|Частичен ремонт (добавяне/местене на контакти и лампи)"}, {"id": "elec_heavy_appliances", "text": "Кои мощни уреди ще имате? (Изберете всички)", "type": "multiselect", "options": ["Фурна", "Индукционен котлон", "Съдомиялна", "Пералня", "Сушилня", "Проточен бойлер"], "required": true}, {"id": "elec_ac_count", "text": "Колко климатика ще се захранват?", "type": "choice", "options": ["0", "1", "2", "3", "4+"], "required": true}, {"id": "elec_outlets_comfort", "text": "Колко контакти желаете във всяка стая?", "type": "choice", "options": ["Базово (по 3-4 на стая)", "Комфорт (по 5-6 на стая)", "Премиум (над 8 на стая)"], "hintText": "💡 Повечето ни клиенти избират ''Комфорт'', за да избегнат разклонители.", "required": true}, {"id": "elec_lighting", "text": "Какъв тип ще е основното осветление?", "type": "multiselect", "options": ["Стандартно (полилеи/плафони)", "Вградени лунички", "Скрито LED осветление"], "required": true}, {"id": "elec_panel", "text": "Главното ел. табло ще се подменя ли?", "type": "choice", "options": ["Да, искам ново скрито (вградено) табло", "Да, искам ново външно табло", "Не, остава старото"], "required": true}]}', "UpdatedAt" = now() WHERE "Name" = 'Електрическа Инсталация';

-- 3. UPSERT SKUS WITH EURO PRICING & FORMULAS

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-006') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.09, 
            "CalculationFormula" = '', 
            "UnitType" = 'sqm', 
            "Description" = 'Шпакловане и фугиране на гипсокартон', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-006';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-006', 'Шпакловане и фугиране на гипсокартон', 'Шпакловане и фугиране на гипсокартон', 4.09, 'sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Painting';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-PREP-HYDRO') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 15.34, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Запечатване на мокри помещения.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-PREP-HYDRO';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-PREP-HYDRO', 'Полагане на хидроизолация (с лента)', 'Запечатване на мокри помещения.', 15.34, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-LV') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 15.34, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'LAN/TV/СОТ.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-POINT-LV';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-POINT-LV', 'Слаботокова точка', 'LAN/TV/СОТ.', 15.34, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-003') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.32, 
            "CalculationFormula" = '', 
            "UnitType" = '€/кв.м', 
            "Description" = 'Market range: €5 - €8', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-003';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-003', 'Боядисване с латекс (2 ръце)', 'Market range: €5 - €8', 3.32, '€/кв.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-RISER-REPLACE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 122.71, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Подмяна на основните метални тръби.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-RISER-REPLACE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-RISER-REPLACE', 'Смяна на вертикален щранг', 'Подмяна на основните метални тръби.', 122.71, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-APPLIANCE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 40.90, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Уреди.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-APPLIANCE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-APPLIANCE', 'Свързване на пералня / съдомиялна', 'Уреди.', 40.90, 'pcs', '1', now(), now()
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
            "Description" = 'Нов водомер.', 
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
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Къртене на баня.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-BATH-FULL';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-BATH-FULL', 'Цялостно къртене на баня', 'Къртене на баня.', 383.47, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-WALL-CONC') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 25.56, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Къртене на бетон.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-WALL-CONC';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-WALL-CONC', 'Къртене на бетонна стена/панел', 'Къртене на бетон.', 25.56, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-110') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.66, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €6.81 - €7.5', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-110';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-110', '���������� �� ����� �� ��������� ����� �� �23 � �����', 'Market range: €6.81 - €7.5', 3.66, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-WALL-LINING') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 20.45, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Монтаж на предстенна обшивка.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-WALL-LINING';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-WALL-LINING', 'Предстенна обшивка', 'Монтаж на предстенна обшивка.', 20.45, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-097') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 34.12, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €63.56 - €69.93', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-097';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-097', '������ (���������) ����� ����� �� 10� ��� ��������� ��� �����������', 'Market range: €63.56 - €69.93', 34.12, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-029') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.32, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €6 - €7', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-029';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-029', 'Монтаж на компютърна розетка RJ45', 'Market range: €6 - €7', 3.32, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-008') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 15.34, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Quantity (Item)', 
            "Description" = 'Труд за оформяне на конзолна кутия, заголване и подготовка на кабелите за 1 брой контакт, ключ или лампа.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-008';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-008', 'Изграждане на излазна точка (контакт/ключ/лампа)', 'Труд за оформяне на конзолна кутия, заголване и подготовка на кабелите за 1 брой контакт, ключ или лампа.', 15.34, 'Per Quantity (Item)', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-SHOWER-FIXTURE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 35.79, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Душ батерия.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-SHOWER-FIXTURE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-SHOWER-FIXTURE', 'Монтаж на душ система', 'Душ батерия.', 35.79, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-035') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 19.53, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €36.38 - €40.03', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-035';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-035', '������ �� ������������ �������� �����', 'Market range: €36.38 - €40.03', 19.53, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-PAINT-COLOR') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.35, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Боядисване с цветен латекс.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-PAINT-COLOR';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-PAINT-COLOR', 'Боядисване в цвят (2 ръце)', 'Боядисване с цветен латекс.', 4.35, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-LAMINATE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 6.14, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Полагане на ламиниран паркет.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-LAMINATE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-LAMINATE', 'Монтаж на ламинат', 'Полагане на ламиниран паркет.', 6.14, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-048') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 21.87, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €40.73 - €44.82', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-048';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-048', '������ �� ����������', 'Market range: €40.73 - €44.82', 21.87, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-002') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.58, 
            "CalculationFormula" = '', 
            "UnitType" = '€/кв.м', 
            "Description" = 'Market range: €6 - €8', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-002';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-002', 'Шпакловка (труд и груби материали)', 'Market range: €6 - €8', 3.58, '€/кв.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-117') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.38, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €8.16 - €8.98', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-117';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-117', '�������� �� ��� ����� �� �32 �� �������� ���������', 'Market range: €8.16 - €8.98', 4.38, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-073') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.28, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €13.55 - €14.92', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-073';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-073', '�������� �� ��� ��������� �� 3�4 ��� ����� �� ����� ��� �����', 'Market range: €13.55 - €14.92', 7.28, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-BOILER') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 71.58, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Бойлер до 100л.', 
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
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-FLOOR-TILE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.67, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Премахване на настилки.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-FLOOR-TILE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-FLOOR-TILE', 'Къртене на подови настилки/замазка', 'Премахване на настилки.', 7.67, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-WALL-BRICK') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 10.23, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Събаряне на тухлени стени.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-WALL-BRICK';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-WALL-BRICK', 'Къртене на тухлена стена', 'Събаряне на тухлени стени.', 10.23, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-053') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 10.22, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €19.02 - €20.93', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-053';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-053', '��������� �� ��e��������� ������������� �����', 'Market range: €19.02 - €20.93', 10.22, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-054') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 11.66, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €21.71 - €23.9', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-054';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-054', '������ �� ������������ � �����', 'Market range: €21.71 - €23.9', 11.66, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-DISPOSAL') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 127.82, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Наемане на строителен контейнер.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-DISPOSAL';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-DISPOSAL', 'Извозване с контейнер', 'Наемане на строителен контейнер.', 127.82, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CABLE-LAY') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.02, 
            "CalculationFormula" = 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, if(Contains(elec_scope, ''Частичен''), global_total_sqm * 1.0, 0))', 
            "UnitType" = 'm', 
            "Description" = 'Издърпване и фиксиране на кабел.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-CABLE-LAY';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-CABLE-LAY', 'Полагане на силов кабел', 'Издърпване и фиксиране на кабел.', 1.02, 'm', 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, if(Contains(elec_scope, ''Частичен''), global_total_sqm * 1.0, 0))', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-115') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.45, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €2.69 - €2.97', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-115';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-115', '�������� �� ��������� ����� �� �23 �� ��������� ���������', 'Market range: €2.69 - €2.97', 1.45, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-067') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.38, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €8.16 - €8.98', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-067';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-067', '�������������� �� ��������� ��� ���������� �� 25��2', 'Market range: €8.16 - €8.98', 4.38, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-016') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.40, 
            "CalculationFormula" = '', 
            "UnitType" = '€/лин.м', 
            "Description" = 'Market range: €4.4 - €5', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-016';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-016', 'Изкопаване на канал в тухла', 'Market range: €4.4 - €5', 2.40, '€/лин.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-008') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 20.45, 
            "CalculationFormula" = '', 
            "UnitType" = 'sqm', 
            "Description" = 'Декоративна облицовка.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-008';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-008', 'Облицовка с камък/тухлички', 'Декоративна облицовка.', 20.45, 'sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-098') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 29.45, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €54.85 - €60.34', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-098';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-098', '������ ( ��������� ) ����� ������ �� 6� ��� ��������� ��� ����������� �����', 'Market range: €54.85 - €60.34', 29.45, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CHASE-CONC') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.67, 
            "CalculationFormula" = 'if(Contains(elec_walls, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', 
            "UnitType" = 'm', 
            "Description" = 'Изкопаване на канал в бетон.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-CHASE-CONC';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-CHASE-CONC', 'Къртене на канал в бетон', 'Изкопаване на канал в бетон.', 7.67, 'm', 'if(Contains(elec_walls, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-010') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 17.90, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Quantity (Item)', 
            "Description" = 'Изграждане на захранваща точка за специфични моторизирани уреди (вентилатор в баня, външни ролетни щори).', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-010';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-010', 'Извод за щори / вентилатор', 'Изграждане на захранваща точка за специфични моторизирани уреди (вентилатор в баня, външни ролетни щори).', 17.90, 'Per Quantity (Item)', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-064') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.38, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €8.16 - €8.98', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-064';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-064', '���� �������� �� ����� �� 75��2', 'Market range: €8.16 - €8.98', 4.38, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-096') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 26.25, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €48.89 - €53.79', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-096';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-096', '������ (���������) ����� ����� �� 8� ��� ��������� ��� �����������', 'Market range: €48.89 - €53.79', 26.25, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-087') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.93, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €5.46 - €6.01', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-087';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-087', '��������� �� ��������� �� 2�(�� 25��2 �� 35��2) � ��������� ��������� �����', 'Market range: €5.46 - €6.01', 2.93, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-099') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 37.35, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €69.57 - €76.54', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-099';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-099', '������ ( ��������� ) ����� ������ �� 7� ��� ��������� ��� ����������� �����', 'Market range: €69.57 - €76.54', 37.35, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-037') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.21, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €7.85 - €8.64', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-037';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-037', '������ �� ������� �� �����', 'Market range: €7.85 - €8.64', 4.21, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-PAINT-WHITE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.32, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Боядисване с бял латекс.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-PAINT-WHITE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-PAINT-WHITE', 'Боядисване в бяло (2 ръце)', 'Боядисване с бял латекс.', 3.32, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-041') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 12.25, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €22.82 - €25.11', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-041';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-041', '������ �� ������������ ���������� "EXIT"', 'Market range: €22.82 - €25.11', 12.25, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-006') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 9.20, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per sqm', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-006';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-006', 'Епоксидна фуга', '', 9.20, 'Per sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-085') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.51, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €8.4 - €9.25', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-085';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-085', '��������� �� ��������� �� 4�(�� 10��2 �� 16��2) � ��������� ��������� �����', 'Market range: €8.4 - €9.25', 4.51, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-066') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.17, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.04 - €4.46', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-066';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-066', '�������������� �� ��������� ��� ���������� �� 10��2', 'Market range: €4.04 - €4.46', 2.17, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-141') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 45.36, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €84.48 - €92.94', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-141';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-141', '������� ������� ������', 'Market range: €84.48 - €92.94', 45.36, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-061') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.45, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €2.69 - €2.97', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-061';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-061', '���� �������� �� ����� �� 4��2', 'Market range: €2.69 - €2.97', 1.45, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-121') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.38, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €8.16 - €8.98', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-121';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-121', '����������� ���������� �������� �� ����������� ���� 40/4 ��', 'Market range: €8.16 - €8.98', 4.38, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-055') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 17.49, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €32.57 - €35.84', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-055';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-055', '������ �� �������������� ������ � �����', 'Market range: €32.57 - €35.84', 17.49, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-005') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 12.78, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Item', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-005';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-005', 'Боядисване на врати и первази', '', 12.78, 'Per Item', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Painting';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-031') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.93, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €5.46 - €6.01', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-031';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-031', '������ �� �������� �� ������ �����������������', 'Market range: €5.46 - €6.01', 2.93, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-130') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 5.83, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €10.85 - €11.95', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-130';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-130', '������ �� ��������� �������� �����', 'Market range: €10.85 - €11.95', 5.83, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-068') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.66, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €6.81 - €7.5', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-068';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-068', '�������������� �� ��������� ��� ���������� �� 75��2', 'Market range: €6.81 - €7.5', 3.66, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-107') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.47, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.6 - €5.07', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-107';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-107', '������ �� ������ �� ������� ������ �� 120��', 'Market range: €4.6 - €5.07', 2.47, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-092') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 26.25, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €48.89 - €53.79', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-092';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-092', '������ ( ��������� ) ����� ��� ������� �� 8� ����� (������) ���������', 'Market range: €48.89 - €53.79', 26.25, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-039') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 8.76, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €16.32 - €17.96', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-039';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-039', '������ �� ����������� ���� ����', 'Market range: €16.32 - €17.96', 8.76, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-007') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.67, 
            "CalculationFormula" = '', 
            "UnitType" = 'm', 
            "Description" = 'Къртене на канал за тръби.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-007';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-007', 'Скрити тръби (къртене)', 'Къртене на канал за тръби.', 7.67, 'm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-042') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 13.14, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €24.48 - €26.93', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-042';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-042', '������ �� ������������� ����������� ���� �� �������� �����', 'Market range: €24.48 - €26.93', 13.14, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-123') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 65.65, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €122.28 - €134.51', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-123';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-123', '������������ ��. ���������� ������ �� ������������ ������� �� 4�', 'Market range: €122.28 - €134.51', 65.65, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-002') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 33.23, 
            "CalculationFormula" = '', 
            "UnitType" = '€/куб.м', 
            "Description" = 'Market range: €60 - €70', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-002';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-002', 'Къртене на бетон', 'Market range: €60 - €70', 33.23, '€/куб.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-025') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 11.94, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €21.7 - €25', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-025';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-025', 'Монтаж на предпазители в табло', 'Market range: €21.7 - €25', 11.94, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-119') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 8.76, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €16.32 - €17.96', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-119';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-119', '�������� �� ��� ����� �� �110 �� �������� ���������', 'Market range: €16.32 - €17.96', 8.76, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-022') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.07, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €5.5 - €6.5', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-022';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-022', 'Монтаж на контакт за скрита електроинсталация', 'Market range: €5.5 - €6.5', 3.07, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-113') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 5.11, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.9 - €5.4', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-113';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-113', '���������� �� ������������� ����� � ������� �����', 'Market range: €4.9 - €5.4', 5.11, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-126') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.06, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €5.7 - €6.28', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-126';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-126', '������ �� ������������ ����� �� ������� ������� �10��2', 'Market range: €5.7 - €6.28', 3.06, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-084') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.20, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €5.95 - €6.55', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-084';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-084', '��������� �� ��������� �� 3�(�� 10��2 �� 16��2) � ��������� ��������� �����', 'Market range: €5.95 - €6.55', 3.20, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-005') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 6.14, 
            "CalculationFormula" = '', 
            "UnitType" = 'sqm', 
            "Description" = 'Премахване на подова замазка.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-005';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-005', 'Къртене на замазка', 'Премахване на подова замазка.', 6.14, 'sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-106') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.05, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €3.8 - €4.19', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-106';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-106', '������ �� ������ �� ������� ������ �� 60��', 'Market range: €3.8 - €4.19', 2.05, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-RCD-INSTALL') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 17.90, 
            "CalculationFormula" = '', 
            "UnitType" = 'pcs', 
            "Description" = 'Специфично подвързване на ДТЗ за влажни зони и токови кръгове.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-RCD-INSTALL';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-RCD-INSTALL', 'Монтаж на Дефектнотокова защита (ДТЗ)', 'Специфично подвързване на ДТЗ за влажни зони и токови кръгове.', 17.90, 'pcs', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Electrical';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-LED-TRAFO') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 15.34, 
            "CalculationFormula" = 'if(Contains(elec_lighting, ''LED''), 1, 0)', 
            "UnitType" = 'pcs', 
            "Description" = 'Монтаж и подвързване на трансформатор за скрито LED осветление.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-LED-TRAFO';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-LED-TRAFO', 'Монтаж на захранващ блок (Траф) за LED', 'Монтаж и подвързване на трансформатор за скрито LED осветление.', 15.34, 'pcs', 'if(Contains(elec_lighting, ''LED''), 1, 0)', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Electrical';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-INSULATION') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 5.11, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Поставяне на минерална или каменна вата.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-INSULATION';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-INSULATION', 'Монтаж на вата (Топло/Шумо)', 'Поставяне на минерална или каменна вата.', 5.11, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-049') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.47, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.6 - €5.07', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-049';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-049', '������ �� ������������ ����� ��������', 'Market range: €4.6 - €5.07', 2.47, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-051') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.79, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €7.05 - €7.77', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-051';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-051', '������ �� ��. ����� ������������� ��������� �� ������ ������� ����������', 'Market range: €7.05 - €7.77', 3.79, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-LAY-TUBE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.05, 
            "CalculationFormula" = 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', 
            "UnitType" = 'm', 
            "Description" = 'Полагане на гофрирана тръба.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-LAY-TUBE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-LAY-TUBE', 'Полагане на гофре', 'Полагане на гофрирана тръба.', 2.05, 'm', 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-102') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.66, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €6.81 - €7.5', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-102';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-102', '������ �� ������� ������ � ������ �� 20�� �� 60��', 'Market range: €6.81 - €7.5', 3.66, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'MICRO-BATH') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 92.03, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Микроцимент за баня.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'MICRO-BATH';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'MICRO-BATH', 'Полагане на микроцимент в мокри зони (Баня)', 'Микроцимент за баня.', 92.03, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-004') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 38.35, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €70 - €80', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-004';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-004', 'Монтаж на бойлер', 'Market range: €70 - €80', 38.35, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-088') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.95, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €7.36 - €8.1', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-088';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-088', '��������� �� ��������� �� 3�(�� 25��2 �� 35��2) � ��������� ��������� �����', 'Market range: €7.36 - €8.1', 3.95, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-014') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 17.90, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Quantity (Item)', 
            "Description" = 'Специфично подвързване на ДТЗ за влажни зони и токови кръгове.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-014';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-014', 'Монтаж на Дефектнотокова защита (ДТЗ)', 'Специфично подвързване на ДТЗ за влажни зони и токови кръгове.', 17.90, 'Per Quantity (Item)', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-004') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 23.01, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per linear meter', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-004';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-004', 'Изграждане на сложни форми/арки', '', 23.01, 'Per linear meter', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-017') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.40, 
            "CalculationFormula" = '', 
            "UnitType" = '€/лин.м', 
            "Description" = 'Market range: €8.2 - €9', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-017';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-017', 'Изкопаване на канал в бетон', 'Market range: €8.2 - €9', 4.40, '€/лин.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-079') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.05, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €3.8 - €4.19', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-079';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-079', '��������� �� ��������� �� 2�(�� 4��2 �� 6��2) � ��������� ��������� �����', 'Market range: €3.8 - €4.19', 2.05, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-127') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 8.60, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €16.01 - €17.62', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-127';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-127', '������������ ��. ���������� ������ �� ����������� ����������', 'Market range: €16.01 - €17.62', 8.60, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-036') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.20, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €5.95 - €6.55', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-036';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-036', '������ �� ������� ������', 'Market range: €5.95 - €6.55', 3.20, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-005') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 6.14, 
            "CalculationFormula" = '', 
            "UnitType" = 'sqm', 
            "Description" = 'Изграждане на метална конструкция', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-005';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-005', 'Изграждане на метална конструкция', 'Изграждане на метална конструкция', 6.14, 'sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Painting';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-027') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 15.13, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €27.2 - €32', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-027';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-027', 'Монтаж на аплик, плафониера', 'Market range: €27.2 - €32', 15.13, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-001') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.67, 
            "CalculationFormula" = '', 
            "UnitType" = '€/кв.м', 
            "Description" = 'Market range: €10 - €20', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-001';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-001', 'Къртене на фаянс/теракота', 'Market range: €10 - €20', 7.67, '€/кв.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-WALLPAPER-REMOVE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.30, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Сваляне на стари тапети.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-WALLPAPER-REMOVE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-WALLPAPER-REMOVE', 'Сваляне на стари тапети', 'Сваляне на стари тапети.', 2.30, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-006') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 102.26, 
            "CalculationFormula" = '', 
            "UnitType" = 'pcs', 
            "Description" = 'Монтаж на конзолна тоалетна.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-006';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-006', 'Вградена структура', 'Монтаж на конзолна тоалетна.', 102.26, 'pcs', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-018') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.87, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €3.3 - €4', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-018';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-018', 'Изкопаване на конзолна кутия в тухла', 'Market range: €3.3 - €4', 1.87, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-118') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 5.83, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €10.85 - €11.95', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-118';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-118', '�������� �� ��� ����� �� �75 �� �������� ���������', 'Market range: €10.85 - €11.95', 5.83, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 30.68, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Полагане на фаянс или теракот.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-STD', 'Лепене на стандартни плочки', 'Полагане на фаянс или теракот.', 30.68, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-006') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.67, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per sqm', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-006';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-006', 'Шпакловка Q5 (Перфектно гладка)', '', 7.67, 'Per sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Painting';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-001') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 148.27, 
            "CalculationFormula" = '', 
            "UnitType" = '€/обект', 
            "Description" = 'Market range: €280 - €300', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-001';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-001', 'Подмяна и монтаж на водопровод (цялостна разводка)', 'Market range: €280 - €300', 148.27, '€/обект', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-007') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.67, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per sqm', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-007';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-007', 'Сложна шарка (Рибена кост) - Добавка', '', 7.67, 'Per sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-TAPE-CORNER') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.07, 
            "CalculationFormula" = '1', 
            "UnitType" = 'm', 
            "Description" = 'Алуминиеви или PVC ъгли.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-TAPE-CORNER';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-TAPE-CORNER', 'Поставяне на ъглохранители', 'Алуминиеви или PVC ъгли.', 3.07, 'm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-103') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 5.11, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €9.51 - €10.47', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-103';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-103', '������ �� ������� ������ � ������ �� 80�� �� 120��', 'Market range: €9.51 - €10.47', 5.11, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-086') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.75, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €3.25 - €3.58', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-086';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-086', '��������� �� ��������� �� 1�(�� 25��2 �� 35��2) � ��������� ��������� �����', 'Market range: €3.25 - €3.58', 1.75, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-SPEC') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 20.45, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Вентилатори или щори.', 
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
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-116') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.17, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.04 - €4.46', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-116';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-116', '�������� �� ��������� ����� �� �32 �� �������� ���������', 'Market range: €4.04 - €4.46', 2.17, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-001') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 12.78, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Item', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-001';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-001', 'Монтаж на контакт', '', 12.78, 'Per Item', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-133') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 29.18, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €54.36 - €59.8', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-133';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-133', '������ �� �����-����������� (���������)', 'Market range: €54.36 - €59.8', 29.18, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-142') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 48.12, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €89.63 - €98.61', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-142';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-142', '��������� ���������� - ����� �� ��������� �������', 'Market range: €89.63 - €98.61', 48.12, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-136') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.02, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €1.9 - €2.1', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-136';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-136', '��������� �� ������������ ����� � ��������� ��������� � ��������� �����', 'Market range: €1.9 - €2.1', 1.02, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-134') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 43.74, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €81.47 - €89.63', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-134';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-134', '������ �� �����������', 'Market range: €81.47 - €89.63', 43.74, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-139') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.62, 
            "CalculationFormula" = '', 
            "UnitType" = '�/9.70 ����', 
            "Description" = 'Market range: €6.74 - €7.43', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-139';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-139', '������ �� ���������� ��������� �������, RJ 45, RJ', 'Market range: €6.74 - €7.43', 3.62, '�/9.70 ����', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'GEN-003') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 15.34, 
            "CalculationFormula" = '1', 
            "UnitType" = 'flat', 
            "Description" = 'Ежедневно почистване', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'GEN-003';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'GEN-003', 'Ежедневно почистване', 'Ежедневно почистване', 15.34, 'flat', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Painting';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-046') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 36.47, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €67.92 - €74.72', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-046';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-046', '������ �� ������� ����������� ���� � �������� �� 1.5�', 'Market range: €67.92 - €74.72', 36.47, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-108') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.21, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €7.85 - €8.64', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-108';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-108', '������ �� �������, �������� � ������� ������', 'Market range: €7.85 - €8.64', 4.21, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-072') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.47, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.6 - €5.07', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-072';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-072', '�������� �� ��� ��������� �� 3�10 �� ����� ��� �����', 'Market range: €4.6 - €5.07', 2.47, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-101') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 60.97, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €113.56 - €124.93', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-101';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-101', '������ ( ��������� ) ����� ������ �� 10� ��� ��������� ��� ����������� �����', 'Market range: €113.56 - €124.93', 60.97, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-050') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.36, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €6.25 - €6.89', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-050';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-050', '������ �� ��. ����� ������������� �� ������ ������� ����������', 'Market range: €6.25 - €6.89', 3.36, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'MICRO-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 71.58, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Полагане на микроцимент.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'MICRO-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'MICRO-STD', 'Полагане на микроцимент (сухи зони)', 'Полагане на микроцимент.', 71.58, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-034') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 6.55, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €12.2 - €13.44', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-034';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-034', '������ �� �������� ��. �������', 'Market range: €12.2 - €13.44', 6.55, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-PREP-LEVEL') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 11.25, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Изравняване на пода.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-PREP-LEVEL';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-PREP-LEVEL', 'Саморазливна замазка', 'Изравняване на пода.', 11.25, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-026') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 18.05, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €32.6 - €38', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-026';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-026', 'Монтаж на дефектнотокова защита в табло', 'Market range: €32.6 - €38', 18.05, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-125') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.34, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.35 - €4.8', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-125';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-125', '������ �� ������������ ����� �� ������� ������� �8��2', 'Market range: €4.35 - €4.8', 2.34, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-058') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 26.68, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €49.69 - €54.67', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-058';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-058', '������ �� ������� ��������', 'Market range: €49.69 - €54.67', 26.68, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-078') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.16, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €2.14 - €2.37', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-078';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-078', '��������� �� ��������� �� 1�(�� 4��2 �� 6��2) � ��������� ��������� �����', 'Market range: €2.14 - €2.37', 1.16, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'MICO-002') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 43.46, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per sqm', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'MICO-002';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'MICO-002', 'Микроцимент за мокри помещения (с лак)', '', 43.46, 'Per sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-019') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.40, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €8.2 - €9', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-019';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-019', 'Изкопаване на конзолна кутия в бетон', 'Market range: €8.2 - €9', 4.40, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-TRIM') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 23.01, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Боядисване на декоративни елементи.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-TRIM';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-TRIM', 'Боядисване на врати / первази', 'Боядисване на декоративни елементи.', 23.01, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-BOX') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 20.45, 
            "CalculationFormula" = '1', 
            "UnitType" = 'm', 
            "Description" = 'Обличане на тръби.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-BOX';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-BOX', 'Изграждане на куфари (Кутии)', 'Обличане на тръби.', 20.45, 'm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CABLE-HEAVY') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.56, 
            "CalculationFormula" = '(Count(elec_heavy_appliances) + elec_ac_count) * 10', 
            "UnitType" = 'm', 
            "Description" = 'Дебел кабел за проточни бойлери.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-CABLE-HEAVY';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-CABLE-HEAVY', 'Полагане на мощен кабел', 'Дебел кабел за проточни бойлери.', 2.56, 'm', '(Count(elec_heavy_appliances) + elec_ac_count) * 10', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-SPACKLE-Q5') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 10.23, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Шитрок за идеално гладка повърхност.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-SPACKLE-Q5';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-SPACKLE-Q5', 'Фина шпакловка (Перфектна Q5)', 'Шитрок за идеално гладка повърхност.', 10.23, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-044') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 31.23, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €58.16 - €63.99', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-044';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-044', '������ �� ������������� ����������� ���� �� �������', 'Market range: €58.16 - €63.99', 31.23, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-124') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 72.93, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €135.84 - €149.43', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-124';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-124', '������������ ��. ���������� ������ �� ������������ ������� �� 6�', 'Market range: €135.84 - €149.43', 72.93, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-011') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 12.78, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Quantity (Item)', 
            "Description" = 'Полагане на FTP/Коаксиален кабел и подготовка на конзола за интернет, телевизия или охранителна техника.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-011';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-011', 'Изграждане на слаботокова точка (LAN/TV/СОТ)', 'Полагане на FTP/Коаксиален кабел и подготовка на конзола за интернет, телевизия или охранителна техника.', 12.78, 'Per Quantity (Item)', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-069') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.16, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €2.14 - €2.37', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-069';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-069', '�������� �� ��������� ���� ��� ������� �� ������� (��������) �����', 'Market range: €2.14 - €2.37', 1.16, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-135') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.75, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €3.25 - €3.58', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-135';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-135', '�������� �� ��������� ����� �� �23', 'Market range: €3.25 - €3.58', 1.75, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-002') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 140.61, 
            "CalculationFormula" = '', 
            "UnitType" = '€/обект', 
            "Description" = 'Market range: €250 - €300', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-002';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-002', 'Подмяна и полагане на канални тръби', 'Market range: €250 - €300', 140.61, '€/обект', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-WC-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 71.58, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Монтаж на тоалетна.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-WC-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-WC-STD', 'Монтаж на стандартна тоалетна (моноблок)', 'Монтаж на тоалетна.', 71.58, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-007') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.53, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Linear Meter', 
            "Description" = 'Издърпване на дебел кабел за проточни бойлери или трифазни консуматори.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-007';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-007', 'Полагане на мощен/трифазен кабел (6мм2+)', 'Издърпване на дебел кабел за проточни бойлери или трифазни консуматори.', 1.53, 'Per Linear Meter', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-013') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 6.14, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Quantity (Item)', 
            "Description" = 'Подреждане на предпазители, ДТЗ и гребени. Цената е за 1 модул (1 предпазител = 1 модул).', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-013';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-013', 'Сглобяване и подвързване на табло (на модул)', 'Подреждане на предпазители, ДТЗ и гребени. Цената е за 1 модул (1 предпазител = 1 модул).', 6.14, 'Per Quantity (Item)', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-112') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.38, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €3.25 - €3.58', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-112';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-112', '���������� �� �������� ����� � ������� �����', 'Market range: €3.25 - €3.58', 4.38, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-075') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.16, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €2.14 - €2.37', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-075';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-075', '��������� �� ��������� �� 2�(�� 0,5��2 �� 2,5��2) � ��������� ��������� �����', 'Market range: €2.14 - €2.37', 1.16, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-070') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.45, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €2.69 - €2.97', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-070';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-070', '�������� �� ��������� ���� ��� ������� �� �����', 'Market range: €2.69 - €2.97', 1.45, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-WALL-PARTITION') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 33.23, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Изграждане на преградна стена.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-WALL-PARTITION';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-WALL-PARTITION', 'Преградна стена (Двуслойна)', 'Изграждане на преградна стена.', 33.23, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-080') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.34, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.35 - €4.8', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-080';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-080', '��������� �� ��������� �� 3�(�� 4��2 �� 6��2) � ��������� ��������� �����', 'Market range: €4.35 - €4.8', 2.34, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-045') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 21.87, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €40.73 - €44.82', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-045';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-045', '������ �� ��������� �� 500W', 'Market range: €40.73 - €44.82', 21.87, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-004') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.07, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Linear Meter', 
            "Description" = 'Изкопаване на канал за полагане на кабели в тухлена зидария.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-004';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-004', 'Къртене на канал в тухла', 'Изкопаване на канал за полагане на кабели в тухлена зидария.', 3.07, 'Per Linear Meter', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-003') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.16, 
            "CalculationFormula" = '', 
            "UnitType" = '€/лин.м', 
            "Description" = 'Market range: €10 - €18', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-003';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-003', 'Обръщане на прозорци и врати след дограма', 'Market range: €10 - €18', 7.16, '€/лин.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-002') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.58, 
            "CalculationFormula" = '', 
            "UnitType" = '€/лин.м', 
            "Description" = 'Market range: €6 - €8', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-002';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-002', 'Монтаж на цокъл (рязан)', 'Market range: €6 - €8', 3.58, '€/лин.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-040') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 10.22, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €19.02 - €20.93', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-040';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-040', '������ �� ��������� ������������� �� ����������� ���� ����', 'Market range: €19.02 - €20.93', 10.22, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-144') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 11.66, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €21.71 - €23.9', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-144';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-144', '�������� �� ��������� ��������� �� ����������', 'Market range: €21.71 - €23.9', 11.66, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-032') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.21, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €7.85 - €8.64', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-032';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-032', '������ �� ������� �� ������� �����������������', 'Market range: €7.85 - €8.64', 4.21, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-005') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 61.36, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Item', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-005';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-005', 'Преместване на ВиК точка (къртене и тръби)', '', 61.36, 'Per Item', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-004') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 0.77, 
            "CalculationFormula" = '', 
            "UnitType" = '€/кв.м', 
            "Description" = 'Market range: €1 - €2', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-004';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-004', 'Шлайфане на стени', 'Market range: €1 - €2', 0.77, '€/кв.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-CHASE-BRICK') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.09, 
            "CalculationFormula" = 'if(Contains(elec_walls, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', 
            "UnitType" = 'm', 
            "Description" = 'Изкопаване на канал за полагане на кабели в тухлена зидария.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-CHASE-BRICK';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-CHASE-BRICK', 'Къртене на канал в тухла', 'Изкопаване на канал за полагане на кабели в тухлена зидария.', 4.09, 'm', 'if(Contains(elec_walls, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Electrical';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-128') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 43.74, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €81.47 - €89.63', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-128';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-128', '������ �� ��������� ��������', 'Market range: €81.47 - €89.63', 43.74, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-057') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 35.74, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €66.57 - €73.23', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-057';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-057', '������ �� ���������� ���������', 'Market range: €66.57 - €73.23', 35.74, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-004') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.05, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per sqm', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-004';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-004', 'Сваляне на тапети', '', 2.05, 'Per sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-105') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.47, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.6 - €5.07', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-105';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-105', '������ �� ������������ �� ������� ������ �� 120��', 'Market range: €4.6 - €5.07', 2.47, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-009') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 23.01, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Quantity (Item)', 
            "Description" = 'Специфично опроводяване за управление на едно осветление от 2 или повече места.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-009';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-009', 'Изграждане на девиаторна/кръстата точка', 'Специфично опроводяване за управление на едно осветление от 2 или повече места.', 23.01, 'Per Quantity (Item)', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-004') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 5.88, 
            "CalculationFormula" = '', 
            "UnitType" = '€/кв.м', 
            "Description" = 'Market range: €9 - €14', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-004';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-004', 'Полагане на подова замазка (до 5 см)', 'Market range: €9 - €14', 5.88, '€/кв.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-122') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 8.60, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €16.01 - €17.62', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-122';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-122', '����������� ���������� ������ �� ����������� ���� 40/4 �� �� �����', 'Market range: €16.01 - €17.62', 8.60, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-120') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 29.75, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €55.4 - €60.95', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-120';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-120', '����������� ���������� �������� �� ���������� ����������� ���', 'Market range: €55.4 - €60.95', 29.75, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-129') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 51.06, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €95.1 - €104.62', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-129';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-129', '������ �� �������������� ��������', 'Market range: €95.1 - €104.62', 51.06, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-063') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.93, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €5.46 - €6.01', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-063';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-063', '���� �������� �� ����� �� 25��2', 'Market range: €5.46 - €6.01', 2.93, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-059') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 30.64, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €57.06 - €62.77', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-059';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-059', '������ �� ��������� �������', 'Market range: €57.06 - €62.77', 30.64, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-132') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 26.25, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €48.89 - €53.79', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-132';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-132', '������ �� �������������� ������', 'Market range: €48.89 - €53.79', 26.25, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-038') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 14.59, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €27.18 - €29.9', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-038';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-038', '������ �� �����, ����������', 'Market range: €27.18 - €29.9', 14.59, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-001') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 0.77, 
            "CalculationFormula" = '', 
            "UnitType" = '€/кв.м', 
            "Description" = 'Market range: €1 - €2', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-001';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-001', 'Грундиране', 'Market range: €1 - €2', 0.77, '€/кв.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'MICO-003') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 10.23, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per sqm', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'MICO-003';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'MICO-003', 'Подготовка на основата (мрежа и шпакловка върху стари плочки)', '', 10.23, 'Per sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-POINT-NEW') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 35.79, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Тръби за топла, студена вода и канал.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-POINT-NEW';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-POINT-NEW', 'Изграждане на нова ВиК точка', 'Тръби за топла, студена вода и канал.', 35.79, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'GEN-002') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.02, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Окончателно почистване на обекта', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'GEN-002';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'GEN-002', 'Окончателно почистване на обекта', 'Окончателно почистване на обекта', 1.02, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Painting';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-009') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 6.14, 
            "CalculationFormula" = '', 
            "UnitType" = 'sqm', 
            "Description" = 'Нивелиране на под.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-009';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-009', 'Саморазливна замазка', 'Нивелиране на под.', 6.14, 'sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-001') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 11.76, 
            "CalculationFormula" = '', 
            "UnitType" = '€/кв.м', 
            "Description" = 'Market range: €18 - €28', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-001';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-001', 'Монтаж на окачен таван (гипсокартон)', 'Market range: €18 - €28', 11.76, '€/кв.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-006') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 0.77, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Linear Meter', 
            "Description" = 'Издърпване и фиксиране на кабел СВТ/ПВВ-МБ1 (напр. 3х1.5, 3х2.5, 3х4).', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-006';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-006', 'Полагане на силов кабел (до 4мм2)', 'Издърпване и фиксиране на кабел СВТ/ПВВ-МБ1 (напр. 3х1.5, 3х2.5, 3х4).', 0.77, 'Per Linear Meter', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-140') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.02, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €1.9 - €2.1', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-140';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-140', '��������� �� ���������� ����� � ��������� FTP � ��������� �����', 'Market range: €1.9 - €2.1', 1.02, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-138') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.20, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €5.95 - €6.55', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-138';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-138', '������ �� ������������ �������', 'Market range: €5.95 - €6.55', 3.20, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-043') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 18.08, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €33.68 - €37.06', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-043';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-043', '������ �� ������������� ����������� ���� �� ����� ��� �����', 'Market range: €33.68 - €37.06', 18.08, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-003') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 3.58, 
            "CalculationFormula" = '', 
            "UnitType" = '€/кв.м', 
            "Description" = 'Market range: €5 - €9', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-003';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-003', 'Редене на ламинат', 'Market range: €5 - €9', 3.58, '€/кв.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-111') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 5.83, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €10.85 - €11.95', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-111';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-111', '���������� �� ����� �� ��������� ����� �� �29 � �����', 'Market range: €10.85 - €11.95', 5.83, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-SHOWER-CABIN') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 168.73, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Душ кабина.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-SHOWER-CABIN';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-SHOWER-CABIN', 'Монтаж на душ кабина или стъклен параван', 'Душ кабина.', 168.73, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-076') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.45, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €2.69 - €2.97', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-076';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-076', '��������� �� ��������� �� 3�(�� 0,5��2 �� 2,5��2) � ��������� ��������� �����', 'Market range: €2.69 - €2.97', 1.45, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-143') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.38, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €8.16 - €8.98', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-143';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-143', '��������� ���������� ��������� �������� �����', 'Market range: €8.16 - €8.98', 4.38, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-020') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.66, 
            "CalculationFormula" = '', 
            "UnitType" = '€/лин.м', 
            "Description" = 'Market range: €3 - €3.5', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-020';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-020', 'Полагане на СВТ проводник до 3х4', 'Market range: €3 - €3.5', 1.66, '€/лин.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-095') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 22.30, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €41.53 - €45.7', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-095';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-095', '������ (���������) ����� ����� �� 7� ��� ��������� ��� �����������', 'Market range: €41.53 - €45.7', 22.30, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DEMO-003') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 23.01, 
            "CalculationFormula" = '', 
            "UnitType" = '€/курс', 
            "Description" = 'Market range: €40 - €50', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DEMO-003';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DEMO-003', 'Извозване на строителни отпадъци', 'Market range: €40 - €50', 23.01, '€/курс', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-PRIMER') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.53, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Грундиране на стени и тавани.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-PRIMER';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-PRIMER', 'Дълбокопроникващ грунд', 'Грундиране на стени и тавани.', 1.53, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-LARGE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 48.57, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Плочи над 60х120 см.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-LARGE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-LARGE', 'Лепене на голямоформатен гранитогрес', 'Плочи над 60х120 см.', 48.57, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PANT-SPACKLE-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.16, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Цялостна шпакловка.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PANT-SPACKLE-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PANT-SPACKLE-STD', 'Шпакловка (Стандартна 2 ръце)', 'Цялостна шпакловка.', 7.16, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-131') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 11.66, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €21.71 - €23.9', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-131';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-131', '������ �� ��������� ������', 'Market range: €21.71 - €23.9', 11.66, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-093') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 34.12, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €63.56 - €69.93', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-093';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-093', '������ ( ��������� ) ����� ��� ������� �� 10� ����� (������) ���������', 'Market range: €63.56 - €69.93', 34.12, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-002') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 10.23, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Quantity (Item)', 
            "Description" = '20', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-002';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-002', 'Монтаж на ключ', '20', 10.23, 'Per Quantity (Item)', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-SINK-INSTALL') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 46.02, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Монтаж на мивки.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-SINK-INSTALL';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-SINK-INSTALL', 'Монтаж на мивка със смесител и сифон', 'Монтаж на мивки.', 46.02, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-104') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.05, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €3.8 - €4.19', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-104';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-104', '������ �� ������������ �� ������� ������ �� 60��', 'Market range: €3.8 - €4.19', 2.05, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-012') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 40.90, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Quantity (Item)', 
            "Description" = 'Къртене на голям отвор в стената за скрит монтаж на апартаментно електрическо табло.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-012';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-012', 'Изкопаване на ниша за вградено табло', 'Къртене на голям отвор в стената за скрит монтаж на апартаментно електрическо табло.', 40.90, 'Per Quantity (Item)', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-005') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 6.14, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per sqm', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-005';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-005', 'Полагане на хидроизолация', '', 6.14, 'Per sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-015') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 12.78, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Quantity (Item)', 
            "Description" = 'Монтаж и подвързване на трансформатор за скрито LED осветление.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-015';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-015', 'Монтаж на захранващ блок (Траф) за LED', 'Монтаж и подвързване на трансформатор за скрито LED осветление.', 12.78, 'Per Quantity (Item)', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-091') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 22.30, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €41.53 - €45.7', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-091';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-091', '������ ( ��������� ) ����� ��� ������� �� 7� ����� (������) ���������', 'Market range: €41.53 - €45.7', 22.30, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-062') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.17, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.04 - €4.46', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-062';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-062', '���� �������� �� ����� �� 10��2', 'Market range: €4.04 - €4.46', 2.17, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-023') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.79, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €4.9 - €6', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-023';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-023', 'Монтаж на ключ за скрита електроинсталация', 'Market range: €4.9 - €6', 2.79, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-024') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 26.41, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €48.3 - €55', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-024';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-024', 'Монтаж на апартаментно табло', 'Market range: €48.3 - €55', 26.41, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-DEV') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 28.12, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Девиаторни ключове.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-POINT-DEV';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-POINT-DEV', 'Девиаторна точка', 'Девиаторни ключове.', 28.12, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-056') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 75.86, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €141.3 - €155.44', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-056';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-056', '������ �� ���������� ��������', 'Market range: €141.3 - €155.44', 75.86, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-100') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 45.23, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €84.24 - €92.67', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-100';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-100', '������ ( ��������� ) ����� ������ �� 8� ��� ��������� ��� ����������� �����', 'Market range: €84.24 - €92.67', 45.23, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-028') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 9.02, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €16.3 - €19', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-028';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-028', 'Монтаж на осветително тяло луна', 'Market range: €16.3 - €19', 9.02, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-030') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.63, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.9 - €5.4', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-030';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-030', '������ �� ������� �� ������ �����������������', 'Market range: €4.9 - €5.4', 2.63, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-005') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.53, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Linear Meter', 
            "Description" = 'Полагане на гофрирана тръба с кабел по под (преди замазка) или зад окачен таван/гипсокартон. Не изисква къртене.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-005';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-005', 'Полагане на кабел в гофре (под/гипсокартон)', 'Полагане на гофрирана тръба с кабел по под (преди замазка) или зад окачен таван/гипсокартон. Не изисква къртене.', 1.53, 'Per Linear Meter', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-114') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.45, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €2.69 - €2.97', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-114';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-114', '�������� �� �������� (������������� �����) �� ��������� ���������', 'Market range: €2.69 - €2.97', 1.45, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-052') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.08, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €7.6 - €8.37', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-052';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-052', '������ �� ��. ����� ������������� �� ������� ��. ���������� � ������ �� �����', 'Market range: €7.6 - €8.37', 4.08, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-083') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.05, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €3.8 - €4.19', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-083';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-083', '��������� �� ��������� �� 2�(�� 10��2 �� 16��2) � ��������� ��������� �����', 'Market range: €3.8 - €4.19', 2.05, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-089') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 5.11, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €9.51 - €10.47', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-089';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-089', '��������� �� ��������� �� 4�(�� 25��2 �� 35��2) � ��������� ��������� �����', 'Market range: €9.51 - €10.47', 5.11, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-PANEL-MOD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 7.67, 
            "CalculationFormula" = '12 + Count(elec_heavy_appliances) + elec_ac_count', 
            "UnitType" = 'module', 
            "Description" = 'Подреждане на предпазители.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-PANEL-MOD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-PANEL-MOD', 'Сглобяване на табло (на модул)', 'Подреждане на предпазители.', 7.67, 'module', '12 + Count(elec_heavy_appliances) + elec_ac_count', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-033') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.21, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €7.85 - €8.64', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-033';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-033', '������ �� �������� �� ������� �����������������', 'Market range: €7.85 - €8.64', 4.21, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-060') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 25.95, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €48.34 - €53.19', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-060';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-060', '������ �� ������������ �����', 'Market range: €48.34 - €53.19', 25.95, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-081') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.93, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €5.46 - €6.01', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-081';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-081', '��������� �� ��������� �� 4�(�� 4��2 �� 6��2) � ��������� ��������� �����', 'Market range: €5.46 - €6.01', 2.93, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-082') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.32, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €2.45 - €2.7', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-082';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-082', '��������� �� ��������� �� 1�(�� 10��2 �� 16��2) � ��������� ��������� �����', 'Market range: €2.45 - €2.7', 1.32, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-071') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.62, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €3 - €3.31', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-071';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-071', '�������� �� ��� ��������� �� 3�4 �� ����� ��� �����', 'Market range: €3 - €3.31', 1.62, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'GEN-001') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 25.56, 
            "CalculationFormula" = '1', 
            "UnitType" = 'flat', 
            "Description" = 'Подготовка, защита и логистика на обекта', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'GEN-001';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'GEN-001', 'Подготовка, защита и логистика на обекта', 'Подготовка, защита и логистика на обекта', 25.56, 'flat', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Painting';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-021') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.58, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €4.6 - €5.5', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-021';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-021', 'Монтаж на електрическа кутия конзолна', 'Market range: €4.6 - €5.5', 2.58, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-090') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 18.38, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €34.23 - €37.66', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-090';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-090', '������ ( ��������� ) ����� ��� ������� �� 6� ����� (������) ���������', 'Market range: €34.23 - €37.66', 18.38, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-003') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 102.26, 
            "CalculationFormula" = '', 
            "UnitType" = '€/бр', 
            "Description" = 'Market range: €180 - €220', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-003';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-003', 'Монтаж на вградена структура за тоалетна', 'Market range: €180 - €220', 102.26, '€/бр', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'MICO-004') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 6.14, 
            "CalculationFormula" = '', 
            "UnitType" = 'sqm', 
            "Description" = 'Допълнителна хидроизолация.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'MICO-004';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'MICO-004', 'Хидроизолация', 'Допълнителна хидроизолация.', 6.14, 'sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-137') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 0.89, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €1.65 - €1.83', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-137';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-137', '��������� �� ��������� ����� � ��������� �� 1���� � ��������� �����', 'Market range: €1.65 - €1.83', 0.89, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-109') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 4.38, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €4.35 - €4.8', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-109';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-109', '���������� �� ����� �� ��� � ���� �� 3�6 � �����', 'Market range: €4.35 - €4.8', 4.38, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-047') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 0.82, 
            "CalculationFormula" = '', 
            "UnitType" = '�/177.10 ����', 
            "Description" = 'Market range: €1.53 - €1.69', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-047';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-047', '������ �� ������� ����������� ���� � �������� ��', 'Market range: €1.53 - €1.69', 0.82, '�/177.10 ����', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'TILE-001') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 16.11, 
            "CalculationFormula" = '', 
            "UnitType" = '€/кв.м', 
            "Description" = 'Market range: €28 - €35', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'TILE-001';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'TILE-001', 'Лепене на плочки (стандартни размери)', 'Market range: €28 - €35', 16.11, '€/кв.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-094') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 18.38, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €34.23 - €37.66', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-094';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-094', '������ (���������) ����� ����� �� 6� ��� ��������� ��� �����������', 'Market range: €34.23 - €37.66', 18.38, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'MICO-001') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 33.23, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per sqm', 
            "Description" = '', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'MICO-001';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'MICO-001', 'Микроцимент за под/стени (стандартен)', '', 33.23, 'Per sqm', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-POINT-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 17.90, 
            "CalculationFormula" = 'if(Contains(elec_outlets_comfort, ''Базово''), (global_room_count * 3) + 4, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, 0)))', 
            "UnitType" = 'pcs', 
            "Description" = 'Труд за 1 брой контакт/ключ.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-POINT-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-POINT-STD', 'Изграждане на излазна точка', 'Труд за 1 брой контакт/ключ.', 17.90, 'pcs', 'if(Contains(elec_outlets_comfort, ''Базово''), (global_room_count * 3) + 4, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, 0)))', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-PANEL-NICHE') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 48.57, 
            "CalculationFormula" = 'if(Contains(elec_panel, ''скрито''), 1, 0)', 
            "UnitType" = 'pcs', 
            "Description" = 'Къртене на голям отвор в стената за скрит монтаж на апартаментно електрическо табло.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-PANEL-NICHE';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-PANEL-NICHE', 'Изкопаване на ниша за вградено табло', 'Къртене на голям отвор в стената за скрит монтаж на апартаментно електрическо табло.', 48.57, 'pcs', 'if(Contains(elec_panel, ''скрито''), 1, 0)', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Electrical';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-065') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 1.45, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €2.69 - €2.97', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-065';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-065', '�������������� �� ��������� ��� ���������� �� 4��2', 'Market range: €2.69 - €2.97', 1.45, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-074') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 0.43, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €0.79 - €0.88', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-074';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-074', '��������� �� ��������� �� 1�(�� 0,5��2 �� 2,5��2) � ��������� ��������� �����', 'Market range: €0.79 - €0.88', 0.43, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-002') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 11.76, 
            "CalculationFormula" = '', 
            "UnitType" = '€/кв.м', 
            "Description" = 'Market range: €18 - €28', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-002';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-002', 'Изграждане на преградна стена (гипсокартон)', 'Market range: €18 - €28', 11.76, '€/кв.м', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'DRYW-CEILING-STD') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 23.01, 
            "CalculationFormula" = '1', 
            "UnitType" = 'sqm', 
            "Description" = 'Монтаж на окачен таван.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'DRYW-CEILING-STD';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'DRYW-CEILING-STD', 'Окачен таван (Едно ниво)', 'Монтаж на окачен таван.', 23.01, 'sqm', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-077') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 2.05, 
            "CalculationFormula" = '', 
            "UnitType" = '�/��', 
            "Description" = 'Market range: €3.8 - €4.19', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-077';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-077', '��������� �� ��������� �� 4�(�� 0,5��2 �� 2,5��2) � ��������� ��������� �����', 'Market range: €3.8 - €4.19', 2.05, '�/��', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'PLMB-WC-BUILTIN') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 97.15, 
            "CalculationFormula" = '1', 
            "UnitType" = 'pcs', 
            "Description" = 'Конзолна тоалетна.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'PLMB-WC-BUILTIN';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'PLMB-WC-BUILTIN', 'Монтаж на структура за вграждане', 'Конзолна тоалетна.', 97.15, 'pcs', '1', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "ServiceSkus" WHERE "SkuCode" = 'ELEC-003') THEN
        UPDATE "ServiceSkus" 
        SET "BasePrice" = 6.14, 
            "CalculationFormula" = '', 
            "UnitType" = 'Per Linear Meter', 
            "Description" = 'Изкопаване на канал за полагане на кабели в твърд стоманобетон. Най-трудният и бавен процес.', 
            "UpdatedAt" = now()
        WHERE "SkuCode" = 'ELEC-003';
    ELSE
        INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), "Id", 'ELEC-003', 'Къртене на канал в бетон/панел', 'Изкопаване на канал за полагане на кабели в твърд стоманобетон. Най-трудният и бавен процес.', 6.14, 'Per Linear Meter', '', now(), now()
        FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация ';
    END IF;
END $$;
