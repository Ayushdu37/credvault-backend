namespace CreditManagement.Application.DTOs;

// Detailed bill response including card details and payment history
public class BillDetailsDto : BillResponseDto
{
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumberMasked { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public IEnumerable<PaymentResponseDto> Payments { get; set; } = new List<PaymentResponseDto>();
}
