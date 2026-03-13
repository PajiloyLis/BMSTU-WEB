namespace Project.Core.Exceptions;

public sealed class OtpCodeExpiredException : Exception
{
    public OtpCodeExpiredException(string message) : base(message)
    {
    }
}
