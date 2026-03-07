# MAUI & MVVM Performance and Reactivity Guidelines

These guidelines dictate how data binding, reactivity, and backend queries should be structured in the BuildSmart MAUI application to ensure optimal mobile performance, avoid expensive full-page reloads, and provide a "React-like" snappy user experience.

## 1. Local Reactivity ("React-like" State Management)
- **Targeted Updates:** NEVER reload an entire page's dataset (e.g., calling `LoadPageDataAsync()`) after a single mutation (like editing a comment or submitting a reply).
- **Observable Properties:** Use the MVVM Toolkit's `[ObservableProperty]` and `ObservableCollection<T>`. When a mutation succeeds via the GraphQL API, take the returned updated entity and apply those changes *directly* to the existing object in the local ViewModel.
- **In-Place UI Updates:** Because the models inherit from `ObservableObject`, modifying a property locally will instantly update the UI without needing an expensive network call.
  - *Example (Editing):* `targetQuestion.QuestionText = newText; targetQuestion.IsEdited = true;`
  - *Example (Adding):* `targetQuestion.Replies.Add(newReply);`

## 2. Lazy Loading & Database Optimization
- **Do Not Over-Fetch:** Do not pull deeply nested relational data (like entire threaded comment histories) on the initial page load.
- **Batching & Pagination:** Use GraphQL batching and pagination (e.g., `offset` and `limit`) to load nested data only when requested.
- **Expand/Collapse Logic:** For threaded conversations or long lists, load a small subset initially (e.g., top-level questions only or the first 3 replies). Fetch the rest incrementally via a "Load More" action.

## 3. UI Event Handling (Binding Context Safety)
- **DataTemplates & Commands:** Avoid binding `Command` properties on `Button` or `TapGestureRecognizer` elements inside deeply nested `DataTemplate` (like `CollectionView` or `BindableLayout`). It often leads to binding context mismatches.
- **Code-Behind Routing:** Prefer using the `Clicked` or `Tapped` events wired to the `*.xaml.cs` code-behind. From the code-behind, retrieve the `CommandParameter` and forward the action to the main ViewModel.
  - *Example:* 
    ```csharp
    private void OnEditClicked(object sender, EventArgs e) {
        if (sender is Button btn && btn.CommandParameter is MyModel item) {
            _viewModel.EditCommand.Execute(item);
        }
    }
    ```

## 4. UI Rendering Restraints
- **Avoid Heavy Layouts:** Minimize the use of deeply nested `StackLayout` or `Grid` components to prevent the Android Choreographer from skipping frames.
- **Hardware Acceleration Considerations:** Be mindful of features that require hardware-accelerated canvases (like complex `RippleDrawable` patterns or intense shadowing) and provide simple fallbacks for lower-end devices.