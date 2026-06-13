-- RUN THIS SCRIPT ON THE LIVE (PRODUCTION) DATABASE --

UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(global_wall_material, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-CHASE-CONC';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(global_wall_material, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 3.5, 0)', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-CHASE-BRICK';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'elec_lv_count', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-POINT-LV';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'elec_dev_count', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-POINT-DEV';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'elec_spec_count', "UpdatedAt" = now() WHERE "SkuCode" = 'ELEC-POINT-SPEC';