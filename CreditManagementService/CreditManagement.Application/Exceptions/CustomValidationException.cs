namespace CreditManagement.Application.Exceptions;

// Thrown when custom domain validation fails (e.g., Luhn checksum failure)
public class CustomValidationException : Exception
{
    public CustomValidationException(string message) : base(message) { }
}
