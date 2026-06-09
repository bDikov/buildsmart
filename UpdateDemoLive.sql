-- RUN THIS SCRIPT ON THE LIVE (PRODUCTION) DATABASE --

UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'demo_brick_sqm', "BasePrice" = 20.00, "UpdatedAt" = now() 
WHERE "SkuCode" = 'DEMO-WALL-BRICK';

UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'demo_conc_sqm', "BasePrice" = 50.00, "UpdatedAt" = now() 
WHERE "SkuCode" = 'DEMO-WALL-CONC';

UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'demo_floor_sqm', "BasePrice" = 15.00, "UpdatedAt" = now() 
WHERE "SkuCode" = 'DEMO-FLOOR-TILE';

UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'if(Contains(demo_disposal, ''Да''), Ceiling((if(Contains(demo_what, ''Цяла баня''), global_bathroom_count * 20, 0) + demo_brick_sqm + demo_conc_sqm) / 15 + (demo_floor_sqm / 35)), 0)', "BasePrice" = 250.00, "UpdatedAt" = now() 
WHERE "SkuCode" = 'DEMO-DISPOSAL';
