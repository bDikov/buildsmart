
-- FINAL FIX: INJECTING MISSING FORMULAS

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, if(Contains(elec_scope, ''Частичен''), global_total_sqm * 1.0, 0))' WHERE "SkuCode" = 'ELEC-CABLE-LAY';
UPDATE "ServiceSkus" SET "CalculationFormula" = '(Count(elec_heavy_appliances) + elec_ac_count) * 10' WHERE "SkuCode" = 'ELEC-CABLE-HEAVY';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_walls, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)' WHERE "SkuCode" = 'ELEC-CHASE-CONC';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_walls, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)' WHERE "SkuCode" = 'ELEC-CHASE-BRICK';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)' WHERE "SkuCode" = 'ELEC-LAY-TUBE';
UPDATE "ServiceSkus" SET "CalculationFormula" = '12 + Count(elec_heavy_appliances) + elec_ac_count' WHERE "SkuCode" = 'ELEC-PANEL-MOD';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_panel, ''скрито''), 1, 0)' WHERE "SkuCode" = 'ELEC-PANEL-NICHE';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_outlets_comfort, ''Базово''), (global_room_count * 3) + 4, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, 0)))' WHERE "SkuCode" = 'ELEC-POINT-STD';
UPDATE "ServiceSkus" SET "CalculationFormula" = '1' WHERE "SkuCode" = 'ELEC-POINT-LV';
UPDATE "ServiceSkus" SET "CalculationFormula" = '1' WHERE "SkuCode" = 'ELEC-POINT-DEV';
UPDATE "ServiceSkus" SET "CalculationFormula" = '1' WHERE "SkuCode" = 'ELEC-POINT-SPEC';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_lighting, ''LED''), 1, 0)' WHERE "SkuCode" = 'ELEC-LED-TRAFO';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'global_total_sqm * 2.5' WHERE "SkuCode" = 'PANT-PRIMER';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(paint_scope, ''Стандартен'') || Contains(paint_scope, ''Сваляне''), global_total_sqm * 2.5, 0)' WHERE "SkuCode" = 'PANT-SPACKLE-STD';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(paint_finish_level, ''Q5''), global_total_sqm * 2.5, 0)' WHERE "SkuCode" = 'PANT-SPACKLE-Q5';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'global_total_sqm * 2.5' WHERE "SkuCode" = 'PANT-PAINT-WHITE';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'global_total_sqm * 2.5' WHERE "SkuCode" = 'PANT-PAINT-COLOR';
UPDATE "ServiceSkus" SET "CalculationFormula" = '1' WHERE "SkuCode" = 'PANT-TAPE-CORNER';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'paint_trim_doors_count' WHERE "SkuCode" = 'PANT-TRIM';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(paint_scope, ''Сваляне''), global_total_sqm * 2.5, 0)' WHERE "SkuCode" = 'PANT-WALLPAPER-REMOVE';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_type, ''Окачен таван''), if(Contains(drywall_rooms, ''В 1 стая''), 20, if(Contains(drywall_rooms, ''В 2-3 стаи''), 50, global_total_sqm)), 0)' WHERE "SkuCode" = 'DRYW-CEILING-STD';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_type, ''Преградни стени''), if(Contains(drywall_rooms, ''В 1 стая''), 20, if(Contains(drywall_rooms, ''В 2-3 стаи''), 50, global_total_sqm)), 0)' WHERE "SkuCode" = 'DRYW-WALL-PARTITION';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_type, ''Предстенна обшивка''), if(Contains(drywall_rooms, ''В 1 стая''), 20, if(Contains(drywall_rooms, ''В 2-3 стаи''), 50, global_total_sqm)), 0)' WHERE "SkuCode" = 'DRYW-WALL-LINING';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'global_bathroom_count * 3' WHERE "SkuCode" = 'DRYW-BOX';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'global_total_sqm * 0.2' WHERE "SkuCode" = 'DRYW-INSULATION';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(tile_rooms, ''Баня''), global_bathroom_count * 25, if(Contains(tile_rooms, ''Кухня''), 10, global_total_sqm * 0.3))' WHERE "SkuCode" = 'TILE-STD';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(tile_rooms, ''Баня''), global_bathroom_count * 25, if(Contains(tile_rooms, ''Кухня''), 10, global_total_sqm * 0.3))' WHERE "SkuCode" = 'TILE-LARGE';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'global_total_sqm * 0.5' WHERE "SkuCode" = 'TILE-PREP-LEVEL';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'global_bathroom_count * 5' WHERE "SkuCode" = 'TILE-PREP-HYDRO';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'global_total_sqm * 0.7' WHERE "SkuCode" = 'TILE-LAMINATE';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'global_total_sqm * 0.5' WHERE "SkuCode" = 'MICRO-STD';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'global_bathroom_count * 25' WHERE "SkuCode" = 'MICRO-BATH';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(plumb_scope, ''Цялостна''), (global_bathroom_count * 5) + 3, if(Contains(plumb_scope, ''извеждане''), 3, 0))' WHERE "SkuCode" = 'PLMB-POINT-NEW';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(plumb_riser, ''Да''), global_bathroom_count, 0)' WHERE "SkuCode" = 'PLMB-RISER-REPLACE';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(plumb_sink_count, ''3+''), 3, 1)' WHERE "SkuCode" = 'PLMB-SINK-INSTALL';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(plumb_wc_type, ''Стандартна''), global_bathroom_count, 0)' WHERE "SkuCode" = 'PLMB-WC-STD';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(plumb_wc_type, ''Вградена''), global_bathroom_count, 0)' WHERE "SkuCode" = 'PLMB-WC-BUILTIN';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(plumb_shower_type, ''кабина''), global_bathroom_count, 0)' WHERE "SkuCode" = 'PLMB-SHOWER-CABIN';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(plumb_shower_type, ''Само''), global_bathroom_count, 0)' WHERE "SkuCode" = 'PLMB-SHOWER-FIXTURE';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(plumb_appliances, ''бойлер''), 1, 0)' WHERE "SkuCode" = 'PLMB-BOILER';
UPDATE "ServiceSkus" SET "CalculationFormula" = '1' WHERE "SkuCode" = 'PLMB-APPLIANCE';
UPDATE "ServiceSkus" SET "CalculationFormula" = '1' WHERE "SkuCode" = 'PLMB-METER-REPLACE';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(demo_what, ''Цяла баня''), global_bathroom_count, 0)' WHERE "SkuCode" = 'DEMO-BATH-FULL';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(demo_what, ''Вътрешни тухлени''), global_total_sqm * 0.2, 0)' WHERE "SkuCode" = 'DEMO-WALL-BRICK';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(demo_what, ''Бетонни''), global_total_sqm * 0.2, 0)' WHERE "SkuCode" = 'DEMO-WALL-CONC';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(demo_what, ''Стари подови''), global_total_sqm * 0.3, 0)' WHERE "SkuCode" = 'DEMO-FLOOR-TILE';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(demo_disposal, ''Да''), 1, 0)' WHERE "SkuCode" = 'DEMO-DISPOSAL';
