using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Database.Context;
using Database.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Project.Core.Exceptions;
using Project.Core.Models.User;
using Project.Core.Repositories;
using Project.Core.Services;
using Project.Service.AuthorizationService.Configuration;

namespace Project.Services.AuthorizationService;

public class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IUserRepository _usersRepository;
    private readonly ProjectDbContext _dbContext;
    private readonly JwtConfiguration _jwtConfiguration;
    private readonly PasswordHashingConfiguration _hashingConfiguration;
    private readonly AuthorizationSecurityConfiguration _securityConfiguration;

    public AuthorizationService(
        IUserRepository userRepository,
        ProjectDbContext dbContext,
        IOptions<JwtConfiguration> jwtConfiguration,
        IOptions<PasswordHashingConfiguration> hashingConfiguration,
        IOptions<AuthorizationSecurityConfiguration> securityConfiguration,
        ILogger<AuthorizationService> logger)
    {
        _usersRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jwtConfiguration = jwtConfiguration.Value ?? throw new ArgumentNullException(nameof(jwtConfiguration));
        _hashingConfiguration = hashingConfiguration.Value ?? throw new ArgumentNullException(nameof(hashingConfiguration));
        _securityConfiguration = securityConfiguration.Value ?? throw new ArgumentNullException(nameof(securityConfiguration));
    }

    public async Task<AuthorizationData> RegisterAsync(string password, string email)
    {
        try
        {
            var salt = GenerateSalt();
            var hashedPassword = HashPassword(password, salt);
            var id = await GetCurrentUserIdAsync(email);
            var user = await _usersRepository.AddUserAsync(new BaseUser(id, email, hashedPassword, salt, "admin"));

            await InitializeSecurityDefaultsForRegisteredUserAsync(email);

            var accessToken = GenerateJwtToken(email, user.Role);
            return new AuthorizationData(email, accessToken, user.Id);
        }
        catch (UserAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "User with email: {Email} already exists", email);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            throw;
        }
    }

    public async Task<AuthorizationData> LoginAsync(string email, string password)
    {
        if (_securityConfiguration.Enabled && _securityConfiguration.RequireTwoFactor)
            throw new InvalidOperationException("Use /api/v1/auth/login/start and /api/v1/auth/login/complete when 2FA is enabled.");

        var user = await ValidateCredentialsOrThrowAsync(email, password);
        var accessToken = GenerateJwtToken(email, user.Role);
        return new AuthorizationData(email, accessToken, user.Id);
    }

    public async Task<LoginStartResult> StartLoginAsync(string email, string password)
    {
        var user = await ValidateCredentialsOrThrowAsync(email, password);
        if (!_securityConfiguration.Enabled || !_securityConfiguration.RequireTwoFactor)
        {
            var token = GenerateJwtToken(email, user.Role);
            return new LoginStartResult(false, null, null, new AuthorizationData(email, token, user.Id));
        }

        var otpCode = GenerateOtpCode();
        var challengeId = Guid.NewGuid();
        user.OtpChallengeId = challengeId;
        user.OtpCodeHash = ComputeHash(otpCode);
        user.OtpExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, _securityConfiguration.OtpLifetimeMinutes));
        await _dbContext.SaveChangesAsync();

        return new LoginStartResult(
            true,
            challengeId,
            user.OtpExpiresAtUtc,
            null,
            _securityConfiguration.ExposeCodesForTests ? otpCode : null);
    }

    public async Task<AuthorizationData> CompleteLoginWithOtpAsync(Guid challengeId, string otpCode)
    {
        var user = await _dbContext.UserDb.FirstOrDefaultAsync(x => x.OtpChallengeId == challengeId);
        if (user is null)
            throw new OtpChallengeNotFoundException($"OTP challenge {challengeId} not found.");

        if (user.OtpExpiresAtUtc is null || user.OtpExpiresAtUtc < DateTimeOffset.UtcNow)
            throw new OtpCodeExpiredException("OTP code has expired.");

        if (string.IsNullOrWhiteSpace(user.OtpCodeHash))
            throw new OtpChallengeNotFoundException("OTP challenge is invalid.");

        if (!SecureEquals(user.OtpCodeHash, ComputeHash(otpCode)))
            throw new InvalidOtpCodeException("Invalid OTP code.");

        user.OtpChallengeId = null;
        user.OtpCodeHash = null;
        user.OtpExpiresAtUtc = null;
        user.FailedLoginAttempts = 0;
        user.LockoutUntilUtc = null;
        user.PasswordChangeRequired = false;
        await _dbContext.SaveChangesAsync();

        var accessToken = GenerateJwtToken(user.Email, user.Role);
        return new AuthorizationData(user.Email, accessToken, user.Id);
    }

    public async Task<PasswordRecoveryResult> RequestPasswordRecoveryAsync(string email)
    {
        var user = await _dbContext.UserDb.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
            throw new UserNotFoundException($"User with email: {email} not found");

        var recoveryToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.RecoveryTokenHash = ComputeHash(recoveryToken);
        user.RecoveryTokenExpiresAtUtc =
            DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, _securityConfiguration.RecoveryTokenLifetimeMinutes));
        await _dbContext.SaveChangesAsync();

        return new PasswordRecoveryResult(_securityConfiguration.ExposeCodesForTests ? recoveryToken : null);
    }

    public async Task ResetPasswordWithRecoveryTokenAsync(string email, string recoveryToken, string newPassword)
    {
        var user = await _dbContext.UserDb.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
            throw new UserNotFoundException($"User with email: {email} not found");

        if (user.RecoveryTokenExpiresAtUtc is null ||
            user.RecoveryTokenExpiresAtUtc < DateTimeOffset.UtcNow ||
            string.IsNullOrWhiteSpace(user.RecoveryTokenHash))
        {
            throw new InvalidRecoveryTokenException("Recovery token is invalid or expired.");
        }

        if (!SecureEquals(user.RecoveryTokenHash, ComputeHash(recoveryToken)))
            throw new InvalidRecoveryTokenException("Recovery token is invalid or expired.");

        var newSalt = GenerateSalt();
        user.Salt = newSalt;
        user.Password = HashPassword(newPassword, newSalt);
        user.RecoveryTokenHash = null;
        user.RecoveryTokenExpiresAtUtc = null;
        user.FailedLoginAttempts = 0;
        user.LockoutUntilUtc = null;
        user.LastPasswordChangedAtUtc = DateTimeOffset.UtcNow;
        user.PasswordChangeRequired = false;
        await _dbContext.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(string email, string oldPassword, string newPassword)
    {
        var user = await ValidateCredentialsOrThrowAsync(email, oldPassword, checkPasswordExpiry: false);
        var newSalt = GenerateSalt();
        user.Salt = newSalt;
        user.Password = HashPassword(newPassword, newSalt);
        user.LastPasswordChangedAtUtc = DateTimeOffset.UtcNow;
        user.PasswordChangeRequired = false;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Guid> GetCurrentUserIdAsync(string email)
    {
        try
        {
            return await _usersRepository.GetCurrentUserIdAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting user with email: {Email}", email);
            throw;
        }
    }

    private async Task<UserDb> ValidateCredentialsOrThrowAsync(
        string email,
        string password,
        bool checkPasswordExpiry = true)
    {
        var user = await _dbContext.UserDb.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
            throw new UserNotFoundException($"User with email: {email} not found");

        if (user.LockoutUntilUtc.HasValue && user.LockoutUntilUtc > DateTimeOffset.UtcNow)
            throw new AccountLockedException($"Account is locked until {user.LockoutUntilUtc:O}");

        var hashedPassword = HashPassword(password, user.Salt);
        if (hashedPassword != user.Password)
        {
            await HandleFailedPasswordAttemptAsync(user);
            throw new InvalidPasswordException($"Invalid password for email: {email}");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutUntilUtc = null;

        if (checkPasswordExpiry && _securityConfiguration.Enabled && IsPasswordExpired(user))
        {
            user.PasswordChangeRequired = true;
            await _dbContext.SaveChangesAsync();
            throw new PasswordChangeRequiredException("Password lifetime expired. Password change is required.");
        }

        await _dbContext.SaveChangesAsync();
        return user;
    }

    private async Task HandleFailedPasswordAttemptAsync(UserDb user)
    {
        user.FailedLoginAttempts += 1;
        if (_securityConfiguration.Enabled &&
            user.FailedLoginAttempts >= Math.Max(1, _securityConfiguration.MaxFailedLoginAttempts))
        {
            user.LockoutUntilUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, _securityConfiguration.LockoutMinutes));
            user.FailedLoginAttempts = 0;
        }

        await _dbContext.SaveChangesAsync();
    }

    private bool IsPasswordExpired(UserDb user)
    {
        var maxAgeDays = Math.Max(1, _securityConfiguration.PasswordMaxAgeDays);
        return user.PasswordChangeRequired || user.LastPasswordChangedAtUtc < DateTimeOffset.UtcNow.AddDays(-maxAgeDays);
    }

    private async Task InitializeSecurityDefaultsForRegisteredUserAsync(string email)
    {
        var user = await _dbContext.UserDb.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
            return;

        user.FailedLoginAttempts = 0;
        user.LockoutUntilUtc = null;
        user.OtpChallengeId = null;
        user.OtpCodeHash = null;
        user.OtpExpiresAtUtc = null;
        user.RecoveryTokenHash = null;
        user.RecoveryTokenExpiresAtUtc = null;
        user.PasswordChangeRequired = false;
        user.LastPasswordChangedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    private string GenerateSalt(int size = 64)
    {
        var salt = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return Convert.ToBase64String(salt);
    }

    private string HashPassword(string password, string salt)
    {
        var iterations = _hashingConfiguration.Iterations;
        var len = _hashingConfiguration.KeyLen;
        var saltBytes = Convert.FromBase64String(salt);
        return Convert.ToBase64String(
            KeyDerivation.Pbkdf2(password, saltBytes, KeyDerivationPrf.HMACSHA512, iterations, len));
    }

    private string GenerateJwtToken(string email, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfiguration.SecurityKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtConfiguration.Issuer,
            audience: _jwtConfiguration.Audience,
            claims: claims,
            expires: DateTime.Now.AddHours(_jwtConfiguration.AccessTokenLifetimeMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string ComputeHash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private string GenerateOtpCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }

    private bool SecureEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
