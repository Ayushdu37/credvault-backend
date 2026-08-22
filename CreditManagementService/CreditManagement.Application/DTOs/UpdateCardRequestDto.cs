using System.ComponentModel.DataAnnotations;

namespace CreditManagement.Application.DTOs;

// Request model for updating editable card properties
public class UpdateCardRequestDto
{
    [Required]
    [StringLength(150)]
    public string CardHolderName { get; set; } = string.Empty;

    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Credit limit must be greater than 0.")]
    public decimal CreditLimit { get; set; }
}
