# Technology Stack: BuildSmart

## Core Technologies
- **Programming Language**: **C# (.NET 9)** - Providing a robust, cross-platform environment for both the API and client application.
- **Backend Framework**: **ASP.NET Core Web API** with **HotChocolate (GraphQL)** - Enabling a flexible and efficient data querying layer for the client.
- **Frontend Framework**: **.NET MAUI** with **StrawberryShake (GraphQL Client)** - Building a cross-platform (iOS, Windows, MacCatalyst) client with a modern, reactive data handling system.

## Performance & Scalability
- **Lazy Loading**: **GraphQL Pagination (Offset-based)** - Implementing on-demand fetching for deeply nested or large datasets to minimize initial payload size.
- **UI Virtualization**: **CollectionView (MAUI)** - Utilizing virtualized scrolling and incremental rendering to maintain UI responsiveness in data-heavy views.

## Data & Communication
- **Persistence**: **Entity Framework Core** with **PostgreSQL** - Using a powerful, object-relational mapper and a reliable relational database for construction data.
- **Real-time Communication**: **SignalR** - Implementing a notification hub for real-time updates and status changes between homeowners and admins.

## Architecture & Testing
- **Architecture**: **Clean Architecture** - Maintaining a clear separation of concerns between Domain, Application, Infrastructure, and API layers to ensure long-term maintainability.
- **Testing**: **xUnit**, **Moq**, and **Snapshooter** - Ensuring high quality and regression testing for the API and domain logic, including snapshot testing for GraphQL responses.

## AI Integration
- **AI Service**: **Google Gemini 1.5 Pro** - Powering the automated construction scope generation worker.
