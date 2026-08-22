using CreditManagement.Domain.Enums;

namespace CreditManagement.Application.DTOs;

// Response model returning payment transaction details
public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid BillId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime PaymentDate { get; set; }
}
