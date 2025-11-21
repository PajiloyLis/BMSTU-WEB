using System.ComponentModel.DataAnnotations;
using Database.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Core.Exceptions;
using Project.Core.Models.Position;
using Project.Core.Services;
using Project.Dto.Http;
using Project.Dto.Http.Converters;
using Project.Dto.Http.Position;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Annotations;

namespace Project.HttpServer.Controllers;

[ApiController]
[Route("/api/v1")]
public class PositionController : ControllerBase
{
    private readonly IPositionService _positionService;
    private readonly ILogger<PositionController> _logger;

    public PositionController(ILogger<PositionController> logger,
        IPositionService positionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
    }

    [AllowAnonymous]
    [HttpGet("/positions/{positionId:guid}")]
    [SwaggerOperation("getPositionById")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(PositionDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> GetPosition([FromRoute] [Required] Guid positionId)
    {
        try
        {
            var position = await _positionService.GetPositionByIdAsync(positionId);

            return Ok(PositionConverter.Convert(position));
        }
        catch (PositionNotFoundException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status404NotFound, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPost("/positions")]
    [SwaggerOperation("createPosition")]
    [SwaggerResponse(StatusCodes.Status201Created, type: typeof(PositionDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> CreatePosition([FromBody] [Required] CreatePositionDto newPosition)
    {
        try
        {
            var createdPosition = await _positionService.AddPositionAsync(newPosition.ParentId,
                newPosition.Title,
                newPosition.CompanyId);

            return StatusCode(StatusCodes.Status201Created, PositionConverter.Convert(createdPosition));
        }
        catch (PositionAlreadyExistsException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status400BadRequest, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (ArgumentException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status400BadRequest, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
        }
    }

    [Authorize(Roles="admin")]
    [HttpPatch("/positions/{positiondId:guid}/title")]
    [SwaggerOperation("updatePositionTitle")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(PositionDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> UpdatePositionTitle([FromRoute] [Required] Guid positionId, [FromBody] [Required] string? title)
    {
        try
        {
            var updatedPosition = await _positionService.UpdatePositionTitleAsync(positionId, title);

            return Ok(PositionConverter.Convert(updatedPosition));
        }
        catch (PositionNotFoundException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status404NotFound, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (PositionAlreadyExistsException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status400BadRequest, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (ArgumentException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status400BadRequest, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
        }
    }

    [Authorize(Roles="admin")]
    [HttpDelete("/positions/{positionId:guid}")]
    [SwaggerOperation("deletePosition")]
    [SwaggerResponse(StatusCodes.Status204NoContent, type: typeof(bool))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> DeletePosition([FromRoute] [Required] Guid positionId)
    {
        try
        {
            await _positionService.DeletePositionAsync(positionId);

            return StatusCode(StatusCodes.Status204NoContent);
        }
        catch (PositionNotFoundException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status404NotFound, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
        }
    }

    [AllowAnonymous]
    [HttpGet("/positions/{headPositionId:guid}/subordinates")]
    [SwaggerOperation("getSubordinatesPositionsByHeadPositionId")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(PositionHierarchyDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> GetSubordinatesPositionsByHeadPositionId([FromRoute] [Required] Guid positionId)
    {
        try
        {
            var positions = await _positionService.GetSubordinatesAsync(positionId);

            return Ok(positions.Select(PositionHierarchyConverter.Convert));
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
        }
    }
    
    // [Authorize(Roles = "admin")]
    // [HttpPatch("updatePositionParentWithSubordinates")]
    // [SwaggerOperation("updatePositionParentWithSubordinates")]
    // [SwaggerResponse(StatusCodes.Status200OK, type: typeof(PositionDto))]
    // [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    // [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    // [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    // public async Task<IActionResult> UpdatePositionParentWithSubordinates([FromBody] [Required] UpdatePositionDto updatePosition)
    // {
    //     try
    //     {
    //         var updatedPosition = await _positionService.UpdatePositionParentWithSubordinatesAsync(updatePosition.Id,
    //             updatePosition.CompanyId,
    //             updatePosition.ParentId,
    //             updatePosition.Title);
    //
    //         return Ok(PositionConverter.Convert(updatedPosition));
    //     }
    //     catch (PositionNotFoundException e)
    //     {
    //         _logger.LogWarning(e, e.Message);
    //         return StatusCode(StatusCodes.Status404NotFound, new ErrorDto(e.GetType().Name, e.Message));
    //     }
    //     catch (PositionAlreadyExistsException e)
    //     {
    //         _logger.LogWarning(e, e.Message);
    //         return StatusCode(StatusCodes.Status400BadRequest, new ErrorDto(e.GetType().Name, e.Message));
    //     }
    //     catch (ArgumentException e)
    //     {
    //         _logger.LogWarning(e, e.Message);
    //         return StatusCode(StatusCodes.Status400BadRequest, new ErrorDto(e.GetType().Name, e.Message));
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError(e, e.Message);
    //         return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
    //     }
    // }
    
    [Authorize(Roles = "admin")]
    [HttpPatch("/positions/{positionId}/parent")]
    [SwaggerOperation("updatePositionParent")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(PositionDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> UpdatePositionParent([FromRoute] [Required] Guid positionId, [FromBody] [Required] Guid parentId, [FromQuery] [Required] int updateMode)
    {
        try
        {
            var updatedPosition = await _positionService.UpdatePositionParent(positionId, parentId, updateMode.ToPositionUpdateMode());

            return Ok(PositionConverter.Convert(updatedPosition));
        }
        catch (PositionNotFoundException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status404NotFound, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (PositionAlreadyExistsException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status400BadRequest, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (ArgumentException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status400BadRequest, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
        }
    }
    
    
    [AllowAnonymous]
    [HttpGet("companies/{companyId:guid}/headPosition")]
    [SwaggerOperation("getCompanyHeadPositionById")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(PositionDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> GetCompanyHeadPosition([FromRoute] [Required] Guid companyId)
    {
        try
        {
            var position = await _positionService.GetHeadPositionByCompanyIdAsync(companyId);

            return Ok(PositionConverter.Convert(position));
        }
        catch (PositionNotFoundException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status404NotFound, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
        }
    }
}