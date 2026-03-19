namespace Project.Service.AuthorizationService.Configuration;

public sealed class AuthorizationSecurityConfiguration
{
    public bool Enabled { get; set; } = false;

    public bool RequireTwoFactor { get; set; } = true;

    public bool ExposeCodesForTests { get; set; } = false;

    public int MaxFailedLoginAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int OtpLifetimeMinutes { get; set; } = 5;

    public int RecoveryTokenLifetimeMinutes { get; set; } = 30;

    public int PasswordMaxAgeDays { get; set; } = 90;
}
