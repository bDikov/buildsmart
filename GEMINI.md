# GEMINI.md

## Project Overview

This is a .NET solution for a "BuildSmart" application. It follows a Clean Architecture pattern, with a clear separation of concerns between the domain, application, and infrastructure layers.

The solution consists of the following projects:

*   **`BuildSmart.Core.Domain`**: Contains the core business logic and entities of the application. It has no external dependencies.
*   **`BuildSmart.Core.Application`**: Implements the application logic and use cases, orchestrating the domain layer.
*   **`BuildSmart.Infrastructure`**: Provides implementations for external concerns like data access, using Entity Framework Core with a PostgreSQL database.
*   **`BuildSmart.Api`**: A .NET 9 Web API that exposes the application's functionality through a GraphQL endpoint using HotChocolate. It uses JWT for authentication.
*   **`BuildSmart.Maui`**: A cross-platform client application built with .NET MAUI for iOS, MacCatalyst, and Windows. It communicates with the API using a GraphQL client (StrawberryShake).
*   **`BuildSmart.Api.Tests`**: Contains tests for the API, using xUnit, Moq for mocking, and Snapshooter for snapshot testing.

The project is set up to use Docker for a containerized database and test environment.

## Building and Running

### Database

The project uses a PostgreSQL database. A `docker-compose.yml` file is provided to easily spin up a PostgreSQL container.

To start the database:

```powershell
docker-compose up -d db
```

### API

To run the API locally, you can use the .NET CLI:

```powershell
dotnet run --project BuildSmart.Api
```

The API will be available at `http://localhost:5086` or `https://localhost:7212`. The GraphQL endpoint is at `/graphql`.

**Visual Studio Users:**
It is highly recommended to use the **`https`** (Project) profile rather than **IIS Express**. 
- This ensures a console terminal window opens to show real-time logs.
- This uses port **7212**, which is the default port configured in the MAUI application's `ApiConfig.cs`.

To switch: Select the dropdown arrow next to the Start button and choose **`https`**.

### MAUI App

To run the MAUI application, you can use the .NET CLI, specifying the target platform:

```powershell
# For Windows
dotnet build BuildSmart.Maui -t:Run -f net9.0-windows10.0.19041.0

# For iOS
dotnet build BuildSmart.Maui -t:Run -f net9.0-ios

# For MacCatalyst
dotnet build BuildSmart.Maui -t:Run -f net9.0-maccatalyst
```

### Running Tests

The project includes a PowerShell script to run the tests.

```powershell
./run-tests.ps1
```

This script uses `dotnet test` to execute the tests in the `BuildSmart.Api.Tests` project.

## Development Conventions

*   **Clean Architecture**: The project follows the principles of Clean Architecture, separating concerns into Domain, Application, and Infrastructure layers.
*   **GraphQL**: The API uses GraphQL for flexible data querying.
*   **JWT Authentication**: Authentication is handled using JSON Web Tokens.
*   **Entity Framework Core**: The infrastructure layer uses EF Core for data access.
*   **.NET MAUI with Blazor Hybrid**: The client application uses .NET MAUI as a native shell, but **all modern UI development MUST be done in Blazor (`.razor`) using the custom Figma design system**. 
    *   **CRITICAL:** Refer to `FRONTEND_GUIDELINES.md` for strict CSS variables, component architectures, and Figma-to-Blazor workflows. Do NOT write native XAML views unless strictly necessary for platform shells.
*   **Testing**: The project has a dedicated test project using xUnit, Moq, and Snapshot testing.
*   **AI Integration**: The platform uses Google Gemini 1.5 Pro for automated construction scope generation. See `AI_IMPLEMENTATION.md` for technical details.

### StrawberryShake GraphQL Generation
**STRICT RULE:** You must **NEVER** change the GraphQL API URL or ports in `StrawberryShake.json` or `.graphqlrc.json` without explicit user permission. You must **NEVER** attempt to run the `BuildSmart.Api` project via `dotnet run` in the background to update the GraphQL schema. 
When a schema update is required, **always ask the user** to start the API manually via Visual Studio or their terminal.

**Troubleshooting Schema Updates (`dotnet graphql update`):**
If the update fails with `error HTTP_ERROR: No connection could be made because the target machine actively refused it`, or if builds are failing due to file locks (`The file is locked by BuildSmart.Api`), there is likely a zombie `dotnet` process holding the port open.
1. **Kill all background processes:** Run `Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue` and `Stop-Process -Name BuildSmart.Api -Force -ErrorAction SilentlyContinue`.
2. **Start the API:** Navigate to `BuildSmart.Api` and run `dotnet run --launch-profile https`. (StrawberryShake requires the HTTPS profile, not HTTP).
3. **Update the Schema:** Once the API says "Now listening on: https://localhost:7212", open a new terminal in `BuildSmart.SharedUI` and run `dotnet graphql update`.
4. **Compile:** Run `dotnet build` to ensure the new schema resolves all `SS0002` errors.

### Manual Migrations

**IMPORTANT:** The Gemini agent is configured to **never** execute migration commands automatically.

### 3rd Party API Keys & Secrets Policy

**STRICT RULE:** Whenever a new 3rd-party API key, secret, or environment variable is introduced into the project, it MUST be synchronized across all environments immediately. This means adding it to:
1. Local `appsettings.json` / `appsettings.Development.json` (as a placeholder) and `dotnet user-secrets`.
2. The `docker-compose.yml` and `docker-compose.prod.yml` environment blocks.
3. The GitHub Actions CI/CD workflow (`main-pipeline.yml`) so it is injected into the live server's `.env` file during deployment.
4. The user must be instructed to add the actual secret values to their GitHub Repository Secrets.

### Git and Version Control

**STRICT RULE:** The Gemini agent must **NEVER EVER** execute `git add`, `git commit`, or `git push` commands unless the user explicitly gives a specific instruction to do so and then confirms it. The user prefers to manage version control manually through Visual Studio.

### Verification Protocol

**ALWAYS RUN BUILD:** When modifying code to fix errors or add features, the agent must **execute `dotnet build`** immediately after the changes to verify them. Do not rely solely on reading previous log files. Active verification is required.

**KILL PROCESSES AFTER BUILD/TEST:** Each time you build or run tests, ensure you kill any processes you have started (like the API or web application) after finishing to avoid file lock issues during subsequent builds.

## Domain Model Changes
*   **ServiceCategory**: Added `bool IsGlobal` property to support global questions that apply to all jobs regardless of category.
*   **JobPost**: Fixed `HomeownerProfileId` mapping in `JobPostService` to prevent FK violations.
*   **JobPostFeedback**: New entity added to support threaded clarifications between Admins and Homeowners.
*   **GraphQL Schema**: Aligned `JobPostStatus` and other Enums to use `UPPER_CASE` in `schema.graphql` to match HotChocolate's default server serialization (`UNDER_REVIEW`).

## New Features

### Architecture & Review Workflow (Latest)
*   **Project-Centric Admin Review**: The Admin dashboard now lists **entire Projects** instead of individual, isolated job tasks. This provides the "Full Picture" of a renovation.
*   **Dashboard Context**: Admins can see the project metadata (description, location, budget), homeowner's full name, and a status summary of all other sections/jobs within the same project.
*   **Scope Comparison**: Added a side-by-side comparison view between the **Original AI-Generated Scope** and the **User-Proposed Scope**.
*   **Q&A Visibility**: Enhanced extraction logic to display human-readable Question & Answer pairs by matching job data against both category-specific and **Global** templates.
*   **Threaded Clarification System**: Admins can post specific questions/comments on a job task and mark them as **Resolved** individually. Homeowners can respond directly.

### Previously Added Features
*   **Global Categories**: 
    *   Admins can create "Global" categories. Questions defined in these categories are automatically added to *every* job post wizard.
    *   Global categories are hidden from the standard category selection list but are processed in the background.
*   **Required Questions**:
    *   Admins can mark specific questions within a category as "Required".
    *   The Job Wizard prevents project submission if any required questions are left unanswered.
*   **Smart Scope Generation (AI Workflow)**:
    *   **Workflow**: Jobs follow a multi-stage approval flow: `Draft` -> `GeneratingScope` -> `WaitingForUserReview` -> `WaitingForAdminReview` -> `Open`.
    *   **Background Worker**: `ScopeGenerationWorker` processes jobs asynchronously using AI.
    *   **Homeowner UI**: Added a **"Generate Smart Scope"** button and a **Scope Review Page**.
*   **Project Navigation**:
    *   Removed auto-navigation to the latest project; users can now tap any project card to view specific details.

## AI Integration Reference
### System Prompt for Scope Generation
**Role**: Expert Construction Manager / Quantity Surveyor.
**Goal**: Transform raw Q&A into a Markdown Scope of Work including:
1. Project Overview
2. Detailed Tasks (inferring technical sub-tasks)
3. Materials (Contractor vs Owner supplied)
4. Site Logistics
5. Exclusions
**Tone**: Technical, Professional, Objective.

### UI Optimization: Preventing Duplicate AI Calls
**CRITICAL RULE:** In `JobWizardViewModel.cs`, the submission to the AI engine (`SubmitJobForScopeGeneration`) **MUST ALWAYS** be guarded by a hash comparison of the user's answers. 
- You must use `_lastSubmittedJobHashes` to store the serialized `_masterAnswerKey` when a job is submitted.
- Before calling `ExecuteAsync(jobId)`, you must verify that `!_lastSubmittedJobHashes.TryGetValue(jobId, out var lastHash) || lastHash != answersHash`.
- **NEVER** remove or bypass this caching logic during refactoring. It is critical for saving API costs and preventing redundant loading states when a user navigates "Back" and "Next" without modifying data.

### Project Proposals & Offer Documents
**Format**: Generated dynamically as PDFs matching the "Project Proposal Template (Community)" Figma design.
**Multilingual & T&C Support**: All generated offer PDFs, including their Terms and Conditions, formatting, and AI-generated pricing breakdowns, must be fully multilingual and dynamic based on the project's selected language code. Hardcoded T&Cs should be extracted and driven by the backend localization engine or passed down appropriately.

## Manual Migration Commands (Pending)

To support the new feedback system and domain changes, please run:

#### .NET CLI
```powershell
dotnet ef migrations add AddJobPostFeedback --project BuildSmart.Infrastructure --startup-project BuildSmart.Api
dotnet ef database update --project BuildSmart.Infrastructure --startup-project BuildSmart.Api
```

## Pipeline: Adding Conditional Questions & Exact Formulas

To ensure generated offers are 100% accurate, the AI AI calculation engine must use **exact user inputs** (e.g., specific square meters) rather than guessing via room-count heuristics. 

When adding subsequential (conditional) questions to a category, you MUST follow this exact 4-step pipeline to ensure both Local and Live environments are synchronized:

### 1. Update the JSON Templates
- Open `Categories_Seed_Templates.json`.
- Add the new question(s). To make a question conditional, you **must** provide:
  - `"dependsOn"`: The exact `"id"` of the parent question.
  - `"dependsOnValue"`: The string value that triggers the question. For multiple triggers, separate them with a pipe (e.g., `"Value 1 | Value 2"`). 
  - *Note: The system fully supports triggering off of `multiselect` parent arrays.*

### 2. Map the Exact Formula (C#)
- Open `UpdateQuestionsRunner/Program.cs`.
- Locate the relevant category and find the `SkuDef` entries.
- Replace any old heuristic logic (e.g., `global_total_sqm * 0.8`) with the exact `"id"` of your newly added JSON question (e.g., `"tile_std_sqm"`).

### 3. Update the Local Native Database (Bypass Windows Defender)
Because Windows Application Control policy blocks `UpdateQuestionsRunner.exe` on this machine, you cannot simply use `dotnet run`.
- Instead, use a `.csx` script (like `UpdateLocalTemplates.csx`) via `dotnet script` to parse the JSON and execute the `UPSERT` commands directly against the native `buildsmart_db` (Port 5432, user: postgres).
- This ensures the UI reflects the changes instantly for local testing.

### 4. Generate & Sync the Live SQL
- Run `node GenerateSql.js` from the project root. This automatically parses `Categories_Seed_Templates.json` and builds secure `ON CONFLICT DO UPDATE` blocks for the categories inside `SeedLiveCategories.sql`.
- **CRITICAL CI/CD OVERWRITE WARNING:** The GitHub Actions CI/CD pipeline (`main-pipeline.yml`) executes `SeedLiveCategories.sql` against the live production database on **every single deployment**. 
- Because `GenerateSql.js` *only* updates the Category Templates, you **MUST manually update** the corresponding `UPDATE "ServiceSkus"` SQL commands inside `SeedLiveCategories.sql` to change formulas or prices. If you perform a manual SQL update directly on the live database but fail to commit it to `SeedLiveCategories.sql`, your changes will be instantly overwritten the next time you deploy!
