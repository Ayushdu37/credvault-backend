using System.ComponentModel.DataAnnotations;

namespace CreditManagement.Application.DTOs;

// Request model for paying a bill
public class PayBillRequestDto
{
    [Required]
    public Guid BillId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(100)]
    public string TransactionReference { get; set; } = string.Empty; // Idempotency reference from client/gateway
}
