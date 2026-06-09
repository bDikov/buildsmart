-- RUN THIS SCRIPT ON THE LIVE (PRODUCTION) DATABASE --

UPDATE "ServiceSkus" SET "CalculationFormula" = 'dryw_ceiling_sqm', "BasePrice" = 45.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-CEILING-STD';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'dryw_partition_sqm', "BasePrice" = 65.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-WALL-PARTITION';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'dryw_lining_sqm', "BasePrice" = 40.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-WALL-LINING';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'dryw_box_m', "BasePrice" = 40.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-BOX';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да''), (if(Contains(dryw_insulation_areas, ''тавани''), dryw_ceiling_sqm, 0) + if(Contains(dryw_insulation_areas, ''стените''), dryw_partition_sqm + dryw_lining_sqm, 0) + if(Contains(dryw_insulation_areas, ''куфарите''), dryw_box_m, 0)), 0)', "BasePrice" = 10.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-INSULATION';