namespace Project.HttpServer.ExternalServices.AgePrediction;

public sealed class AgePredictionOptions
{
    public bool UseMock { get; set; } = true;

    public string MockBaseUrl { get; set; } = "http://external-age-mock:8080";

    public string RealBaseUrl { get; set; } = "https://api.agify.io";

    public int TimeoutSeconds { get; set; } = 10;
}
