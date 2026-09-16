using MenuOrdering.API.Enums;

namespace MenuOrdering.API.DTOs.Orders;

public class OrderResponse
{
    public int Id { get; set; }
    public int TableId { get; set; }
    public string TableNumber { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public List<OrderItemResponse> Items { get; set; } = new();
}