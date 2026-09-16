using System.ComponentModel.DataAnnotations;
using MenuOrdering.API.Enums;

namespace MenuOrdering.API.DTOs.Payments;

public class UpdatePaymentStatusRequest
{
    public PaymentStatus Status { get; set; }

    [MaxLength(200)]
    public string? TransactionReference { get; set; }
}