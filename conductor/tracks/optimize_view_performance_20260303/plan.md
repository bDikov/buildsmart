# Implementation Plan: Optimize View Performance (ProjectDetailPage & AuctionHubPage)

## Phase 1: Backend Optimization (GraphQL & EF Core)
- [x] Task: Analyze existing queries (`GetMyProjects`, `GetAvailableAuctions`, `jobPostQuestionById`) to identify and remove excessive `.Include()` chains in EF Core.
- [x] Task: Implement HotChocolate `DataLoaders` for related collections (Questions, Feedbacks) to eliminate N+1 queries during data fetching.
- [x] Task: Optimize database indexes if necessary to support the faster targeted queries.
- [x] Task: Conductor - User Manual Verification 'Phase 1: Backend Optimization (GraphQL & EF Core)' (Protocol in workflow.md)

## Phase 2: Frontend Optimization (MAUI UI Thread & Bindings)
- [ ] Task: Refactor `ProjectDetailPage.xaml` to improve UI Virtualization (e.g., using `CollectionView` effectively and minimizing nested `BindableLayout` depth).
- [ ] Task: Refactor `AuctionHubPage.xaml` to improve UI Virtualization.
- [ ] Task: Review `ProjectDetailViewModel` and `AuctionHubViewModel` to ensure data parsing and mapping (e.g., converting GraphQL results to ViewModels) occurs on background threads via `Task.Run()`.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: Frontend Optimization (MAUI UI Thread & Bindings)' (Protocol in workflow.md)

## Phase 3: Final Verification & Clean Up
- [ ] Task: Test both views under varying network conditions to verify instant initial rendering.
- [ ] Task: Ensure all tests pass.
- [ ] Task: Conductor - User Manual Verification 'Phase 3: Final Verification & Clean Up' (Protocol in workflow.md)
