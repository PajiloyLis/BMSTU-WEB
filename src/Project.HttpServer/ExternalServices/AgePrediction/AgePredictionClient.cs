using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Project.HttpServer.ExternalServices.AgePrediction;

public sealed class AgePredictionClient : IAgePredictionClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AgePredictionOptions _options;

    public AgePredictionClient(
        IHttpClientFactory httpClientFactory,
        IOptions<AgePredictionOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<AgePredictionResult> PredictAgeAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be provided", nameof(name));

        if (!_options.Enabled)
            throw new InvalidOperationException("Внешнее предсказание возраста отключено.");

        var baseUrl = string.Equals(_options.Mode, "real", StringComparison.OrdinalIgnoreCase)
            ? _options.RealBaseUrl
            : _options.MockBaseUrl;

        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var requestUri = $"{normalizedBaseUrl}/?name={Uri.EscapeDataString(name)}";

        using var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AgifyResponse>(cancellationToken: cancellationToken);
        if (payload is null)
            throw new InvalidOperationException("Внешний сервис вернул пустой ответ.");

        return new AgePredictionResult
        {
            Name = payload.Name ?? name,
            Age = payload.Age,
            Count = payload.Count
        };
    }

    private sealed class AgifyResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("age")]
        public int? Age { get; set; }

        [JsonPropertyName("count")]
        public int? Count { get; set; }
    }
}

