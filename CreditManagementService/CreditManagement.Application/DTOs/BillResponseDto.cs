using CreditManagement.Domain.Enums;

namespace CreditManagement.Application.DTOs;

// Response model returning bill information
public class BillResponseDto
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public DateOnly BillingCycleStart { get; set; }
    public DateOnly BillingCycleEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MinimumDue { get; set; }
    public DateOnly DueDate { get; set; }
    public BillStatus Status { get; set; }
    public DateTime GeneratedAt { get; set; }
}
