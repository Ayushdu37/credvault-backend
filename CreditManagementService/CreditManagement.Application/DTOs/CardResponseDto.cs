namespace CreditManagement.Application.DTOs;

// Response model returning masked card details
public class CardResponseDto
{
    public Guid Id { get; set; }
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumberMasked { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
