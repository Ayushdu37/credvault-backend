using CreditManagement.Application.DTOs;

namespace CreditManagement.Application.Interfaces;

// Service interface orchestrating Payment processing and history use cases
public interface IPaymentService
{
    Task<PaymentResponseDto> PayBillAsync(Guid userId, PayBillRequestDto request);
    Task<IEnumerable<PaymentResponseDto>> GetHistoryAsync(Guid userId);
    Task<PaymentDetailsDto> GetPaymentDetailsAsync(Guid userId, Guid paymentId);
}
