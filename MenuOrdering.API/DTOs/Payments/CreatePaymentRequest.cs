using System.ComponentModel.DataAnnotations;

namespace MenuOrdering.API.DTOs.Payments;

public class CreatePaymentRequest
{
    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = null!;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}