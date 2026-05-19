# Sentry API Reference for BuildSmart

Use these `curl.exe` patterns to query the BuildSmart Sentry organization.

**Organization Slug:** `test-ufb`
**Project Slug:** `buildsmart-api`

## Authentication
Every request must include the header:
`Authorization: Bearer YOUR_SENTRY_TOKEN_HERE`

## Common Queries

### 1. List Latest Issues
`curl.exe -s -H "Authorization: Bearer [TOKEN]" https://sentry.io/api/0/projects/test-ufb/buildsmart-api/issues/?sort=new`

### 2. Search Events by JobId
`curl.exe -s -H "Authorization: Bearer [TOKEN]" "https://sentry.io/api/0/projects/test-ufb/buildsmart-api/events/?query=[JOB_ID]"`

### 3. Get Event Details (Breadcrumbs & Context)
`curl.exe -s -H "Authorization: Bearer [TOKEN]" https://sentry.io/api/0/projects/test-ufb/buildsmart-api/events/[EVENT_ID]/`

### 4. Search for Estimator Logs
`curl.exe -s -H "Authorization: Bearer [TOKEN]" "https://sentry.io/api/0/projects/test-ufb/buildsmart-api/events/?query=Estimator"`
