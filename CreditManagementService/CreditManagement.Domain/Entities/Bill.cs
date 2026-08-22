using CreditManagement.Domain.Enums;

namespace CreditManagement.Domain.Entities;

// Represents a monthly statement/bill generated for a card
public class Bill
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public DateOnly BillingCycleStart { get; set; }
    public DateOnly BillingCycleEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MinimumDue { get; set; }
    public DateOnly DueDate { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Unpaid;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Card Card { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
