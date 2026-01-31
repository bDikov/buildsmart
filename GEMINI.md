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
*   **.NET MAUI with MVVM**: The client application is built with .NET MAUI and likely follows the MVVM pattern.
*   **Testing**: The project has a dedicated test project using xUnit, Moq, and Snapshot testing.

### Manual Migrations

**IMPORTANT:** The Gemini agent is configured to **never** execute migration commands automatically.

#### Package Manager Console (Visual Studio)
If using the Package Manager Console, use these commands (Set `BuildSmart.Infrastructure` as Default Project):
```powershell
Add-Migration <MigrationName> -Project BuildSmart.Infrastructure -StartupProject BuildSmart.Api
Update-Database -Project BuildSmart.Infrastructure -StartupProject BuildSmart.Api
```

#### .NET CLI (Terminal)
If using the terminal, use these commands:
```powershell
dotnet ef migrations add <MigrationName> --project BuildSmart.Infrastructure --startup-project BuildSmart.Api
dotnet ef database update --project BuildSmart.Infrastructure --startup-project BuildSmart.Api
```

*   **Agent Responsibility**: The agent modifies Entity classes and provides these commands.
*   **User Responsibility**: The user must execute these commands to update the database schema.

## Domain Model Changes
*   **ServiceCategory**: Added `bool IsGlobal` property to support global questions that apply to all jobs regardless of category.