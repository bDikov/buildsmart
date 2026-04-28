namespace BuildSmart.SharedUI;

public static class ApiConfig
{
    public static string? BaseUrlOverride { get; set; }

    public static string GetBaseUrl()
    {
        if (!string.IsNullOrEmpty(BaseUrlOverride))
        {
            return BaseUrlOverride;
        }

        return "https://localhost:7212";
    }

    public static string GetGraphQLUrl() => $"{GetBaseUrl()}/graphql";

    public static string GetGraphQLWebSocketUrl() => $"{GetBaseUrl().Replace("https", "wss")}/graphql";
}

