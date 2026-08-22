namespace CreditManagement.Application.Exceptions;

// Thrown when an unauthenticated or unauthorized action is attempted
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
