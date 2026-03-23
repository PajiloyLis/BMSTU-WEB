using System.Net.Mime;
using Project.Dto.Http.External;
using Project.HttpServer.ExternalServices.AgePrediction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project.HttpServer.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/external/age")]
public sealed class ExternalAgeController : ControllerBase
{
    private readonly IAgePredictionClient _client;

    public ExternalAgeController(IAgePredictionClient client)
    {
        _client = client;
    }

    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<AgePredictionDto>> PredictAge([FromQuery] string name, CancellationToken ct)
    {
        var result = await _client.PredictAgeAsync(name, ct);
        var dto = new AgePredictionDto(result.Name, result.Age, result.Count);
        return Ok(dto);
    }
}

