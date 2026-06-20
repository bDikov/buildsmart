---
name: add_question
description: Guide and instructions for adding a new question to an existing category, mapping it to SKUs, updating formulas, and synchronizing local and live databases.
---

# Workspace Skill: Adding Questions, SKUs, and Formulas

Use this skill whenever the user requests to add a new question to a category, create conditional (subsequential) questions, or map user inputs to specific calculation formulas and SKUs.

## Step-by-Step Pipeline

Follow this 5-step pipeline without exception:

### 1. Requirements Gathering (Clarifying with the User)
Before modifying files, ask the user to clarify:
- **Parent Question**: Does this question depend on a previous answer? (e.g., "Only show if they chose 'Standard repair'").
- **Question Details**: Text in Bulgarian (or target language), type (e.g. `number`, `choice`, `multiselect`, `boolean`), options (if any), and whether it is required.
- **SKU Mapping**: What SKU (ServiceSku) does this answer affect? If it's a new SKU:
  - What is the SKU code (e.g., `PANT-MESH`), name, description, unit type (e.g. `sqm`, `m`, `pcs`), and base price?
- **Formula**: How should the SKU quantity be calculated using the new answer? (e.g., `if(tile_gerung == 'Да', bathroom_count * 6.0, 0)`).

### 2. Update the JSON Templates
- Open `Categories_Seed_Templates.json` (in the root directory).
- Locate the target category and add the new question.
- **For conditional (subsequential) questions**, include:
  - `"dependsOn"`: The exact `"id"` of the parent question.
  - `"dependsOnValue"`: The string value or regex pattern (pipe-separated e.g., `"Value 1 | Value 2"`) that triggers this question.

> [!IMPORTANT]
> Once modified, copy the updated `Categories_Seed_Templates.json` to both:
> 1. `BuildSmart.Infrastructure/Categories_Seed_Templates.json`
> 2. `BuildSmart.Api/Categories_Seed_Templates.json`

### 3. Update the SKU definitions & Formulas in the Sync Scripts
- Open [SyncDbSchemaAndFormulas.csx](file:///C:/Users/bonch/source/repos/BuildSmart/SyncDbSchemaAndFormulas.csx) and [GenerateLiveSql.csx](file:///C:/Users/bonch/.gemini/antigravity/brain/3aff7c08-ebab-4fde-9973-991d0f0dee4c/scratch/GenerateLiveSql.csx) (or wherever the generator script is located).
- Locate the relevant category section (e.g., `// Electrical`, `// Tiling`) in both scripts.
- Add or update the `SkuDef` or `WriteSkuBlock` entry with the SKU code, name, description, base price, unit, and the exact algebraic formula referencing the new question ID.

### 4. Update the Local Database
- Run the local database updater script in the project root to apply the new template structures, SKUs, and formulas locally:
  ```powershell
  dotnet script SyncDbSchemaAndFormulas.csx
  ```

### 5. Generate and Sync the Live SQL
- Run the SQL generator script to rebuild the database-agnostic update file:
  ```powershell
  dotnet script scratch/GenerateLiveSql.csx
  ```
  *(Note: This creates `SyncLiveDb.sql` in the project root).*
- Provide the generated `SyncLiveDb.sql` file to the user so they can execute it directly on the Live PostgreSQL database using pgAdmin or DBeaver.
- Commit the updated C# and JSON files.
