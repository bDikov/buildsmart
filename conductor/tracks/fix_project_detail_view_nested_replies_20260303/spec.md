# Specification: Bug fix @ProjectDetailView not loading after implementing neasted replies

## Goal
Fix the issue where `ProjectDetailView` fails to load after the implementation of nested replies in the Job Post Feedback system.

## Problem Description
- The `ProjectDetailView` is reported to be non-functional (not loading).
- This regression occurred after implementing nested replies for Job Post Feedback.
- Expected behavior: `ProjectDetailView` should display project details, including the threaded clarification system (nested replies).

## Constraints
- Must adhere to Clean Architecture.
- Maintain existing GraphQL schema and HotChocolate patterns.
- Ensure type safety and high test coverage (>80%).
