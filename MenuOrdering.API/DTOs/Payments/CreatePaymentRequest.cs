using System.ComponentModel.DataAnnotations;

namespace MenuOrdering.API.DTOs.Payments;

public class CreatePaymentRequest
{
    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = null!;
}