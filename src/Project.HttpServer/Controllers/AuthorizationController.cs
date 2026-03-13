using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Project.Core.Exceptions;
using Project.Dto.Http;
using Project.Dto.Http.Converters;
using Project.Dto.Http.User;
using Swashbuckle.AspNetCore.Annotations;
using IAuthorizationService = Project.Core.Services.IAuthorizationService;

namespace Project.HttpServer.Controllers;

[ApiController]
[Route("/api/v1/auth")]
public class AuthorizationController : ControllerBase
{
    private readonly ILogger<AuthorizationController> _logger;

    private readonly IAuthorizationService _authorizationService;

    public AuthorizationController(IAuthorizationService authorizationService,
        ILogger<AuthorizationController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerOperation("login")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(AuthorizationDataDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> Login([FromBody][Required] LoginDto dto)
    {
        try
        {
            var authData = await _authorizationService.LoginAsync(dto.Email, dto.Password);
            return Ok(AuthorizationDataConverter.Convert(authData));
        }
        catch (PasswordChangeRequiredException ex)
        {
            _logger.LogWarning(ex, "Password change required for user: {Email}", dto.Email);
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (AccountLockedException ex)
        {
            _logger.LogWarning(ex, "Account locked for user: {Email}", dto.Email);
            return StatusCode(StatusCodes.Status423Locked, new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (InvalidPasswordException ex)
        {
            _logger.LogWarning(ex, $"Invalid password for user: {dto.Email}");
            return BadRequest(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, $"User with email: {dto.Email} not found");
            return NotFound(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(ex.GetType().Name, ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpPost("login/start")]
    [SwaggerOperation("start login with optional otp challenge")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(LoginStartResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status423Locked, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> StartLogin([FromBody][Required] LoginDto dto)
    {
        try
        {
            var result = await _authorizationService.StartLoginAsync(dto.Email, dto.Password);
            return Ok(new LoginStartResponseDto(
                result.RequiresOtp,
                result.ChallengeId,
                result.OtpExpiresAtUtc,
                result.AuthorizationData is null ? null : AuthorizationDataConverter.Convert(result.AuthorizationData),
                result.OtpCodeForTests));
        }
        catch (PasswordChangeRequiredException ex)
        {
            _logger.LogWarning(ex, "Password change required for user: {Email}", dto.Email);
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (AccountLockedException ex)
        {
            _logger.LogWarning(ex, "Account locked for user: {Email}", dto.Email);
            return StatusCode(StatusCodes.Status423Locked, new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (InvalidPasswordException ex)
        {
            _logger.LogWarning(ex, "Invalid password for user: {Email}", dto.Email);
            return BadRequest(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with email: {Email} not found", dto.Email);
            return NotFound(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during start login");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(ex.GetType().Name, ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpPost("login/complete")]
    [SwaggerOperation("complete login with otp code")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(AuthorizationDataDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status410Gone, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> CompleteLogin([FromBody][Required] LoginOtpConfirmDto dto)
    {
        try
        {
            var authData = await _authorizationService.CompleteLoginWithOtpAsync(dto.ChallengeId, dto.OtpCode);
            return Ok(AuthorizationDataConverter.Convert(authData));
        }
        catch (InvalidOtpCodeException ex)
        {
            _logger.LogWarning(ex, "Invalid otp code for challenge: {ChallengeId}", dto.ChallengeId);
            return BadRequest(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (OtpChallengeNotFoundException ex)
        {
            _logger.LogWarning(ex, "Otp challenge not found: {ChallengeId}", dto.ChallengeId);
            return NotFound(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (OtpCodeExpiredException ex)
        {
            _logger.LogWarning(ex, "Otp code expired for challenge: {ChallengeId}", dto.ChallengeId);
            return StatusCode(StatusCodes.Status410Gone, new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during otp login completion");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(ex.GetType().Name, ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [SwaggerOperation("register")]
    [SwaggerResponse(StatusCodes.Status200OK, type: typeof(AuthorizationDataDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> Register([FromBody] LoginDto dto)
    {
        try
        {
            var authData = await _authorizationService.RegisterAsync(dto.Password,
                dto.Email);
            return Ok(AuthorizationDataConverter.Convert(authData));
        }
        catch (UserAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, $"User with email {dto.Email} already exists");
            return BadRequest(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, $"Employee or company with email {dto.Email} was not found for registration");
            return BadRequest(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during registration");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(ex.GetType().Name, ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpPost("password/recovery/request")]
    [SwaggerOperation("request password recovery")]
    [SwaggerResponse(StatusCodes.Status202Accepted, type: typeof(PasswordRecoveryRequestResponseDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> RequestPasswordRecovery([FromBody][Required] PasswordRecoveryRequestDto dto)
    {
        try
        {
            var result = await _authorizationService.RequestPasswordRecoveryAsync(dto.Email);
            return Accepted(new PasswordRecoveryRequestResponseDto(
                "Recovery token has been issued.",
                result.RecoveryTokenForTests));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with email {Email} not found for recovery request", dto.Email);
            return NotFound(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password recovery request");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(ex.GetType().Name, ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpPost("password/recovery/confirm")]
    [SwaggerOperation("confirm password recovery")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> ConfirmPasswordRecovery([FromBody][Required] PasswordRecoveryConfirmDto dto)
    {
        try
        {
            await _authorizationService.ResetPasswordWithRecoveryTokenAsync(dto.Email, dto.RecoveryToken, dto.NewPassword);
            return Ok();
        }
        catch (InvalidRecoveryTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid recovery token for user: {Email}", dto.Email);
            return BadRequest(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with email {Email} not found for recovery confirm", dto.Email);
            return NotFound(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password recovery confirmation");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(ex.GetType().Name, ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpPost("password/change")]
    [SwaggerOperation("change password by old/new password")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status423Locked, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> ChangePassword([FromBody][Required] PasswordChangeDto dto)
    {
        try
        {
            await _authorizationService.ChangePasswordAsync(dto.Email, dto.OldPassword, dto.NewPassword);
            return Ok();
        }
        catch (PasswordChangeRequiredException ex)
        {
            _logger.LogWarning(ex, "Password change required for user: {Email}", dto.Email);
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (AccountLockedException ex)
        {
            _logger.LogWarning(ex, "Account locked for user: {Email}", dto.Email);
            return StatusCode(StatusCodes.Status423Locked, new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (InvalidPasswordException ex)
        {
            _logger.LogWarning(ex, "Invalid password while change for user: {Email}", dto.Email);
            return BadRequest(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with email {Email} not found for password change", dto.Email);
            return NotFound(new ErrorDto(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password change");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(ex.GetType().Name, ex.Message));
        }
    }
    
    // [Authorize(Roles = "employee")]
    // [HttpGet("currentUser")]
    // [SwaggerResponse(StatusCodes.Status200OK, type: typeof(Guid))]
    // [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    // [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    // [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    // public async Task<IActionResult> GetCurrentUserId()
    // {
    //     try
    //     {
    //         var email = User.FindFirst(ClaimTypes.Email)?.Value;
    //         if (email is null)
    //         {
    //             throw new SecurityTokenException("Invalid token");
    //         }
    //
    //         var authData = await _authorizationService.GetCurrentUserIdAsync(email);
    //         return Ok(authData);
    //     }
    //     catch (SecurityTokenException e)
    //     {
    //         _logger.LogWarning(e, "Token invalid");
    //         return BadRequest(new ErrorDto(e.GetType().Name, e.Message));
    //     }
    //     catch (UserNotFoundException ex)
    //     {
    //         _logger.LogWarning(ex, $"User with not found");
    //         return NotFound(new ErrorDto(ex.GetType().Name, ex.Message));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error occurred during login");
    //         return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(ex.GetType().Name, ex.Message));
    //     }
    // }
}