namespace Project.HttpServer.ExternalServices.AgePrediction;

public sealed class AgePredictionOptions
{
    public bool Enabled { get; set; } = true;

    // Supported values: "mock", "real"
    public string Mode { get; set; } = "mock";

    public string RealBaseUrl { get; set; } = "https://api.agify.io";

    public string MockBaseUrl { get; set; } = "http://external-mock:8080";
}

