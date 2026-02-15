# AI Implementation Guide: Smart Scope Generation

This document explains the integration of Google Gemini AI into the BuildSmart platform for professional construction scope generation.

## Overview
The application uses **Gemini 1.5 Pro** to transform raw homeowner inputs (from the Smart Blueprint wizard) into professional, technically sound, and legally defensive Scopes of Work (SOW).

## Core Components

### 1. `IAiService` (Interface)
Defined in `BuildSmart.Core.Application`. It abstracts the AI logic to allow for easy switching between Mock and Real implementations.
- `GenerateJobScopeAsync(JobPost jobPost)`: Generates a detailed SOW for a specific job.
- `GenerateProjectSummaryAsync(Project project)`: Generates a strategic roadmap for multi-job projects.

### 2. `GeminiAiService` (Implementation)
Located in `BuildSmart.Infrastructure`. It uses the `Mscc.GenerativeAI` library.
- **Model:** `gemini-1.5-pro` (chosen for superior reasoning and technical writing).
- **Prompt Engineering:** Uses a sophisticated "Senior Construction Manager & Quantity Surveyor" system prompt.
- **Guardrails:** Includes strict anti-hallucination rules to prevent "scope creep" and ensure factual consistency.

### 3. `ScopeGenerationWorker` (Background Service)
Located in `BuildSmart.Api`. It processes AI generation tasks asynchronously to ensure the UI remains responsive. It dequeues jobs, calls the `IAiService`, and updates the `JobPost` status.

## Prompt Logic (Senior Construction Manager)
The AI is instructed to:
1.  **Infer Technical Sub-tasks:** e.g., "Install Sink" automatically adds plumbing disconnection and leak testing.
2.  **Separate Materials:** Categorizes into "Contractor Supplied" vs "Owner Supplied".
3.  **Defensive Writing:** Includes mandatory clauses for building codes and permits.
4.  **Zero Hallucination:** Instructions to never add work not explicitly requested in the user's answers.

## Configuration
The API Key is stored in `appsettings.json` under:
```json
"Gemini": {
    "ApiKey": "YOUR_KEY"
}
```

## Security & Privacy
Because the application is configured for a professional/enterprise context:
- Data sent to the AI is strictly for scope generation.
- It is recommended to use **Paid Tier / Vertex AI** credentials in production to ensure data is not used for model training.
