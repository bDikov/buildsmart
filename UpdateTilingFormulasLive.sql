-- RUN THIS SCRIPT ON THE LIVE (PRODUCTION) DATABASE --

UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'tile_std_sqm', "UpdatedAt" = now() 
WHERE "SkuCode" = 'TILE-STD';

UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'tile_large_sqm', "UpdatedAt" = now() 
WHERE "SkuCode" = 'TILE-LARGE';

UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'tile_laminate_sqm', "BasePrice" = 6.00, "UpdatedAt" = now() 
WHERE "SkuCode" = 'TILE-LAMINATE';
