using Project.Dto.Http.External;

namespace Project.HttpServer.ExternalServices.AgePrediction;

public interface IAgePredictionClient
{
    Task<AgePredictionDto> PredictAgeAsync(string name, CancellationToken cancellationToken = default);
}
