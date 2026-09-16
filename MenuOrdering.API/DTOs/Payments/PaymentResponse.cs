using MenuOrdering.API.Enums;

namespace MenuOrdering.API.DTOs.Payments;

public class PaymentResponse
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int BillId { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public string? PaymentMethod { get; set; }

    public string? TransactionReference { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }
}