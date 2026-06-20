---
name: add_question
description: Guide and instructions for adding a new question to an existing category, mapping it to SKUs, updating formulas, and synchronizing local and live databases using the automated startup seeding pipeline.
---

# Workspace Skill: Adding Questions, SKUs, and Formulas

Use this skill whenever the user requests to add a new question to a category, create conditional (subsequential) questions, or map user inputs to specific calculation formulas and SKUs.

## Step-by-Step Pipeline

Follow this 4-step pipeline without exception:

### 1. Requirements Gathering (Clarifying with the User)
Before modifying files, ask the user to clarify:
- **Parent Question**: Does this question depend on a previous answer? (e.g., "Only show if they chose 'Standard repair'").
- **Question Details**: Text in Bulgarian (or target language), type (e.g. `number`, `choice`, `multiselect`, `boolean`), options (if any), and whether it is required.
- **SKU Mapping**: What SKU (ServiceSku) does this answer affect? If it's a new SKU:
  - What is the SKU code (e.g., `PANT-MESH`), name, description, unit type (e.g. `sqm`, `m`, `pcs`), and base price?
- **Formula**: How should the SKU quantity be calculated using the new answer? (e.g., `if(tile_gerung == 'Да', bathroom_count * 6.0, 0)`).

### 2. Update the JSON templates for Categories
- Open `Categories_Seed_Templates.json` (in the root directory).
- Locate the target category and add the new question.
- **For conditional (subsequential) questions**, include:
  - `"dependsOn"`: The exact `"id"` of the parent question.
  - `"dependsOnValue"`: The string value or regex pattern (pipe-separated e.g., `"Value 1 | Value 2"`) that triggers this question.

> [!IMPORTANT]
> Once modified, copy the updated `Categories_Seed_Templates.json` to both:
> 1. `BuildSmart.Infrastructure/Categories_Seed_Templates.json`
> 2. `BuildSmart.Api/Categories_Seed_Templates.json`

### 3. Update the Category SKU JSON Seed Files
Category SKU JSON files inside [BuildSmart.Infrastructure](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Infrastructure) are the **single source of truth** for SKU metadata, base prices (in BGN), and calculation formulas.
- Locate the corresponding JSON file in the infrastructure project (e.g., `Painting_SKUs_Seed.json`, `Plumbing_SKUs_Seed.json`, etc.).
- Add or update the SKU block inside the `"skus"` array:
  ```json
  {
    "skuCode": "PANT-NEW-SKU",
    "name": "New SKU Name",
    "description": "SKU Description",
    "basePrice": 15.00,
    "unitType": "sqm",
    "calculationFormula": "if(paint_scope == 'Стандартен', global_total_sqm * 2.5, 0)"
  }
  ```
- Also, synchronize the updated file to the [BuildSmart.Api](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Api) project to keep templates aligned across folders.

### 4. Build and Push (Automated Synchronization)
No manual SQL execution or runner script is needed on the Live DB. 
- Run `dotnet build` locally to compile the solution and verify that the embedded resources load properly.
- Push the changes to the `main` branch. 
- The CI/CD deployment pipeline will build and run the updated API.
- Upon startup, [AppDbContext.cs](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.Infrastructure/Persistence/AppDbContext.cs) will read the embedded JSON seed files, automatically convert base prices from BGN to EUR (by dividing by `1.95583`), insert any new SKUs, and update the properties and calculation formulas for all existing SKUs.
