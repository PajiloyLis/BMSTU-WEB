using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Project.Dto.Http.External;

namespace Project.HttpServer.ExternalServices.AgePrediction;

public sealed class AgePredictionClient : IAgePredictionClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<AgePredictionOptions> _options;
    private readonly ILogger<AgePredictionClient> _logger;

    public AgePredictionClient(
        HttpClient httpClient,
        IOptionsMonitor<AgePredictionOptions> options,
        ILogger<AgePredictionClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgePredictionDto> PredictAgeAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Parameter 'name' must not be empty.", nameof(name));

        var cfg = _options.CurrentValue;
        var provider = cfg.UseMock ? "mock" : "real";
        var baseUrl = cfg.UseMock ? cfg.MockBaseUrl : cfg.RealBaseUrl;
        var endpoint = $"{baseUrl.TrimEnd('/')}/?name={Uri.EscapeDataString(name)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Age provider returned non-success. Provider={Provider}, Status={Status}, Body={Body}",
                provider, (int)response.StatusCode, body);
            throw new HttpRequestException($"Age provider returned status {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<AgifyResponse>(cancellationToken: cancellationToken);
        if (payload is null)
            throw new HttpRequestException("Age provider returned empty response.");

        return new AgePredictionDto(
            payload.Name ?? name,
            payload.Age,
            payload.Count ?? 0,
            provider);
    }

    private sealed class AgifyResponse
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
        public int? Count { get; set; }
    }
}
