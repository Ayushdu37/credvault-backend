using System.ComponentModel.DataAnnotations;

namespace CreditManagement.Application.DTOs;

// Request model for registering a new card
public class AddCardRequestDto
{
    [Required]
    [CreditCard(ErrorMessage = "Invalid credit card number format.")]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string CardHolderName { get; set; } = string.Empty;

    [Required]
    [Range(1, 12, ErrorMessage = "Expiry month must be between 1 and 12.")]
    public int ExpiryMonth { get; set; }

    [Required]
    [Range(2025, 2100, ErrorMessage = "Expiry year must be a valid future year.")]
    public int ExpiryYear { get; set; }

    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Credit limit must be greater than 0.")]
    public decimal CreditLimit { get; set; }
}
