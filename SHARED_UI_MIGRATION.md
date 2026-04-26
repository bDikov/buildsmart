# Blazor Hybrid Migration: Separating UI into BuildSmart.SharedUI

This document outlines the architectural shift made to separate the Blazor Hybrid UI from the native `.NET MAUI` project into a dedicated Razor Class Library (RCL) named `BuildSmart.SharedUI`.

## Why Did We Do This? (The Goal)

Previously, all Blazor components (`.razor`), CSS (`app.css`), ViewModels, and GraphQL schemas were housed directly inside `BuildSmart.Maui`. 

While this works, it tightly couples web UI logic to native mobile code (like iOS provisioning profiles and Android SDKs). By extracting the UI into `BuildSmart.SharedUI`, we achieve the following:

1. **True Cross-Platform Reusability:** The exact same UI code in `BuildSmart.SharedUI` can now be referenced by `BuildSmart.Maui` (for iOS/Android/Desktop apps) AND a future `BuildSmart.Web` project (for a standard website using Blazor WebAssembly or Blazor Server) without writing the UI twice.
2. **Separation of Concerns:** UI logic is strictly HTML/C#, completely decoupled from native device APIs.
3. **Faster Development Cycles:** The Shared UI project can be tested and developed independently without needing heavy mobile emulators to compile.

---

## What Exactly Was Moved?

During the migration, the following core directories were moved from `BuildSmart.Maui` into the new `BuildSmart.SharedUI` project:
*   `Components/` (All `.razor` files including `MainLayout`, `Login`, `BsDropdown`, etc.)
*   `ViewModels/` (All MVVM logic)
*   `GraphQL/` (StrawberryShake API definitions and schemas)
*   `wwwroot/css/` (The Figma-based `app.css` design system)
*   `Services/` (Web-agnostic services like `IAuthService`)

---

## How the Native MAUI Project Was Fixed

Moving files across projects completely breaks the existing C# namespaces and XAML references. Here is the step-by-step resolution that was applied to make the MAUI shell successfully compile and load the new Shared UI.

### 1. Project Reference
The `BuildSmart.Maui.csproj` was updated to include a direct `<ProjectReference>` to `..\BuildSmart.SharedUI\BuildSmart.SharedUI.csproj`. This allows MAUI to "see" the extracted code.

### 2. C# Namespace Updates (`MauiProgram.cs` & Code-Behinds)
When ViewModels and Services were moved, their C# namespaces changed from `BuildSmart.Maui.ViewModels` to `BuildSmart.SharedUI.ViewModels`. 
*   We performed a bulk update across `MauiProgram.cs` and all `Views/*.xaml.cs` code-behinds to replace the old `using BuildSmart.Maui...` statements with the new `BuildSmart.SharedUI...` namespaces.
*   This ensured the Dependency Injection container in `MauiProgram.cs` could properly resolve and register the ViewModels.

### 3. XAML Namespace Updates (`xmlns`)
Native MAUI `.xaml` files (which we are keeping as native Shell wrappers for the Blazor views) needed to know that their bound ViewModels now lived in an external assembly.
*   **Old:** `xmlns:vm="clr-namespace:BuildSmart.Maui.ViewModels"`
*   **New:** `xmlns:vm="clr-namespace:BuildSmart.SharedUI.ViewModels;assembly=BuildSmart.SharedUI"`
*   This was applied across all 20+ native views so data-binding compiled correctly.

### 4. BlazorWebView Component Mapping
The `<BlazorWebView>` controls inside the native `.xaml` pages act as the bridge to the web UI. They had to be updated to point to the new SharedUI assembly.
*   **Old:** `<RootComponent ComponentType="{x:Type local:Components.Pages.Login}" />`
*   **New:** We added `xmlns:pages="clr-namespace:BuildSmart.SharedUI.Components.Pages;assembly=BuildSmart.SharedUI"` to the XAML header.
*   We then updated the component type to `{x:Type pages:Login}`.

### 5. Restoring Native Converters
During the file move, the `Converters/` folder was accidentally deleted. 
*   Converters (like `InverseBoolConverter` and `StepToProgressConverter`) implement the `IValueConverter` interface, which is a **native MAUI/XAML concept**, not a Blazor/HTML concept.
*   Therefore, they cannot exist in `BuildSmart.SharedUI`. We used Git to restore the `Converters/` folder directly back into `BuildSmart.Maui` where they belong, allowing the native XAML pages to compile.

---

## Future Development Guidelines

Moving forward with this dual-project architecture:

1. **UI Components & ViewModels:** Always create new `.razor` components and `.cs` ViewModels inside `BuildSmart.SharedUI`.
2. **Native Device Features:** If a ViewModel needs to access a mobile device's camera or GPS, you must:
   *   Create an interface (e.g., `ICameraService`) inside `BuildSmart.SharedUI`.
   *   Implement the actual native code inside `BuildSmart.Maui`.
   *   Register the implementation in `MauiProgram.cs`.
3. **Styling:** Continue using the `var(--bg-card)` Figma variables in `BuildSmart.SharedUI/wwwroot/css/app.css`.

This architecture represents the gold standard for enterprise Blazor Hybrid applications.

---

## Post-Migration Fixes

### 1. API Connection Failure (Android Emulator)
After extracting the Shared UI, the app could not connect to the API on the Android emulator. This was because `ApiConfig.cs` (now in the shared library) was relying on a mocked `DeviceInfo` class that incorrectly returned `DevicePlatform.Web`. This caused Android to attempt connecting to `https://localhost:7212` instead of the required `https://10.0.2.2:7212` loopback alias.
*   **Fix:** We introduced a static `BaseUrlOverride` property in `BuildSmart.SharedUI.ApiConfig`. The native MAUI app now inspects the real `Microsoft.Maui.Devices.DeviceInfo` in `MauiProgram.cs` and injects the correct base URL before registering any services.

### 2. Sign-In Buttons Fix
Addressed issues with the sign-in buttons (including external auth) to ensure proper navigation and event handling within the new Blazor Hybrid structure.

### 3. Native AppShell Routing Crash (Index Out of Range)
When navigating via MAUI's native `Shell.Current.GoToAsync` (e.g., during logout to `//LoginPage`) with `FlyoutBehavior="Disabled"` and multiple root `ShellContent` items, the app would crash with `ArgumentOutOfRangeException: Index was out of range`.
*   **Fix:** Wrapped the `ShellContent` items in `AppShell.xaml` inside a `<TabBar>` and set `Shell.TabBarIsVisible="False"`. This provides the required native routing structure to MAUI without displaying the tabs.

### 4. Blazor "Address Not Found" Error
After the migration, clicking Blazor navigation links (like `/feed` or `/user-profile`) resulted in a Blazor "Sorry, there's nothing at this address" message. This happened because the Blazor `<Router>` in `BuildSmart.Maui/Main.razor` was only scanning the native MAUI assembly for `@page` directives.
*   **Fix:** Updated `Main.razor` to include the `BuildSmart.SharedUI` assembly in the router's `AdditionalAssemblies` parameter:
    ```razor
    <Router AppAssembly="@typeof(Main).Assembly" AdditionalAssemblies="new[] { typeof(BuildSmart.SharedUI.Components.Layout.MainLayout).Assembly }">
    ```

### 5. UI Thread Freezes (ObservableCollection modified on Background Thread)
After an asynchronous GraphQL API call completed in the background, updating an `ObservableCollection` (like `Auctions.Add()`) that was bound to the UI would cause the app to silently freeze or throw an invisible `InvalidOperationException` ("Collection was modified").
*   **Fix:** Wrapped all collection modifications inside the ViewModels with `AppServiceLocator.MainThread.BeginInvokeOnMainThread(...)` to mathematically guarantee UI state changes only occur on the main renderer thread.

### 6. Fatal Native Crashes (VMDisconnectedException) on Alerts
When the `AlertService` attempted to display a popup dialog (`DisplayAlert`) from a background thread (e.g., inside a `catch (Exception)` block for a failed network call), Android would bypass `try/catch` handlers and throw a fatal, unrecoverable native application crash (`Mono.Debugger.Soft.VMDisconnectedException`).
*   **Fix:** Updated `BuildSmart.Maui.Services.AlertService` to marshal all UI dialogs to the Main Thread via `MainThread.InvokeOnMainThreadAsync(...)`.

### 7. Dependency Injection Crash on Native Pickers
Clicking the "Settings" menu (which navigates to `UserProfileViewModel`) caused an instant `InvalidOperationException: Unable to resolve service` crash. This occurred because the ViewModel in the `SharedUI` project required `BuildSmart.SharedUI.MauiMocks.IMediaPicker`, but `MauiProgram.cs` was attempting to inject the actual native Android/iOS `Microsoft.Maui.Media.IMediaPicker`.
*   **Fix:** Created "Adapter" classes in the native MAUI project (`AppMediaPicker` and `AppFilePicker`) that implement the `SharedUI.MauiMocks` interfaces but secretly call the real `Microsoft.Maui` native hardware APIs under the hood. Registered these adapters in `MauiProgram.cs`.