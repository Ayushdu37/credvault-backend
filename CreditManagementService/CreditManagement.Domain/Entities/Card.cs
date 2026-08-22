using CreditManagement.Domain.Enums;

namespace CreditManagement.Domain.Entities;

// Represents a registered credit card
public class Card
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; } // Soft reference to Identity Service User
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumberMasked { get; set; } = string.Empty; // e.g., **** **** **** 1234
    public string CardNumberHash { get; set; } = string.Empty; // SHA-256 for duplicate card detection
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Issuer { get; set; } = string.Empty; // Visa, Mastercard, RuPay, Amex
    public decimal CreditLimit { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: One Card -> Many Bills
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
}
