# Specification: Reactive UI and SignalR Sync

## Overview
Currently, mutations in the Auction Hub (editing questions, editing answers, or adding replies) trigger a full page reload (`LoadAuctionAsync`). This leads to a degraded user experience, UI flickering, and unnecessary database queries. This track implements targeted component-level updates for local mutations and introduces SignalR to sync these granular updates in real-time across all connected clients.

## Goals
1. **Local Reactivity:** Eliminate the need to call `LoadAuctionAsync` after a successful mutation. Updates should be applied directly to the local `ObservableCollection` in the view models.
2. **Granular Payloads:** Ensure GraphQL mutations return the exact modified entity data required to update the UI.
3. **Real-time Synchronization:** Leverage SignalR to broadcast these granular updates to other clients viewing the same Auction page, updating their UI without manual refreshing.

## System Architecture

### 1. GraphQL Layer
- Mutations (`EditJobQuestion`, `EditJobAnswer`, `ReplyToQuestion`) must return the full schema of the updated entity.
- The MAUI client must request the necessary fields (e.g., `isEdited`, `updatedAt`, `questionText`, etc.) in the mutation response to update the local model.

### 2. MAUI ViewModels (`AuctionHubViewModel.cs`)
- Provide helper methods in `QuestionViewModel` (or `AuctionHubViewModel`) to inject updates.
  - `UpdateQuestion(IQuestionDetails updatedQuestion)`
  - `UpdateAnswer(IQuestionDetails updatedQuestion)`
  - `AddReply(IQuestionReplyDetails newReply)`
- Remove `await LoadAuctionAsync(id)` from the success blocks of the editing/replying commands.

### 3. SignalR Backend (`BuildSmart.Api`)
- Extend the `NotificationHub` (or create a new `AuctionHub`) to handle active connections for specific job posts (e.g., Grouping connections by `jobPostId`).
- `JobPostService` will trigger events like `ReceiveQuestionUpdate` or `ReceiveNewReply` and broadcast the serialized DTO to the relevant group.

### 4. SignalR Frontend Integration
- MAUI client establishes a SignalR connection upon entering the `AuctionHubPage` and joins the specific `jobPostId` group.
- Listeners for `ReceiveQuestionUpdate` and `ReceiveNewReply` are mapped to the same helper methods created in step 2 to update the UI seamlessly.

## Definition of Done
- Editing a question/answer updates the text and shows the "(EDITED)" label instantly without reloading the page.
- Submitting a reply adds it to the list instantly without reloading the page.
- Opening the same Auction page on two different devices/emulators demonstrates real-time syncing: editing or replying on Device A instantly updates the UI on Device B.
- No performance regressions or memory leaks caused by lingering SignalR connections.