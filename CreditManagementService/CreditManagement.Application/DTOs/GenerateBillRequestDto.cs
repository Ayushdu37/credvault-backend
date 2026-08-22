using System.ComponentModel.DataAnnotations;

namespace CreditManagement.Application.DTOs;

// Request model for generating a new statement/bill for a card
public class GenerateBillRequestDto
{
    [Required]
    public DateOnly BillingCycleStart { get; set; }

    [Required]
    public DateOnly BillingCycleEnd { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Total amount must be greater than or equal to 0.")]
    public decimal TotalAmount { get; set; }

    // Optional: If not provided, minimum due will be calculated as 5% of total amount
    public decimal? MinimumDue { get; set; }

    // Optional: If not provided, due date will default to cycle end date + 18 days
    public DateOnly? DueDate { get; set; }
}
