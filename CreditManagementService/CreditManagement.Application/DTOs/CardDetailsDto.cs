namespace CreditManagement.Application.DTOs;

// Detailed card response including recent bills summary
public class CardDetailsDto : CardResponseDto
{
    public IEnumerable<BillResponseDto> RecentBills { get; set; } = new List<BillResponseDto>();
}
