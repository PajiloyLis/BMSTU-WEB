namespace Project.Integration.Tests.Infrastructure;

public static class IntegrationApiClientFactory
{
    private const string ExternalBaseUrlEnv = "INTEGRATION_BASE_URL";
    public static HttpClient CreateClient()
    {
        var externalBaseUrl = Environment.GetEnvironmentVariable(ExternalBaseUrlEnv);
        if (string.IsNullOrWhiteSpace(externalBaseUrl))
        {
            throw new InvalidOperationException(
                $"{ExternalBaseUrlEnv} is not set. Integration tests are external-only and require a running app container URL.");
        }

        var normalizedBaseUrl = NormalizeBaseUrl(externalBaseUrl);
        var client = new HttpClient
        {
            BaseAddress = new Uri(normalizedBaseUrl)
        };

        EnsureExternalApplicationIsReachable(client, normalizedBaseUrl);
        return client;
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        return baseUrl.EndsWith("/", StringComparison.Ordinal)
            ? baseUrl
            : $"{baseUrl}/";
    }

    private static void EnsureExternalApplicationIsReachable(HttpClient client, string baseUrl)
    {
        Exception? lastException = null;
        for (var i = 0; i < 60; i++)
        {
            try
            {
                using var response = client.GetAsync("api/v1/companies").GetAwaiter().GetResult();
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    return;

                lastException = new InvalidOperationException(
                    $"External app at '{baseUrl}' is reachable but returned {(int)response.StatusCode} on preflight GET api/v1/companies.");
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            Thread.Sleep(1000);
        }

        throw new InvalidOperationException(
            $"INTEGRATION_BASE_URL is set to '{baseUrl}', but preflight request to 'api/v1/companies' failed. " +
            "Make sure the app container is running and reachable from the test process.",
            lastException);
    }
}
