using CreditManagement.Application.DTOs;

namespace CreditManagement.Application.Interfaces;

// Service interface orchestrating Bill management use cases
public interface IBillService
{
    Task<BillResponseDto> GenerateBillAsync(Guid userId, Guid cardId, GenerateBillRequestDto request);
    Task<IEnumerable<BillResponseDto>> GetBillsByUserIdAsync(Guid userId);
    Task<BillDetailsDto> GetBillDetailsAsync(Guid userId, Guid billId);
}
