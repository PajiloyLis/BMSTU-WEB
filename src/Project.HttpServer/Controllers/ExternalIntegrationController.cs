using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Dto.Http;
using Project.Dto.Http.External;
using Project.HttpServer.ExternalServices.AgePrediction;
using Swashbuckle.AspNetCore.Annotations;

namespace Project.HttpServer.Controllers;

[ApiController]
[Route("/api/v1/external")]
public sealed class ExternalIntegrationController : ControllerBase
{
    private readonly IAgePredictionClient _agePredictionClient;
    private readonly ILogger<ExternalIntegrationController> _logger;

    public ExternalIntegrationController(
        IAgePredictionClient agePredictionClient,
        ILogger<ExternalIntegrationController> logger)
    {
        _agePredictionClient = agePredictionClient ?? throw new ArgumentNullException(nameof(agePredictionClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [AllowAnonymous]
    [HttpGet("age-prediction")]
    [SwaggerOperation("Predict age by person name via external service")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(AgePredictionDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status502BadGateway, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> PredictAge([FromQuery] string name, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _agePredictionClient.PredictAgeAsync(name, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid age prediction request");
            return BadRequest(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "External age service call failed");
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing age prediction");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(ex.GetType().Name, ex.Message));
        }
    }
}
