# Specification: Optimize View Performance (ProjectDetailPage & AuctionHubPage)

## 1. Overview
The `ProjectDetailPage` and `AuctionHubPage` in the .NET MAUI application currently experience significant delays (up to 5 seconds) when loading. The goal of this track is to analyze and apply both Backend (GraphQL/Entity Framework) and Frontend (MAUI UI) optimizations to achieve near-instantaneous content rendering.

## 2. Functional Requirements
- **Frontend Optimization (.NET MAUI):**
  - Implement strict UI virtualization for all lists and nested collections to prevent the UI thread from hanging.
  - Optimize `BindableLayout` usage; avoid deep nesting of complex DataTemplates.
  - Offload long-running initialization logic (like data parsing/mapping) from the main thread.
  - Optimize image loading and caching mechanisms.
- **Backend Optimization (GraphQL/EF Core):**
  - Implement `DataLoaders` across all GraphQL queries fetching related entities (e.g., questions, replies, feedbacks) to eliminate N+1 SQL query problems.
  - Refine Entity Framework `.Include()` chains to prevent fetching unnecessary large object graphs.
  - Ensure fast execution of initial queries feeding these views.

## 3. Non-Functional Requirements
- **Performance:** Pages must render their initial interactive state instantly, rather than waiting 5 seconds.
- **Maintainability:** Backend optimizations should adhere to Clean Architecture principles. Frontend structural changes must not break existing data bindings.

## 4. Acceptance Criteria
- [ ] Navigating to `ProjectDetailPage` or `AuctionHubPage` renders the UI layout immediately without freezing the application.
- [ ] No N+1 database queries are generated when fetching data for these pages.
- [ ] The full data payload loads noticeably faster than the previous 5-second baseline.

## 5. Out of Scope
- Redesigning the visual layout of the pages.
- Altering the business logic regarding what data is displayed.