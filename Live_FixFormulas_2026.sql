-- =========================================================================
-- LIVE DATABASE PATCH: FIXING OVERBLOWN ELECTRICAL & DRYWALL MULTIPLIERS
-- =========================================================================
-- This script safely updates only the CalculationFormula for specific SKUs.
-- It will NOT delete or reset any existing projects, users, or data.

BEGIN;

-- 1. Fix Concrete Chasing (reduce from 3.5x to 0.6x)
UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'if(Contains(elec_walls, ''Бетон'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 0.6, 0)',
    "UpdatedAt" = now()
WHERE "SkuCode" = 'ELEC-CHASE-CONC';

-- 2. Fix Brick Chasing (reduce from 3.5x to 0.6x)
UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'if(Contains(elec_walls, ''Тухла'') && Contains(elec_scope, ''Цялостна''), global_total_sqm * 0.6, 0)',
    "UpdatedAt" = now()
WHERE "SkuCode" = 'ELEC-CHASE-BRICK';

-- 3. Fix Conduit/Tube Laying (reduce from 3.5x to 2.0x)
UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 2.0, 0)',
    "UpdatedAt" = now()
WHERE "SkuCode" = 'ELEC-LAY-TUBE';

-- 4. Fix Cable Laying (reduce from 3.5x to 2.5x)
UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'if(Contains(elec_scope, ''Цялостна''), global_total_sqm * 2.5, if(Contains(elec_scope, ''Частичен''), global_total_sqm * 1.0, 0))',
    "UpdatedAt" = now()
WHERE "SkuCode" = 'ELEC-CABLE-LAY';

-- 5. Fix Drywall Partition Walls (reduce from 0.5x to 0.2x for the "whole apartment" fallback)
UPDATE "ServiceSkus" 
SET "CalculationFormula" = 'if(Contains(drywall_type, ''Преградни стени''), if(Contains(drywall_rooms, ''1 стая''), 12, if(Contains(drywall_rooms, ''2-3 стаи''), 25, global_total_sqm * 0.2)), 0)',
    "UpdatedAt" = now()
WHERE "SkuCode" = 'DRYW-WALL-PARTITION';

COMMIT;
