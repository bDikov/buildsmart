namespace BuildSmart.SharedUI;

public static class ApiConfig
{
    public static string? BaseUrlOverride { get; set; }
    public static string? SentryDsn { get; set; } = "";
    public static string? ClarityProjectId { get; set; } = "";
    public static string? GoogleTagManagerId { get; set; } = "";
    public static string? PostHogApiKey { get; set; } = "";
    public static string? PostHogApiHost { get; set; } = "https://us.i.posthog.com";

    public static string GetBaseUrl()
    {
        if (!string.IsNullOrEmpty(BaseUrlOverride))
        {
            return BaseUrlOverride;
        }

        return "https://localhost:44378";
    }

    public static string GetGraphQLUrl() => $"{GetBaseUrl()}/graphql/";

    public static string GetGraphQLWebSocketUrl() => $"{GetBaseUrl().Replace("https", "wss")}/graphql/";
}

