using MenuOrdering.API.Enums;

namespace MenuOrdering.API.Models;

public class Order
{
    public int Id { get; set; }

    public int TableId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal SubTotal { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public RestaurantTable Table { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public Bill? Bill { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}