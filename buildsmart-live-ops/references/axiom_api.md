# Axiom API & Query Reference for BuildSmart

Use these commands or the Axiom MCP server to query production logs in real time.

**Default Dataset:** `buildsmart-prod`
**Endpoint:** `https://api.axiom.co/v1/datasets/buildsmart-prod/query`
**Regional Ingest Endpoint:** `https://eu-central-1.aws.edge.axiom.co/v1/logs`

## Authentication
Every API request must include the header:
`Authorization: Bearer xaat-060d6033-7c92-4f00-9095-abf05f5a7b7d`

---

## Method A: Querying via Axiom MCP Server
If the Axiom MCP server is registered in `settings.json`, agents can invoke the tool directly:
```json
{
  "apl": "['buildsmart-prod'] | sort by _time desc | take 25"
}
```

---

## Method B: Querying via PowerShell (Invoke-RestMethod)

### 1. View Latest Production Logs
```powershell
$body = @{
    apl = "['buildsmart-prod'] | sort by _time desc | take 20"
    startTime = (Get-Date).AddHours(-24).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

$res = Invoke-RestMethod -Uri "https://api.axiom.co/v1/datasets/buildsmart-prod/query" `
    -Method Post `
    -Headers @{ "Authorization" = "Bearer $env:AXIOM_TOKEN" } `
    -ContentType "application/json" `
    -Body $body

$res.matches | Select-Object -ExpandProperty data
```

### 2. Search Logs by JobId
```powershell
$body = @{
    apl = "['buildsmart-prod'] | where ['@m'] contains '$jobId' or JobId == '$jobId' | sort by _time desc | take 50"
    startTime = (Get-Date).AddDays(-7).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

$res = Invoke-RestMethod -Uri "https://api.axiom.co/v1/datasets/buildsmart-prod/query" `
    -Method Post `
    -Headers @{ "Authorization" = "Bearer $env:AXIOM_TOKEN" } `
    -ContentType "application/json" `
    -Body $body

$res.matches | Select-Object -ExpandProperty data
```

### 3. Filter Production Errors & Exceptions
```powershell
$body = @{
    apl = "['buildsmart-prod'] | where ['@l'] == 'Error' or ['@l'] == 'Fatal' or severity_text == 'ERROR' | sort by _time desc | take 30"
    startTime = (Get-Date).AddHours(-12).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

$res = Invoke-RestMethod -Uri "https://api.axiom.co/v1/datasets/buildsmart-prod/query" `
    -Method Post `
    -Headers @{ "Authorization" = "Bearer $env:AXIOM_TOKEN" } `
    -ContentType "application/json" `
    -Body $body

$res.matches | Select-Object -ExpandProperty data
```
