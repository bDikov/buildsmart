---
name: sku_pricing
description: Reference guidelines for verifying, managing, and updating SKU base prices (in BGN) and units using the embedded JSON seed files.
---

# Workspace Skill: SKU Pricing Reference

Use this skill whenever you need to check the base prices, units, or description of any SKU. The embedded JSON files in `BuildSmart.Infrastructure` are the single source of truth for all pricing data.

## Pricing Source of Truth Files

All base prices are defined in BGN (Bulgarian Levs) in the following files:
* Painting / Spackling: [Painting_SKUs_Seed.json](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Infrastructure/Painting_SKUs_Seed.json)
* Drywall: [Drywall_SKUs_Seed.json](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Infrastructure/Drywall_SKUs_Seed.json)
* Electrical: [Electrical_SKUs_Seed.json](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Infrastructure/Electrical_SKUs_Seed.json)
* Global Questions: [Global_SKUs_Seed.json](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Infrastructure/Global_SKUs_Seed.json)
* Microcement: [Microcement_SKUs_Seed.json](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Infrastructure/Microcement_SKUs_Seed.json)
* Plumbing: [Plumbing_SKUs_Seed.json](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Infrastructure/Plumbing_SKUs_Seed.json)
* Tiling / Flooring: [Tiling_SKUs_Seed.json](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Infrastructure/Tiling_SKUs_Seed.json)
* Demolition / Hauling: [Demolition_SKUs_Seed.json](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Infrastructure/Demolition_SKUs_Seed.json)

## Price Conversion Note

When the backend seeds or updates the database:
1. It reads these files.
2. It automatically converts the BGN prices to EUR using the fixed peg rate of `1.95583`:
   $$\text{Price in EUR} = \frac{\text{Price in BGN}}{1.95583}$$
3. It saves the resulting EUR price into the database.

## Updating Prices

To update the pricing of any SKU permanently:
1. Edit the base price (in BGN) inside the corresponding `*_SKUs_Seed.json` file in `BuildSmart.Infrastructure`.
2. Copy the file to the `BuildSmart.Api` project folder to keep them in sync.
3. Deploy the application; the startup database seeder will automatically update the live database prices.
