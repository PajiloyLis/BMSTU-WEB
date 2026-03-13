namespace Project.Core.Exceptions;

public sealed class InvalidRecoveryTokenException : Exception
{
    public InvalidRecoveryTokenException(string message) : base(message)
    {
    }
}
