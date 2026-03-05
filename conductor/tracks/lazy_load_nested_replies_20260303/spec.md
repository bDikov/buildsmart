# Specification: Lazy-loading and Pagination for Nested Replies (Tradesman Q&A)

## Overview
Currently, the nested reply system in the Tradesman Q&A section loads all replies eagerly, which impacts UI performance and loading times. This feature introduces lazy-loading for these replies, allowing users to load them in batches of 5 on demand.

## Functional Requirements
- **Hidden Replies by Default**: All nested replies under Tradesman Q&A questions must be hidden initially.
- **"See Conversation" Toggle**: A button or label displaying the total number of replies (e.g., "See Conversation (12)") must be shown.
- **Offset-based Pagination**: Implement a GraphQL field or argument to fetch replies using `offset` and `limit` (batch size of 5).
- **Incremental Loading**: Tapping the "See More" button within an expanded thread should fetch and append the next 5 replies.
- **State Management**: The MAUI app must manage the local collection of replies per question to allow seamless appending without full reloads.

## Non-Functional Requirements
- **Performance**: Drastically reduce the initial payload size of project and auction detail queries by excluding nested replies from the default fetch.
- **UI Responsiveness**: The UI must remain fluid while fetching and inserting new replies into the view.

## Acceptance Criteria
- [ ] Tradesman Q&A questions load without any nested replies initially.
- [ ] A button correctly displays the total count of replies available.
- [ ] Tapping the button loads the first 5 replies.
- [ ] If more replies exist, a "Load More" button appears at the bottom of the visible replies.
- [ ] All 5 replies are fetched via a paginated GraphQL query using offset/limit.
- [ ] The "Load More" button disappears once all replies are shown.

## Out of Scope
- Applying this optimization to Project Feedbacks (Admin Clarifications) in this iteration.
- Implementing cursor-based pagination.
- Infinite scroll (must be manual click).
