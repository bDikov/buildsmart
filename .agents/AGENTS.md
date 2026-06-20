# BuildSmart Workspace Rules

## 1. Database Seeding & Synchronization
- **Automated Startup Seeding**: Never run manual raw SQL scripts on the live database to sync questions, categories, SKUs, or formulas. Always use the automated startup seeding pipeline.
- **Single Source of Truth**: Update the JSON template and SKU seed files in the `BuildSmart.Infrastructure` project. 
- **JSON File Sync**: Always synchronize updated SKU and Category JSON seed files between `BuildSmart.Infrastructure` and `BuildSmart.Api` to keep configurations aligned across folders.
- **Price Conversion**: The database seeder expects base prices in BGN and automatically converts them to EUR using `Math.Round(price / 1.95583m, 2)`. Define all seed prices in BGN.

## 2. Startup Concurrency & Advisory Locks
- Seeding tasks in `Program.cs` must run under a PostgreSQL transaction-level advisory lock (`pg_advisory_xact_lock(748291)`) to coordinate concurrent startups of the API and Hangfire worker servers safely.

## 3. Entity Framework Core Change Tracking
- **Explicit DbSet Addition**: When seeding new child translation entities that inherit client-side generated key IDs, do not use collection navigations (e.g. `category.Translations.Add(...)`). Instead, add them explicitly to their respective `DbSet` (e.g. `await ServiceCategoryTranslations.AddAsync(...)`) to force EF Core to issue `INSERT` statements rather than failing `UPDATE` queries.
- **Change Tracker Clear**: Always clear the EF Core change tracker (`context.ChangeTracker.Clear()`) between distinct database seeding stages to prevent entity cache contamination.
