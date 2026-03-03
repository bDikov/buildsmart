# Specification: Bug fix edit pencil in threaded replies comments

## Overview
This track addresses a missing "edit" capability for threaded replies within the project feedback system. Currently, the edit pencil icon is only visible on top-level comments. When manually added to threaded replies, the application freezes (App Not Responding) upon interaction.

## Functional Requirements
- **Display Edit Pencil**: The edit pencil icon must be visible on all threaded replies (nested comments) authored by the current user.
- **Enable Inline Editing**: Tapping the edit pencil on a threaded reply must trigger the standard inline text editor, matching the experience of top-level comments.
- **Save/Cancel Actions**: Users must be able to save their edits or cancel them, returning the reply to its previous state.
- **Prevent App Freeze**: Resolve the root cause of the "App Not Responding" issue when interacting with the edit UI in a nested context.

## Non-Functional Requirements
- **Performance**: The transition to edit mode must be smooth and not cause UI freezes or noticeable lag on mobile devices.
- **Consistency**: The editing UI and behavior must be consistent between top-level comments and threaded replies.

## Acceptance Criteria
- [ ] Edit pencil icon appears on nested replies authored by the user in both Homeowner and Tradesman views.
- [ ] Tapping the pencil opens the inline editor without freezing the application.
- [ ] Edits are successfully saved and reflected in the UI.
- [ ] Canceling an edit reverts the text to its original state and hides the editor.
- [ ] UI remains responsive throughout the edit/save/cancel lifecycle.

## Out of Scope
- Adding edit capability for replies authored by *other* users.
- Changing the overall design of the feedback/comment system.
- Modifying the backend API unless necessary to support nested reply updates.
