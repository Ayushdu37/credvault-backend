using CreditManagement.Domain.Enums;

namespace CreditManagement.Domain.Entities;

// Represents a payment transaction against a bill
public class Payment
{
    public Guid Id { get; set; }
    public Guid BillId { get; set; }
    public Guid UserId { get; set; } // Soft reference to Identity Service User
    public decimal Amount { get; set; }
    public string TransactionReference { get; set; } = string.Empty; // Idempotency key to prevent duplicates
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Bill Bill { get; set; } = null!;
}
