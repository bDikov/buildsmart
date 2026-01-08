namespace BuildSmart.Maui;

public static class ApiConfig
{
    public static string GetBaseUrl()
    {
        // Use https and port 7212 to match the Kestrel/Project launch profile
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
        {
            // 10.0.2.2 is the special alias to your host loopback interface
            return "https://10.0.2.2:7212";
        }
        
        return "https://localhost:7212";
    }

    public static string GetGraphQLUrl() => $"{GetBaseUrl()}/graphql";

    public static string GetGraphQLWebSocketUrl() => $"{GetBaseUrl().Replace("https", "wss")}/graphql";
}
