-- RUN THIS SCRIPT ON THE LIVE (PRODUCTION) DATABASE --

-- Cable chase and channel formulas
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(global_wall_material, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-CHASE-CONC';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(global_wall_material, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-CHASE-BRICK';

-- Standard point mappings
UPDATE "ServiceSkus" SET "CalculationFormula" = 'elec_lv_count', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-POINT-LV';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'elec_dev_count', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-POINT-DEV';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'elec_spec_count', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-POINT-SPEC';

-- Console box excavation and mounting formulas (fixed the unrealistically low 1 pcs fallback)
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(global_wall_material, ''Тухла'') && !Contains(elec_scope, ''Само монтаж''), (if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, (global_room_count * 3) + 4)) + elec_lv_count + elec_dev_count + elec_spec_count), 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-084';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(global_wall_material, ''Бетон'') && !Contains(elec_scope, ''Само монтаж''), (if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, (global_room_count * 3) + 4)) + elec_lv_count + elec_dev_count + elec_spec_count), 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-085';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(!Contains(elec_scope, ''Само монтаж''), (if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, (global_room_count * 3) + 4)) + elec_lv_count + elec_dev_count + elec_spec_count), 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-020';

-- Visible element mounting formulas (fixed socket, switch, and lighting counts)
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, (global_room_count * 3) + 4))', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-002';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'elec_dev_count + global_room_count + global_bathroom_count', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-001';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_lighting, ''Стандартно''), global_room_count + global_bathroom_count, 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-009';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_lighting, ''лунички''), (global_room_count + global_bathroom_count) * 6, 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-010';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(!Contains(elec_scope, ''Само монтаж''), global_room_count + global_bathroom_count, 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-007';

UPDATE "ServiceSkus" SET "CalculationFormula" = '(12 + Count(elec_heavy_appliances) + elec_ac_count) * 2', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-037';

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(elec_lighting, ''LED''), global_room_count, 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-LED-TRAFO';