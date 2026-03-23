namespace Project.Core.Exceptions;

public sealed class InvalidOtpCodeException : Exception
{
    public InvalidOtpCodeException(string message) : base(message)
    {
    }
}
