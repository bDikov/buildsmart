# Live GraphQL Diagnostics for BuildSmart

The live API endpoint is `https://buildsmart.bg/graphql`.
You can use `curl.exe` to execute queries and mutations for diagnostic purposes.

## Authentication
Most queries require a JWT token. You can obtain one by logging in via the app or using a service account token if available.

## Diagnostic Queries

### 1. Check Service SKUs Count
```graphql
query {
  serviceSkus {
    totalCount
  }
}
```

### 2. Verify Job Post Status
```graphql
query($id: UUID!) {
  jobPost(id: $id) {
    id
    title
    status
    aiCalculations {
      totalEstimatedPrice
      tasks {
        title
        estimatedPrice
        skuItems {
          skuCode
          quantity
        }
      }
    }
  }
}
```

## Executing via Curl
`curl.exe -X POST -H "Content-Type: application/json" -d "{\"query\": \"...\"}" https://buildsmart.bg/graphql`
