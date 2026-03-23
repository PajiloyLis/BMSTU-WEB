namespace Project.Core.Models.User;

public sealed class PasswordRecoveryResult
{
    public PasswordRecoveryResult(string? recoveryTokenForTests)
    {
        RecoveryTokenForTests = recoveryTokenForTests;
    }

    public string? RecoveryTokenForTests { get; }
}
