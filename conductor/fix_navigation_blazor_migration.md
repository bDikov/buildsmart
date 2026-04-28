# Implementation Plan - Fix Project Creation and Navigation

The "Create New Project" button and general navigation are failing because of inconsistent route naming and a disconnect between the native MAUI Shell and the Blazor Router after the migration.

## Objective
Standardize all routes to kebab-case and ensure the `INavigationBridge` implementations for both Web and Mobile can correctly route to Blazor components.

## Key Files & Context
- **Shared UI (Routes):** `BuildSmart.SharedUI/Components/Pages/*.razor`
- **Shared UI (Logic):** `BuildSmart.SharedUI/ViewModels/*.cs`
- **Web Bridge:** `BuildSmart.Web/Services/WebServices.cs`
- **MAUI Bridge:** `BuildSmart.Maui/Services/NavigationBridge.cs`
- **MAUI Entry Point:** `BuildSmart.Maui/Main.razor`

## Implementation Steps

### 1. Standardize Razor Page Routes
Update the `@page` directive in the following files to use kebab-case:
- `TradesmanDetails.razor`: `@page "/tradesman-details"`
- `ProjectDetail.razor`: `@page "/project-detail"`
- `PassedAuctions.razor`: `@page "/passed-auctions"`
- `Notifications.razor`: `@page "/notifications"`
- `ActiveJobs.razor`: `@page "/active-jobs"`

### 2. Create a Blazor Navigation Registry
Add a service to `BuildSmart.SharedUI` to bridge the gap between static services and the scoped `NavigationManager`.
- Create `BuildSmart.SharedUI/Services/IBlazorNavigationRegistry.cs` and its implementation. This registry will hold a reference to the active `NavigationManager`.
- Register this service as **Scoped** in `BuildSmart.Web` (to support multiple users/circuits).
- Register this service as **Singleton** in `BuildSmart.Maui` (single user).

### 3. Update ViewModels
Update all `NavigateToAsync` calls to use the new kebab-case routes.
- Fix `job-wizzard` typo in `FeedPageViewModel.cs`.
- Replace `JobWizardPage`, `ProjectDetailPage`, etc., with `/job-wizard`, `/project-detail`.
- Ensure leading slashes are consistent (e.g., always use `/job-wizard`).

### 4. Update Web Navigation Bridge
Refine `WebNavigationBridge.NavigateToAsync` in `BuildSmart.Web/Services/WebServices.cs`:
- It already has access to `NavigationManager` via DI.
- Add logic to handle cases where it receives the old "Page" suffix routes by mapping them to the new kebab-case routes for backward compatibility if needed, though primary goal is to fix the calls.

### 5. Update MAUI Navigation Bridge & Hook
- **Bridge Update:** Modify `BuildSmart.Maui/Services/NavigationBridge.cs` to inject `IBlazorNavigationRegistry`.
- In `NavigateToAsync`, if the route starts with `/` and `registry.CurrentManager` is not null, use `registry.CurrentManager.NavigateTo(url)`.
- Otherwise, fallback to `Shell.Current.GoToAsync(route)`.
- **Hook Registry:** Update `BuildSmart.Maui/Main.razor` to inject `IBlazorNavigationRegistry` and `NavigationManager`, then set `Registry.CurrentManager = NavManager` in `OnInitialized`.

## Verification & Testing
- **Web:** Verify clicking "Create Project" on the Feed and My Projects pages successfully navigates to `/job-wizard`.
- **Mobile:** Verify clicking the same buttons navigates within the `BlazorWebView` to the Job Wizard.
- **Cross-Platform:** Ensure "Go Back" and navigation to details pages (Tradesman/Project) still work as expected.
