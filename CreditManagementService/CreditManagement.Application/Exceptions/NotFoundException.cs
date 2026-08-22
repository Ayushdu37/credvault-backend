namespace CreditManagement.Application.Exceptions;

// Thrown when a requested resource (Card, Bill, Payment) is not found
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
