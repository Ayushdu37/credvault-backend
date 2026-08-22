namespace CreditManagement.Application.DTOs;

// Detailed payment response with bill and card summary
public class PaymentDetailsDto : PaymentResponseDto
{
    public decimal BillTotalAmount { get; set; }
    public decimal BillRemainingDue { get; set; }
    public string CardNumberMasked { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
}
