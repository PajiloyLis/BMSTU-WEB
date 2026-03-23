namespace Project.HttpServer.ExternalServices.AgePrediction;

public interface IAgePredictionClient
{
    Task<AgePredictionResult> PredictAgeAsync(string name, CancellationToken cancellationToken = default);
}

