namespace Project.Core.Exceptions;

public sealed class PasswordChangeRequiredException : Exception
{
    public PasswordChangeRequiredException(string message) : base(message)
    {
    }
}
