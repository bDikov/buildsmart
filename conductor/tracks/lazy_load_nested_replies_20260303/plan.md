# Implementation Plan: Lazy-loading and Pagination for Nested Replies (Tradesman Q&A)

## Phase 1: Backend Implementation (GraphQL Pagination)
- [x] Task: Conductor - Add `replyCount` field to `JobPostQuestion` type in the API.
- [x] Task: Conductor - Update `JobPostQuestionType` to support `replies` with `offset` and `limit` arguments.
- [x] Task: Conductor - Update `IJobPostService` and implementation to fetch replies with pagination.
- [x] Task: Conductor - Write unit tests in `BuildSmart.Api.Tests` to verify paginated reply fetching.
- [x] Task: Conductor - User Manual Verification 'Backend Pagination' (Protocol in workflow.md)

## Phase 2: MAUI ViewModel and Service Update
- [x] Task: Conductor - Update `AuctionHubViewModel` to store loaded replies per question.
- [x] Task: Conductor - Implement `LoadMoreRepliesCommand` in `AuctionHubViewModel` using offset/limit.
- [x] Task: Conductor - Update the initial auction fetching query to exclude nested replies (fetch only `replyCount`).
- [x] Task: Conductor - Write unit tests in `BuildSmart.Maui.Tests` for `AuctionHubViewModel` pagination logic.
- [x] Task: Conductor - User Manual Verification 'ViewModel Logic' (Protocol in workflow.md)

## Phase 3: MAUI UI Implementation
- [x] Task: Conductor - Update `AuctionHubPage.xaml` to display the "See Conversation (X)" button instead of replies.
- [x] Task: Conductor - Implement the expanded view for replies within the question's layout.
- [x] Task: Conductor - Add "See More" button at the bottom of the reply list if `loaded < total`.
- [x] Task: Conductor - Ensure smooth UI updates when new replies are appended to the list.
- [x] Task: Conductor - User Manual Verification 'UI Implementation' (Protocol in workflow.md)

## Phase 4: Finalization and Refinement
- [x] Task: Conductor - Verify that pagination works correctly for questions with many replies (e.g., > 10).
- [x] Task: Conductor - Perform final manual verification on target platforms (Windows/iOS).
- [x] Task: Conductor - Ensure code coverage for new logic exceeds 80%.
- [x] Task: Conductor - User Manual Verification 'Final Verification' (Protocol in workflow.md)
