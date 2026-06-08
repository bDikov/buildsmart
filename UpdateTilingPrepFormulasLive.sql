-- RUN THIS SCRIPT ON THE LIVE (PRODUCTION) DATABASE --

UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'tile_prep_level_sqm', "UpdatedAt" = now() 
WHERE "SkuCode" = 'TILE-PREP-LEVEL';

UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'tile_prep_hydro_sqm', "UpdatedAt" = now() 
WHERE "SkuCode" = 'TILE-PREP-HYDRO';