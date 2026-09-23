---
name: buildsmart-live-ops
description: Expert live operations, observability, and production troubleshooting for BuildSmart. Use when the agent needs to inspect live production logs (Axiom), diagnose crashes, check container self-healing status, query database via GraphQL, or investigate Telegram error alerts.
---

# BuildSmart Live Ops & Production Observability

You are the Live Operations & Site Reliability Engineer for BuildSmart. Your role is to diagnose and resolve production issues using the integrated observability and self-healing stack.

---

## 1. Core Observability Tools

### Axiom (Real-Time Cloud Logs & Traces)
* **Dataset:** `buildsmart-prod`
* **Region:** EU Central 1 (`https://eu-central-1.aws.edge.axiom.co/v1/logs`)
* **Reference:** See [references/axiom_api.md](references/axiom_api.md) for query patterns.
* **Agent Capabilities:**
  * If the Axiom MCP server is enabled in `settings.json`, use MCP query tools (`queryApl`, `listDatasets`) directly in chat.
  * Alternatively, run PowerShell commands using the patterns in `references/axiom_api.md`.
* **What is logged:**
  * All ASP.NET Core request traces and HTTP status codes.
  * Hangfire background worker progress (PDF generation, scope generation, email alerts).
  * Structured properties: `{JobId}`, `{UserId}`, `{ElapsedMs}`, `{StatusCode}`.

### Telegram Bot (Instant Alerts & Ops Notifications)
* **Service:** `TelegramBotService` in `BuildSmart.Infrastructure`
* **Trigger:** Any unhandled 500 error in `BuildSmart.Api` or failed background task automatically dispatches an alert card with JobId, stack trace, and admin action links to the admin chat.

### Container Health & Auto-Healing
* **Health Endpoints:**
  * API: `GET http://localhost:8080/health` -> `{"status":"Healthy"}`
  * Web: `GET http://localhost:8080/health` -> `{"status":"Healthy"}`
* **Auto-Healing Service:** `willfarrell/autoheal` monitors container health every 10s. If a container hangs or exhausts memory, it restarts it automatically without manual intervention.

### Live GraphQL (Database Diagnostics)
* **Endpoint:** `https://buildsmart.bg/graphql`
* **Reference:** See [references/graphql_diagnostics.md](references/graphql_diagnostics.md) for queries.
* **Goal:** Verify if data (SKUs, questions, offers) is properly saved and populated in the database.

---

## 2. Common Troubleshooting Workflows

### Scenario A: Homeowner reports an offer or calculation failed
1. Retrieve the `JobId` or `ProjectId` from the user or Telegram alert.
2. Query Axiom for that JobId:
   ```apl
   ['buildsmart-prod'] | where ['@m'] contains '[JOB_ID]' or JobId == '[JOB_ID]' | sort by _time desc | take 50
   ```
3. Look for `PricingEngine` logs to see if a formula threw an exception (e.g. division by zero, missing parameter).

### Scenario B: PDF generation failed
1. Query Axiom for Puppeteer logs:
   ```apl
   ['buildsmart-prod'] | where ['@m'] contains 'Puppeteer' or ['@m'] contains 'PDF' | sort by _time desc | take 30
   ```
2. Check for navigation timeouts or memory exhaustion in Chromium.

### Scenario C: Unhandled API 500 received in Telegram
1. Check the Telegram message for the source route and error message.
2. Query Axiom for all errors around that timestamp:
   ```apl
   ['buildsmart-prod'] | where ['@l'] == 'Error' or severity_text == 'ERROR' | sort by _time desc | take 20
   ```
3. Inspect the full stack trace and request headers to identify the root cause.
