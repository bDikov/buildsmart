-- 1. WIPE EVERYTHING CLEAN
DELETE FROM "AiCalculationSkuItems";
DELETE FROM "AiCalculationTasks";
DELETE FROM "AiCalculations";
DELETE FROM "TaskSkuItems";
DELETE FROM "ServiceSkus";

-- 2. INJECT ALL SKUS AND FORMULAS (SOFIA 2026 LOW-END MARKET RATES)

DO $$
DECLARE
    cat_gen uuid;
    cat_elec uuid;
    cat_paint uuid;
    cat_dryw uuid;
    cat_tile uuid;
    cat_mico uuid;
    cat_plmb uuid;
    cat_demo uuid;
BEGIN
    SELECT "Id" INTO cat_gen FROM "ServiceCategories" WHERE "Name" = 'Global Questions';
    SELECT "Id" INTO cat_elec FROM "ServiceCategories" WHERE "Name" = 'Електрическа Инсталация';
    SELECT "Id" INTO cat_paint FROM "ServiceCategories" WHERE "Name" = 'Бояджийски и шпакловъчни услуги';
    SELECT "Id" INTO cat_dryw FROM "ServiceCategories" WHERE "Name" = 'Сухо строителство';
    SELECT "Id" INTO cat_tile FROM "ServiceCategories" WHERE "Name" = 'Подови и стенни настилки';
    SELECT "Id" INTO cat_mico FROM "ServiceCategories" WHERE "Name" = 'Микроцимент';
    SELECT "Id" INTO cat_plmb FROM "ServiceCategories" WHERE "Name" = 'ВиК Услуги';
    SELECT "Id" INTO cat_demo FROM "ServiceCategories" WHERE "Name" = 'Къртене и извозване';

    -- GLOBAL
    INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") VALUES
    (gen_random_uuid(), cat_gen, 'GEN-001', 'Site Prep & Protection', 'Preparation, protection, and logistics.', 40, 'Flat', '1', now(), now()),
    (gen_random_uuid(), cat_gen, 'GEN-002', 'Final Cleaning', 'Complete final cleaning after works.', 1.5, 'sqm', 'global_total_sqm', now(), now()),
    (gen_random_uuid(), cat_gen, 'GEN-003', 'Daily Cleaning', 'Daily site cleaning.', 25, 'Flat', '1', now(), now());

    -- ELECTRICAL (Based on Sofia Low-End Market Rates: €5.50/contact, €4.40/m chasing)
    INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") VALUES
    (gen_random_uuid(), cat_elec, 'ELEC-CABLE-LAY', 'Полагане на силов кабел', 'Издърпване и фиксиране на кабел.', 3.0, 'm', 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, if(Contains(elec_scope, ''Частичен''), global_total_sqm * 1.0, 0))', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-CABLE-HEAVY', 'Полагане на мощен кабел', 'Дебел кабел за проточни бойлери.', 4.5, 'm', '(Count(elec_heavy_appliances) + elec_ac_count) * 10', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-CHASE-CONC', 'Къртене на канал в бетон', 'Изкопаване на канал в бетон.', 8.2, 'm', 'if(Contains(elec_walls, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-CHASE-BRICK', 'Къртене на канал в тухла', 'Изкопаване на канал в тухла.', 4.4, 'm', 'if(Contains(elec_walls, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-LAY-TUBE', 'Полагане на гофре', 'Полагане на гофрирана тръба.', 3.5, 'm', 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-PANEL-MOD', 'Сглобяване на табло (на модул)', 'Подреждане на предпазители.', 12.0, 'module', '12 + Count(elec_heavy_appliances) + elec_ac_count', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-PANEL-NICHE', 'Изкопаване на ниша за вградено табло', 'Скрит монтаж.', 85, 'pcs', 'if(Contains(elec_panel, ''скрито''), 1, 0)', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-POINT-STD', 'Изграждане на излазна точка', 'Труд за 1 брой контакт/ключ.', 5.5, 'pcs', 'if(Contains(elec_outlets_comfort, ''Базово''), (global_room_count * 3) + 4, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, 0)))', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-POINT-LV', 'Слаботокова точка', 'LAN/TV/СОТ.', 6.0, 'pcs', '1', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-POINT-DEV', 'Девиаторна точка', 'Девиаторни ключове.', 12.0, 'pcs', '1', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-POINT-SPEC', 'Извод за щори/вентилатор', 'Вентилатори или щори.', 15.0, 'pcs', '1', now(), now()),
    (gen_random_uuid(), cat_elec, 'ELEC-LED-TRAFO', 'Монтаж на захранващ блок за LED', 'Трансформатор.', 25, 'pcs', 'if(Contains(elec_lighting, ''LED''), 1, 0)', now(), now());

    -- PAINTING (Based on Sofia Low-End Market Rates: €1/m primer, €6/m spackle, €5/m paint)
    INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") VALUES
    (gen_random_uuid(), cat_paint, 'PANT-PRIMER', 'Дълбокопроникващ грунд', 'Грундиране на стени и тавани.', 1, 'sqm', 'if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)', now(), now()),
    (gen_random_uuid(), cat_paint, 'PANT-SPACKLE-STD', 'Шпакловка (Стандартна 2 ръце)', 'Цялостна шпакловка.', 6, 'sqm', 'if(Contains(paint_tasks, ''Цялостна шпакловка'') || Contains(paint_tasks, ''Сваляне на тапети''), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5), 0)', now(), now()),
    (gen_random_uuid(), cat_paint, 'PANT-SPACKLE-Q5', 'Фина шпакловка (Перфектна Q5)', 'Шитрок за гладка повърхност.', 12, 'sqm', 'if(Contains(paint_finish_level, ''Q5'') || Contains(paint_finish_level, ''Перфектно''), if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5), 0)', now(), now()),
    (gen_random_uuid(), cat_paint, 'PANT-PAINT-WHITE', 'Боядисване в бяло (2 ръце)', 'Боядисване с бял латекс.', 5, 'sqm', 'if(paint_sqm > 0, paint_sqm, global_total_sqm * 2.5)', now(), now()),
    (gen_random_uuid(), cat_paint, 'PANT-PAINT-COLOR', 'Боядисване в цвят (2 ръце)', 'Боядисване с цветен латекс.', 7, 'sqm', 'if(Contains(paint_colors, ''цвят'') && !Contains(paint_colors, ''бяло''), if(paint_sqm > 0, paint_sqm * 0.5, global_total_sqm * 1.2), 0)', now(), now()),
    (gen_random_uuid(), cat_paint, 'PANT-TAPE-CORNER', 'Поставяне на ъглохранители', 'Алуминиеви или PVC ъгли.', 5, 'm', 'if(paint_sqm > 0, paint_sqm * 0.1, global_total_sqm * 0.25)', now(), now()),
    (gen_random_uuid(), cat_paint, 'PANT-TRIM', 'Боядисване на врати/первази', 'Декоративни елементи.', 35, 'pcs', 'if(Contains(paint_trim_doors_count, ''4+''), 4, if(Contains(paint_trim_doors_count, ''3''), 3, if(Contains(paint_trim_doors_count, ''2''), 2, if(Contains(paint_trim_doors_count, ''1''), 1, 0))))', now(), now()),
    (gen_random_uuid(), cat_paint, 'PANT-WALLPAPER-REMOVE', 'Сваляне на стари тапети', 'Сваляне на стари тапети.', 4, 'sqm', 'if(Contains(paint_tasks, ''Сваляне на тапети''), if(paint_sqm > 0, paint_sqm * 0.5, global_total_sqm), 0)', now(), now());

    -- DRYWALL (Based on Sofia Low-End Market Rates: €18/m ceiling/partition)
    INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") VALUES
    (gen_random_uuid(), cat_dryw, 'DRYW-CEILING-STD', 'Окачен таван (Едно ниво)', 'Монтаж на окачен таван.', 18, 'sqm', 'if(Contains(drywall_type, ''Окачен таван''), if(Contains(drywall_rooms, ''1 стая''), 15, if(Contains(drywall_rooms, ''2-3 стаи''), 40, global_total_sqm)), 0)', now(), now()),
    (gen_random_uuid(), cat_dryw, 'DRYW-WALL-PARTITION', 'Преградна стена (Двуслойна)', 'Изграждане на преградна стена.', 18, 'sqm', 'if(Contains(drywall_type, ''Преградни стени''), if(Contains(drywall_rooms, ''1 стая''), 12, if(Contains(drywall_rooms, ''2-3 стаи''), 25, global_total_sqm * 0.5)), 0)', now(), now()),
    (gen_random_uuid(), cat_dryw, 'DRYW-WALL-LINING', 'Предстенна обшивка', 'Монтаж на предстенна обшивка.', 15, 'sqm', 'if(Contains(drywall_type, ''Предстенна обшивка''), if(Contains(drywall_rooms, ''1 стая''), 15, if(Contains(drywall_rooms, ''2-3 стаи''), 35, global_total_sqm * 0.8)), 0)', now(), now()),
    (gen_random_uuid(), cat_dryw, 'DRYW-BOX', 'Изграждане на куфари (Кутии)', 'Обличане на тръби.', 35, 'm', 'if(Contains(drywall_type, ''Куфари''), global_bathroom_count * 3, 0)', now(), now()),
    (gen_random_uuid(), cat_dryw, 'DRYW-INSULATION', 'Монтаж на вата', 'Поставяне на вата.', 8, 'sqm', 'if(Contains(drywall_insulation, ''Да''), if(Contains(drywall_rooms, ''1 стая''), 15, if(Contains(drywall_rooms, ''2-3 стаи''), 40, global_total_sqm)), 0)', now(), now());

    -- TILING (Based on Sofia Low-End Market Rates: €28/m tiles, €5/m laminate)
    INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") VALUES
    (gen_random_uuid(), cat_tile, 'TILE-STD', 'Лепене на стандартни плочки', 'Полагане на фаянс/теракот.', 28, 'sqm', 'if(Contains(tile_type, ''Стандартни плочки''), if(Contains(tile_rooms, ''Баня''), global_bathroom_count * 20, 0) + if(Contains(tile_rooms, ''Кухня''), 10, 0) + if(Contains(tile_rooms, ''Коридор''), 8, 0), 0)', now(), now()),
    (gen_random_uuid(), cat_tile, 'TILE-LARGE', 'Лепене на голямоформатен гранитогрес', 'Плочи над 60х120 см.', 45, 'sqm', 'if(Contains(tile_type, ''Голямоформатен''), if(Contains(tile_rooms, ''Баня''), global_bathroom_count * 20, 0) + if(Contains(tile_rooms, ''Кухня''), 15, 0) + if(Contains(tile_rooms, ''Спални'') || Contains(tile_rooms, ''Хол''), global_total_sqm * 0.5, 0), 0)', now(), now()),
    (gen_random_uuid(), cat_tile, 'TILE-PREP-LEVEL', 'Саморазливна замазка', 'Изравняване на пода.', 9, 'sqm', 'if(Contains(tile_prep, ''Саморазливна замазка''), global_total_sqm * 0.8, 0)', now(), now()),
    (gen_random_uuid(), cat_tile, 'TILE-PREP-HYDRO', 'Полагане на хидроизолация', 'Запечатване на мокри помещения.', 15, 'sqm', 'if(Contains(tile_prep, ''Хидроизолация''), global_bathroom_count * 15, 0)', now(), now()),
    (gen_random_uuid(), cat_tile, 'TILE-LAMINATE', 'Монтаж на ламинат', 'Полагане на ламиниран паркет.', 5, 'sqm', 'if(Contains(tile_type, ''Ламиниран паркет''), global_total_sqm * 0.7, 0)', now(), now());

    -- MICROCEMENT
    INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") VALUES
    (gen_random_uuid(), cat_mico, 'MICRO-STD', 'Полагане на микроцимент (сухи зони)', 'Полагане на микроцимент.', 120, 'sqm', 'if(Contains(mico_area, ''Сухи зони''), if(Contains(mico_rooms, ''1-2 стаи''), 30, global_total_sqm * 0.8), 0)', now(), now()),
    (gen_random_uuid(), cat_mico, 'MICRO-BATH', 'Полагане на микроцимент в мокри зони', 'Микроцимент за баня.', 160, 'sqm', 'if(Contains(mico_area, ''Мокри зони''), global_bathroom_count * 20, 0)', now(), now());

    -- PLUMBING (Based on Sofia Low-End Market Rates: €280 total pipe replacement, €180 builtin structure)
    INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") VALUES
    (gen_random_uuid(), cat_plmb, 'PLMB-POINT-NEW', 'Изграждане на нова ВиК точка', 'Тръби за топла/студена вода.', 55, 'pcs', 'if(Contains(plumb_scope, ''Цялостна''), (global_bathroom_count * 5) + 3, if(Contains(plumb_scope, ''извеждане''), 3, 0))', now(), now()),
    (gen_random_uuid(), cat_plmb, 'PLMB-RISER-REPLACE', 'Смяна на вертикален щранг', 'Подмяна на тръби.', 280, 'pcs', 'if(Contains(plumb_riser, ''Да''), global_bathroom_count, 0)', now(), now()),
    (gen_random_uuid(), cat_plmb, 'PLMB-SINK-INSTALL', 'Монтаж на мивка', 'Монтаж на мивки.', 45, 'pcs', 'if(Contains(plumb_sink_count, ''3+''), 3, if(Contains(plumb_sink_count, ''2''), 2, if(Contains(plumb_sink_count, ''1''), 1, 0)))', now(), now()),
    (gen_random_uuid(), cat_plmb, 'PLMB-WC-STD', 'Монтаж на стандартна тоалетна', 'Монтаж на тоалетна.', 80, 'pcs', 'if(Contains(plumb_wc_type, ''Стандартна''), global_bathroom_count, 0)', now(), now()),
    (gen_random_uuid(), cat_plmb, 'PLMB-WC-BUILTIN', 'Монтаж на структура за вграждане', 'Конзолна тоалетна.', 180, 'pcs', 'if(Contains(plumb_wc_type, ''Вградена''), global_bathroom_count, 0)', now(), now()),
    (gen_random_uuid(), cat_plmb, 'PLMB-SHOWER-CABIN', 'Монтаж на душ кабина', 'Душ кабина.', 250, 'pcs', 'if(Contains(plumb_shower_type, ''кабина'') || Contains(plumb_shower_type, ''Вана''), global_bathroom_count, 0)', now(), now()),
    (gen_random_uuid(), cat_plmb, 'PLMB-SHOWER-FIXTURE', 'Монтаж на душ система', 'Душ батерия.', 40, 'pcs', 'if(Contains(plumb_shower_type, ''Само'') || Contains(plumb_shower_type, ''кабина'') || Contains(plumb_shower_type, ''Вана''), global_bathroom_count, 0)', now(), now()),
    (gen_random_uuid(), cat_plmb, 'PLMB-BOILER', 'Монтаж на ел. бойлер', 'Бойлер до 100л.', 70, 'pcs', 'if(Contains(plumb_appliances, ''бойлер''), 1, 0)', now(), now()),
    (gen_random_uuid(), cat_plmb, 'PLMB-APPLIANCE', 'Свързване на пералня/съдомиялна', 'Уреди.', 40, 'pcs', 'if(Contains(plumb_appliances, ''Пералня'') && Contains(plumb_appliances, ''Съдомиялна''), 2, if(Contains(plumb_appliances, ''Пералня'') || Contains(plumb_appliances, ''Съдомиялна''), 1, 0))', now(), now()),
    (gen_random_uuid(), cat_plmb, 'PLMB-METER-REPLACE', 'Смяна на водомер', 'Нов водомер.', 30, 'pcs', 'global_bathroom_count * 2', now(), now());

    -- DEMOLITION (Based on Sofia Low-End Market Rates: €10/m tiles, €60/m3 concrete, €40 course)
    INSERT INTO "ServiceSkus" ("Id", "ServiceCategoryId", "SkuCode", "Name", "Description", "BasePrice", "UnitType", "CalculationFormula", "CreatedAt", "UpdatedAt") VALUES
    (gen_random_uuid(), cat_demo, 'DEMO-BATH-FULL', 'Цялостно къртене на баня', 'Къртене на баня.', 450, 'pcs', 'if(Contains(demo_what, ''Цяла баня''), global_bathroom_count, 0)', now(), now()),
    (gen_random_uuid(), cat_demo, 'DEMO-WALL-BRICK', 'Къртене на тухлена стена', 'Събаряне на тухлени стени.', 12, 'sqm', 'if(Contains(demo_what, ''тухлени стени''), if(Contains(demo_rooms, ''1-2 стаи''), 15, 35), 0)', now(), now()),
    (gen_random_uuid(), cat_demo, 'DEMO-WALL-CONC', 'Къртене на бетонна стена', 'Къртене на бетон.', 35, 'sqm', 'if(Contains(demo_what, ''Бетонни''), if(Contains(demo_rooms, ''1-2 стаи''), 10, 20), 0)', now(), now()),
    (gen_random_uuid(), cat_demo, 'DEMO-FLOOR-TILE', 'Къртене на подови настилки', 'Премахване на настилки.', 10, 'sqm', 'if(Contains(demo_what, ''подови настилки''), if(Contains(demo_rooms, ''1-2 стаи''), 20, global_total_sqm * 0.8), 0)', now(), now()),
    (gen_random_uuid(), cat_demo, 'DEMO-DISPOSAL', 'Извозване с контейнер', 'Наемане на контейнер.', 40, 'pcs', 'if(Contains(demo_disposal, ''Да''), 1, 0)', now(), now());

END $$;
