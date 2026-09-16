using MenuOrdering.API.Enums;

namespace MenuOrdering.API.Models;

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? PaymentMethod { get; set; }

    public string? TransactionReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public Order Order { get; set; } = null!;
}