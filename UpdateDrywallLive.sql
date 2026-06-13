-- RUN THIS SCRIPT ON THE LIVE (PRODUCTION) DATABASE --

-- Disable the old combined SKU so it doesn't double charge
UPDATE "ServiceSkus" SET "CalculationFormula" = '0', "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-INSULATION';

-- Update the new split SKUs
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''тавани''), dryw_ceiling_sqm, 0)', "BasePrice" = 10.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-INSUL-CEILING';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''стените''), dryw_partition_sqm + dryw_lining_sqm, 0)', "BasePrice" = 10.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-INSUL-WALL';
UPDATE "ServiceSkus" SET "CalculationFormula" = 'if(Contains(drywall_insulation, ''Да'') && Contains(dryw_insulation_areas, ''куфарите''), dryw_box_m, 0)', "BasePrice" = 10.00, "UpdatedAt" = now() WHERE "SkuCode" = 'DRYW-INSUL-BOX';