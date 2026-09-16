namespace MenuOrdering.API.Models;

public class Bill
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Tax { get; set; }

    public decimal ServiceCharge { get; set; }

    public decimal Total { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public Order Order { get; set; } = null!;
}