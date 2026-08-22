namespace CreditManagement.Application.Exceptions;

// Thrown when a unique constraint or duplicate operation is detected
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
