# BuildSmart Workspace Rules

## 1. Database Seeding & Synchronization
- **Automated Startup Seeding**: Never run manual raw SQL scripts on the live database to sync questions, categories, SKUs, or formulas. Always use the automated startup seeding pipeline.
- **Single Source of Truth**: Update the JSON template and SKU seed files in the `BuildSmart.Infrastructure` project. 
- **JSON File Sync**: Always synchronize updated SKU and Category JSON seed files between `BuildSmart.Infrastructure` and `BuildSmart.Api` to keep configurations aligned across folders.
- **Price Conversion**: The database seeder expects base prices in BGN and automatically converts them to EUR using `Math.Round(price / 1.95583m, 2)`. Define all seed prices in BGN.
- **No Background DB Modifications or Seeding Runs**: The AI assistant must never automatically execute tests, scripts, or database queries to modify, seed, or migrate the user's database. The user will manually initiate all migrations, seeding commands, and database updates.

## 2. Startup Concurrency & Advisory Locks
- Seeding tasks in `Program.cs` must run under a PostgreSQL transaction-level advisory lock (`pg_advisory_xact_lock(748291)`) to coordinate concurrent startups of the API and Hangfire worker servers safely.

## 3. Entity Framework Core Change Tracking
- **Explicit DbSet Addition**: When seeding new child translation entities that inherit client-side generated key IDs, do not use collection navigations (e.g. `category.Translations.Add(...)`). Instead, add them explicitly to their respective `DbSet` (e.g. `await ServiceCategoryTranslations.AddAsync(...)`) to force EF Core to issue `INSERT` statements rather than failing `UPDATE` queries.
- **Change Tracker Clear**: Always clear the EF Core change tracker (`context.ChangeTracker.Clear()`) between distinct database seeding stages to prevent entity cache contamination.

## 4. Code Refinements & Warning Resolution
- **Proactive Warning Checks**: Once all tests pass and you are ready to prepare the walkthrough, use `git diff` to identify all changed files. Inspect these files and the build outputs to ensure no new compiler warnings, nullability issues, or performance warnings have been introduced, and resolve them before finalizing the walkthrough.

## Secrets and Configuration Management
- **Never Hardcode Secrets**: Do not hardcode API keys, credentials, secrets, or third-party service endpoints in the codebase.
- **Configuration-Driven**: Always load these configurations dynamically from configuration providers (e.g., `appsettings.json`, environment variables, User Secrets, or Key Vaults).

## 5. Testing & Database Isolation
- **InMemory database provider**: Always use the EF Core InMemory database provider (`UseInMemoryDatabase`) for unit/integration tests that require a database context. 
- **Database isolation**: Never attempt to connect to a real, local, or external PostgreSQL database inside tests (never use `UseNpgsql` or hardcoded connection strings).
- **Isolated database names**: For each test method/scenario, initialize the InMemory database with a unique database name (e.g. `$"PricingSimulationDb_{Guid.NewGuid()}"`) to ensure total isolation between concurrent test executions.
- **Separate seeding context**: Perform all test database setup and seeding in a separate, scoped `DbContext` instance before querying and executing assertions in a new `DbContext` instance to clear the EF Core tracking cache.

## 6. Spider-Net Feature & Pricing Engine
- **Unified Manager Route**: `/admin/spider-net` is the single unified admin page ([SpiderNetManager.razor](file:///C:/Users/bonch/source/repos/BuildSmart/BuildSmart.SharedUI/Components/Pages/Admin/SpiderNetManager.razor)) for managing all categories, questions, pricing formulas, and service SKUs.
- **Config Import/Export**: The entire questionnaire structure (Questions, Formulas, SKUs, Categories) is synced using a single exported JSON schema.
- **Importer Logic**: 
  - To prevent database duplications, if an imported SKU already exists in the database by `SkuCode`, the importer updates it using `UpdateServiceSkuAsync` instead of creating a duplicate or leaving its price/formula empty.
  - Suffix duplicate categories (e.g. `Category_1`) must be avoided. Legacy duplicate SKUs should be merged or deleted to ensure `SkuCode` is semantically unique.
- **Pricing Engine (`PricingEngine.cs`)**:
  - Homeowner answers from the UI wizard are stored as **strings** (e.g. `"6"`, `"True"`) in the `JobDetails` JSON.
  - The pricing engine dynamically parses string numbers into `decimal` and string booleans into `bool` before NCalc formula evaluation.
  - If a task evaluates to `€0.00` total, it is automatically dropped from the final offer to prevent empty tasks from showing on the PDF/UI. Every formula must evaluate to a positive decimal for its corresponding task to be included.
- **System Categories**: Under the Category Edit Form, checking **"Is Project Details (System Category)"** serializes `"isProjectDetails": true` at the root of the category's `TemplateStructure` JSON. The wizard checks `IsProjectDetailsCategory` to identify this category for project-wide details (such as location and budget) and filters it out of the selectable trade categories step.

## 7. Frontend, Theme Compliance & Localization
- **Style and Theme Compliance**: Always adhere strictly to the project's CSS design system. Never hardcode colors (e.g., `#FFFFFF`, `rgba(0,0,0,0.1)`) inside Razor components or stylesheets. Always use CSS variables (e.g., `var(--bg-card)`, `var(--text-primary)`, `var(--color-primary)`) to ensure native Dark/Light mode theme parity.
- **Typography Sizing**: Never hardcode font sizes, line heights, or font weights individually. Use the official typography tokens (`font: var(--font-h0)` to `font: var(--font-h6)`, `var(--font-body-1)`, `var(--font-body-2)`) through the CSS `font` shorthand property.
- **Strict Text Localization**: Hardcoded user-facing strings or placeholders in Razor components are prohibited. Always define localized values in the localization resource files (`AppResources.resx` and `AppResources.bg.resx`) and bind them dynamically using the `@Loc["ResourceKey"]` pattern.
- **Prohibition of Emojis**: Do not use emojis (such as 🛡️, 🚀, ⚙️) in any user-facing text, buttons, labels, or captions. They are prohibited by project policy to maintain a clean, premium, and professional aesthetic.

## 8. Figma MCP Server Configuration
- **Figma MCP Server Name**: The local Figma parser is registered under the server name `"fig"` (not `"figma"`, `"figma-local"`, or `"local_figma"`). It runs the `@bilalba/fig-mcp` package configured in `settings.json`.
- **Invoking Figma MCP Tools**: When querying local `.fig` files, call tools like `mcp_fig_get_tree_summary` or `mcp_fig_get_node_details` specifically on the `"fig"` server:
  - **ServerName**: `"fig"`
  - **ToolName**: `"mcp_fig_get_tree_summary"` (gets tree structure) or `"mcp_fig_get_node_details"` (gets node coordinates, spacing, and styles).
  - **Arguments**: `{"path": "C:\\absolute\\path\\to\\design.fig"}`

## 9. Docker Builds & Cache Clean Policy
- **No-Cache Builds**: To prevent Docker from serving stale code or cached layers on the live server, always build with the `--no-cache` flag (e.g. `docker-compose build --no-cache` or `docker build --no-cache`).
- **Prune Builder Cache**: Always run or instruct the user to run `docker builder prune -a -f` before launching new builds to ensure that unused build cache is completely cleared.
- **Clean Volumes**: When deploying or testing changes locally with Docker Compose, use `docker-compose down -v --remove-orphans` to clear stale volumes and cache, preventing old configuration or migration states from persisting.

## 10. CI/CD Pipeline & VPS Deployment Rules
- **Enforce Error Propagation (`set -e`)**: Any multi-line SSH deployment script (e.g., using `appleboy/ssh-action`) must start with `set -e`. This ensures the workflow aborts immediately if any command (like `docker compose build`) fails, preventing silent fallbacks to stale cached images.
- **Avoid Custom Package Mirrors**: Do not replace standard package manager mirrors (like replacing `deb.debian.org` with `mirrors.cloudflare.com` inside Dockerfiles) because they can fail to resolve under specific VPS network configurations, causing builds to fail.
- **Post-Deploy Asset Verifications**: When verifying deployments, always check that the live site (e.g., `https://buildsmart.bg`) is serving the new asset version suffixes (e.g., `v=1.4`) to confirm that the fresh build is live.

## 11. Marketing and Advertisement (PostHog Tracking)
- **Automatic UTM Tracking**: PostHog automatically captures UTM parameters (`utm_source`, `utm_medium`, `utm_campaign`, `utm_term`, `utm_content`) from the URL query string on initialization. Do not write custom parser logic to extract these parameters; they are native out-of-the-box.
- **Dynamic Event Instrumentation**: When implementing new advertising/landing pages or promo features, use `JSRuntime.InvokeVoidAsync("posthog.capture", "event_name", properties)` to log key conversion interactions (e.g. clicking a promo CTA).
- **Safety Wrappers (Try/Catch)**: All client-side analytics calls must be wrapped in `try/catch` blocks. This ensures that if PostHog fails to load, is blocked by adblockers, or is not configured (empty `ApiKey`), the application continues working without throwing JavaScript interop exceptions.
- **Campaign Custom Funnels**: Pre-defined funnel events (`wizard_started`, `wizard_step_completed`, `ai_estimate_generated`) can be queried directly in the PostHog Cloud dashboard to build ad-to-conversion analytics reports.
- **Landing Page Layouts**: If creating custom blank/ad landing pages that bypass the default `MainLayout.razor`, make sure to manually call `initializePostHog` during the page's `OnAfterRenderAsync` to ensure tracking script activation.

## 12. PDF Template and Page-Breaking Rules
- **Modern CSS Print Standards**: When configuring page breaks for PDF generation (printed via Puppeteer/Chromium), always use modern CSS print properties (`break-after: page`, `break-before: page`, `break-inside: avoid`) in addition to legacy aliases (`page-break-after`, `page-break-before`, `page-break-inside`).
- **Flexbox Page-Breaking Bug**: Chromium's print engine has known limitations applying page-breaks preceding or containing CSS flexbox layout containers (`display: flex;`). To prevent page break failures (such as the Terms & Conditions page header rendering on the bottom of the previous page), explicitly apply `page-break-before: always;` / `break-before: page;` to the succeeding flex container to force a clean page break.
- **Embedded Templates Rebuild Requirement**: The HTML template `OfferTemplate.html` is embedded as a resource in the `BuildSmart.Infrastructure` assembly. After making modifications to the template, the solution must be rebuilt (`dotnet build`) for the updated embedded resource to take effect in the compiled binaries.

## 13. Guest Account & Campaign Registration Analytics
- **Database User Count Queries**: When querying registration metrics, user counts, or campaign lead conversions from the `"Users"` table in PostgreSQL, auto-generated guest accounts (`@buildsmart.guest`) must strictly be excluded (e.g. `WHERE "Email" NOT LIKE '%@buildsmart.guest' AND "FirstName" != 'Guest'`).
- **Conversion Goal Instrumentation**: Ad campaign conversion goals (Google Ads, GTM, PostHog) must track explicit self-registration events (`registration_success`) triggered only upon completed Google OAuth signups or verified email registrations, and NEVER on guest session creation.

## 14. High-Conversion Blog Copywriting & Storytelling
- **Framework Compliance**: All marketing articles and blog posts must adhere to the `high-conversion-blog-copywriting` skill ([SKILL.md](file:///C:/Users/bonch/.gemini/antigravity/skills/high_conversion_blog_copywriting/SKILL.md)).
- **Story-Driven Hooks (PAS Framework)**: Start articles with a dramatic, real-world scenario (e.g. avoiding contractor traps in Sofia, unitemized quotes, 0% deposit protection) rather than passive generic introductions.
- **Bucket Brigade Intrigue Loops**: Maintain reader momentum with punchy bridge phrases (*"Here's where 90% of homeowners get trapped...", "The dirty little secret..."*).
- **Rhythm & Structure**: Keep paragraphs short (1-3 lines), use high-contrast formatting, itemized case study graphics, and seamless CTAs directing readers to the AI calculation wizard (`/job-wizard`) and video feed (`/feed`).


