# PowerShell Script to Update All GitHub Secrets for BuildSmart
# Usage: Run this script in PowerShell to update or set GitHub repository secrets via GitHub CLI (gh).

Param (
    [string]$Repo = "bDikov/buildsmart"
)

Write-Host "Updating GitHub Secrets for repository: $Repo..." -ForegroundColor Cyan

# 1. Database Secrets
gh secret set DB_USER --repo $Repo --body "postgres"
# gh secret set DB_PASSWORD --repo $Repo --body "YOUR_DB_PASSWORD"

# 2. Domain & System Secrets
gh secret set DOMAIN_WEB --repo $Repo --body "buildsmart.bg"

# 3. Facebook Ads, Webhook & Login Secrets
gh secret set FACEBOOK_APP_ID --repo $Repo --body "1400203902055364"
gh secret set FACEBOOK_APP_SECRET --repo $Repo --body "761d6e49031ea9270ddb98bf8b4230ca"
gh secret set FACEBOOK_VERIFY_TOKEN --repo $Repo --body "BuildSmart_FB_Webhook_Secret_Token_2026"
# gh secret set FACEBOOK_PIXEL_ID --repo $Repo --body "YOUR_META_PIXEL_ID"

# 4. Google OAuth Secrets
# gh secret set GOOGLE_CLIENT_ID --repo $Repo --body "YOUR_GOOGLE_CLIENT_ID"
# gh secret set GOOGLE_CLIENT_SECRET --repo $Repo --body "YOUR_GOOGLE_CLIENT_SECRET"

# 5. Gemini AI Secrets
# gh secret set GEMINI_API_KEY --repo $Repo --body "YOUR_GEMINI_API_KEY"

# 6. Sentry DSN
# gh secret set SENTRY_API_DSN --repo $Repo --body "YOUR_SENTRY_DSN"

# 7. PostHog Analytics
# gh secret set POSTHOG_API_KEY --repo $Repo --body "YOUR_POSTHOG_KEY"
# gh secret set POSTHOG_API_HOST --repo $Repo --body "https://us.i.posthog.com"

Write-Host "GitHub Secrets updated successfully!" -ForegroundColor Green
