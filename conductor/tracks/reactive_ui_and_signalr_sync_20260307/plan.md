# Implementation Plan: Reactive UI and SignalR Sync

## Phase 1: GraphQL Payload Optimization
- [ ] Task: Review and update `schema.graphql` and `Mutations.graphql` (StrawberryShake) to ensure `editJobQuestion`, `editJobAnswer`, and `replyToQuestion` return the necessary data fields (id, text, isEdited, updatedAt, author).
- [ ] Task: Ensure the `.NET API` resolvers for these mutations return the full, updated objects.

## Phase 2: Local Component-Based Reactivity
- [ ] Task: Implement `UpdateQuestion` and `UpdateAnswer` methods in `QuestionViewModel.cs` or `CommonViewModels.cs` that update the local `[ObservableProperty]` fields in-place.
- [ ] Task: Implement an `AddReply` method to append directly to the local `Replies` ObservableCollection.
- [ ] Task: Modify `AuctionHubViewModel.cs` mutations (`EditQuestionAsync`, `EditAnswerAsync`, `ReplyToQuestionAsync`) to use these local update methods and remove the calls to `await LoadAuctionAsync(id)`.

## Phase 3: Backend SignalR Hub Expansion
- [ ] Task: Create or modify `NotificationHub` in the `.NET API` to support Groups based on `JobPostId`. Implement `JoinAuctionGroup` and `LeaveAuctionGroup` methods.
- [ ] Task: Update `JobPostService.cs` (or a dedicated integration service) to trigger `Clients.Group(jobPostId).SendAsync("ReceiveQuestionUpdate", ...)` and `"ReceiveNewReply"` whenever a question is edited or replied to.

## Phase 4: Frontend SignalR Integration
- [ ] Task: Update MAUI `AuctionHubViewModel` (or a dedicated SignalR service) to connect to the SignalR Hub when the page is loaded and join the specific Job/Auction group.
- [ ] Task: Implement event listeners in MAUI for `ReceiveQuestionUpdate` and `ReceiveNewReply`.
- [ ] Task: Wire the incoming SignalR data to the exact same ViewModel update methods created in Phase 2, ensuring the UI thread is used (`MainThread.BeginInvokeOnMainThread`).
- [ ] Task: Ensure the SignalR connection properly disconnects or leaves the group when navigating away from the page (`OnDisappearing` / `IQueryAttributable`).

## Phase 5: Verification & Cleanup
- [ ] Task: Manual testing to verify editing and replying works instantly without full page reloads.
- [ ] Task: Manual testing with two clients to verify SignalR synchronization.