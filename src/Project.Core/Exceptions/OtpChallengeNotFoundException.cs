namespace Project.Core.Exceptions;

public sealed class OtpChallengeNotFoundException : Exception
{
    public OtpChallengeNotFoundException(string message) : base(message)
    {
    }
}
