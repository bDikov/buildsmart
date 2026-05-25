# BuildSmart Frontend & Blazor Hybrid UI Guidelines

This document serves as the definitive guide for AI agents and human developers working on the frontend of the BuildSmart application. It captures the architecture, styling principles, and specific workflows established during the migration from native MAUI XAML to Blazor Hybrid.

## 1. Core Architecture: Blazor Hybrid

BuildSmart uses a **Blazor Hybrid** approach inside a .NET MAUI shell. 
- Native device integrations (Camera, File Picker) and navigation boundaries are handled by MAUI.
- All modern UI rendering, layouts, and responsive components are handled by HTML/CSS within `.razor` files.
- **Do not write new XAML views** unless strictly necessary for platform-specific shells. All new pages and components should be placed in `BuildSmart.Maui/Components/`.

### 1.1 Web vs Mobile Host Parity (CRITICAL)
Because this application runs as both a Server/Web app and a MAUI Mobile app, there are **two distinct HTML hosts**:
- **Web App Host:** `BuildSmart.Web/Components/App.razor`
- **Mobile MAUI Host:** `BuildSmart.Maui/wwwroot/index.html`

**STRICT RULE:** Whenever you add a new `<script>`, global JavaScript function (like `setCookie`), `<link>` stylesheet, or modify the `<head>` tag, you MUST apply the change to **BOTH** `App.razor` and `index.html`. Failure to duplicate these host-level changes will cause features to work on the web but crash on the mobile emulator (or vice versa).

## 2. ViewModel Injection & Data Flow

BuildSmart heavily utilizes the MVVM pattern with `CommunityToolkit.Mvvm`.

**CRITICAL: The Double-Injection Trap**
ViewModels in `MauiProgram.cs` are typically registered as `Transient` (meaning a new instance is created every time it is injected). 
- **Parent Pages:** Only the top-level parent page (e.g., `UserProfile.razor`) should use `@inject UserProfileViewModel ViewModel`.
- **Child Components:** Child components (e.g., `ProfileCard.razor`) **MUST NOT** `@inject` the ViewModel. Doing so will create a second, empty instance of the ViewModel, causing data binding to fail.
- **Data Passing:** Parent pages must pass the ViewModel down to child components as a parameter:
  ```razor
  <!-- In Parent Page -->
  <ProfileCard ViewModel="ViewModel" />
  ```
  ```razor
  <!-- In Child Component -->
  @code {
      [Parameter, EditorRequired]
      public UserProfileViewModel ViewModel { get; set; } = default!;
  }
  ```

## 3. Styling & The Figma Design System

We use a custom, Fimga-driven CSS design system. **We do not rely on Bootstrap for component styling** to avoid overriding conflicts, though Bootstrap is present in `index.html` for basic scaffolding.

**Theming & AI Agents:** If tasked with adding a new theme (like Dark Mode) or altering the design system, agents **MUST** read and follow the playbook at `conductor/THEME_IMPLEMENTATION_GUIDE.md`.

### Rules of CSS
1. **The `bs-` Namespace:** All custom utility classes must be prefixed with `bs-` (e.g., `bs-btn-primary`, `bs-card`, `bs-input`, `bs-placeholder`). This prevents Bootstrap from hijacking our styling (e.g., Bootstrap uses `.placeholder` for loading skeletons, which turns text into solid grey boxes!).
2. **No `!important`:** Do not use `!important` tags. The `bs-` namespace provides enough specificity.
3. **Global CSS Variables & The "No Hardcoding" Rule:** Always use the CSS variables defined in `wwwroot/css/app.css`. **NEVER hardcode colors (e.g., `rgba(255,255,255,0.1)` or `#FFFFFF`) inside `.razor` files.** Hardcoding colors instantly breaks the Light/Dark theme switching.
   - **Borders & Backgrounds:** If a component needs a border or a subtle background, you MUST use `var(--border-color)` or `var(--bg-card-alt)`.
   - **Text Colors & Light Theme Contrast:** Always verify your UI in the Light Theme. Variables like `--text-muted` and `--color-warning` must maintain a high enough opacity/darkness to be legible on pure white backgrounds (`--bg-card`).
   - **Typography (1-Typeface Rule):** `var(--font-h0)` to `var(--font-h3)`, `var(--font-body-1)`, `var(--font-body-2)`. (Fallback legacy fonts: `var(--font-primary)`, `var(--font-heading)`, `var(--font-secondary)`).
   - **Elevations (Surfaces):** `var(--elevation-00dp)` to `var(--elevation-24dp)`.
   - **Backgrounds:** `var(--bg-page)`, `var(--bg-card)`, `var(--bg-card-alt)`.
   - **Text:** `var(--text-primary)`, `var(--text-secondary)`, `var(--text-muted)`, `var(--text-disabled)`.
   - **Colors:** `var(--color-primary)`, `var(--color-secondary)`, `var(--color-success)`, `var(--color-warning)`, `var(--color-danger)`, `var(--color-info)`, `var(--color-tertiary)`.
   - **State Opacities:** `var(--state-disabled)`, `var(--state-hover)`, `var(--state-focus)`, etc.
4. **Dark Mode is Native:** Dark mode is handled automatically at the root level via `@media (prefers-color-scheme: dark)` in `app.css`. As long as you use `var(--bg-card)` and `var(--text-primary)`, components will seamlessly swap colors without requiring JS logic or duplicate CSS classes.

### Mobile-First & Responsive Layouts
To ensure the UI is fully functional on mobile devices within the MAUI WebView, always adhere to mobile-first responsive principles:
1. **Fluid Containers:** Avoid hardcoding large pixel widths (e.g., `max-width: 800px;` with large fixed paddings). Use fluid containers and reduce padding on smaller viewports (`padding: 12px;` or `1rem` on mobile, scaling up with `@media (min-width: 768px)`).
2. **Flex Wrapping:** Never assume horizontal space is infinite. When using `d-flex` for side-by-side elements, use `flex-wrap` and stack elements on small screens using `flex-column flex-sm-row`.
3. **Button Sizing:** Use full-width buttons on mobile (`w-100`) that snap to their auto-content width on larger screens (`w-sm-auto`).
4. **Prevent Horizontal Bleeding:** Apply `word-break: break-word;` and `overflow-wrap: break-word;` to text containers (titles, descriptions, scopes) and ensure parent containers have `overflow-x: hidden;` to prevent the UI from horizontal scrolling or breaking.

### Reusable Classes Available
- **Containers:** `.bs-card`
- **Buttons:** `.bs-btn-primary`, `.bs-btn-dark`, `.bs-btn-action-light`
- **Inputs:** `.bs-input`

## 4. Converting Figma to Blazor (The AI Workflow)

When an AI agent is tasked with building a new UI component from a Figma file, it must strictly follow this protocol:

### Step 1: Validating the `.fig` file
The AI relies on the Figma MCP (Model Context Protocol) tool to read local `.fig` files.
- **CRITICAL:** The `.fig` file must be a modern **ZIP archive format**. 
- If the tool throws `Error: Invalid .fig file: not a ZIP archive`, the human developer must open the design in the Figma Desktop/Web app and explicitly use **File -> "Save local copy..."** to generate a valid, modern `.fig` file.

### Step 2: Extracting the Tree
1. Run `mcp_fig_get_tree_summary` to understand the hierarchical structure of the layout.
2. Run `mcp_fig_get_node_details` on the specific target frame to extract precise flexbox gap dimensions, border radiuses, and text styles.

### Step 3: Building the `.razor` Component
1. Translate Figma "Auto Layout" into standard CSS Flexbox or Grid.
2. Replace hardcoded text with `@bind` or `@` evaluations tied to the injected ViewModel.
3. Extract SVG icons directly from the Figma node details and embed them inline. Remove hardcoded colors from the SVGs and use `fill="currentColor"` or `stroke="currentColor"` so they adapt dynamically to the Light/Dark theme text colors.
4. Replace hardcoded Figma hex/rgb colors with the equivalent `var(--...)` tokens from the design system.

## 5. UI/UX Interaction Standards

### Modals & Dialogs
Use fixed full-screen overlay divs with `backdrop-filter: blur(4px)` and a centered `.bs-card` for popup forms. Ensure the `.modal-content` has `max-height: 90vh; overflow-y: auto;` so forms do not run off the screen on mobile devices.

### Custom Dropdowns & Popups (The WebView Focus Bug)
Native WebViews (iOS WKWebView / Android WebView) handle touch events and focus differently than standard desktop browsers.
- **NEVER use `@onfocusout`:** Relying on `@onfocusout` on a wrapper `div` to close custom dropdowns or calendars is incredibly buggy on mobile and will cause the popup to instantly close unexpectedly.
- **USE an Invisible Overlay:** Instead, place a full-screen, transparent `<div class="dropdown-overlay" @onclick="Close"></div>` directly behind your popup menu. This safely intercepts "outside clicks" on mobile devices to close the menu.
- **Prevent Focus Loss on Internal Buttons:** If your popup has interactive elements (like `<` and `>` buttons on a calendar), tapping them on mobile can pull focus away and accidentally close the popup. Always apply `@onmousedown:preventDefault="true"` and `type="button"` to these internal navigation buttons to retain focus.

### Event Callbacks
When a child component (like a button inside a card) needs to trigger a UI state change in the parent (like opening a modal), use `EventCallback`:
```razor
[Parameter]
public EventCallback OnActionClicked { get; set; }
```

### ViewModel Commands
Bind primary actions directly to the ViewModel's asynchronous RelayCommands:
```razor
<button class="bs-btn-primary" @onclick="ViewModel.SaveProfileCommand.ExecuteAsync">Save</button>
```

## 6. Localization Policy

**STRICT RULE:** Hardcoded text in `.razor` components is strictly prohibited. 
All static text, labels, button texts, and placeholders MUST be localized. Because the application targets multiple languages, you must always use the injected localization service (e.g., `@Loc["Your_Resource_Key"]`) or ensure the text is driven by a translatable resource. 
## 7. Video Reels & Carousel Implementation

The application uses a TikTok/Tinder-style vertical video stack (Reels) heavily reliant on Blazor + JavaScript Interop for high-performance gesture tracking and media playback. When maintaining or expanding this feature, adhere to the following architectural patterns:

### DOM Stability via Circular Buffering
**CRITICAL:** When looping a video feed infinitely, **do not clone `Guid`s or destroy DOM nodes.** 
- Browsers aggressively clear their internal `<video>` buffer when a DOM element is destroyed or re-created, causing black-screen flickering and massive data re-downloading.
- **The Fix:** Implement a **Circular Buffer**. When an item is swiped, `Remove()` the exact object from the top of the `ObservableCollection` and `Insert(0, item)` to move it to the bottom. Because the `Id` remains identical, Blazor simply moves the existing HTML node in the DOM, perfectly preserving the browser's downloaded media buffer and the initialized `Plyr` instance.
- **CSS Stack Logic:** The stack uses `nth-last-child` selectors to arrange the cards. The active card is `nth-last-child(1)`, the next queued card is `nth-last-child(2)`, and the previous/bottom card is `nth-last-child(3)`.

### Bi-Directional Swiping (2-Way Carousel)
The swipe interaction (`ProcessSwipeEndFromJS`) is bi-directional:
- **Swipe Left (`deltaX < 0`):** Moves forward in the queue. The current top card is removed and pushed to the bottom of the stack.
- **Swipe Right (`deltaX > 0`):** Moves backward in the queue. The absolute bottom-most card is pulled directly to the top of the stack.

### Browser Autoplay Policies & Synchronous Play
Modern browsers (iOS Safari, Chrome) strictly block audio if a video is played programmatically without a direct, synchronous "trusted user gesture".
- **The Trap:** If JavaScript waits for Blazor to re-render and trigger the next play state via an async SignalR/WebSocket message, the browser will classify the play command as "untrusted" and forcefully mute the video.
- **The Solution:** The next video MUST be played synchronously inside the JavaScript `touchend` event loop (`window.reelsObserver.playVideo(nextVideoId);`). Only *after* the video starts playing should JS notify Blazor to update the underlying C# state.

### Global Mute Synchronization
The application maintains a `globalMuted` state so that unmuting one video unmutes all subsequent videos.
- **Race Condition Prevention:** Because the browser's Autoplay Policy can aggressively force a video into a `muted` state if it detects a violation, this forced mute triggers a `volumechange` event that can accidentally overwrite the user's `globalMuted` preference.
- **The Lock:** When programmatically changing the volume or falling back to a muted state, JS applies a temporary lock (`player.__ignoreVolumeChangeUntil = Date.now() + 200;`). The `volumechange` event listener must check this lock and ignore any changes made by the system within that window, ensuring only genuine user taps update the `globalMuted` state.