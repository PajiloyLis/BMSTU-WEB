namespace Project.Core.Exceptions;

public sealed class AccountLockedException : Exception
{
    public AccountLockedException(string message) : base(message)
    {
    }
}
