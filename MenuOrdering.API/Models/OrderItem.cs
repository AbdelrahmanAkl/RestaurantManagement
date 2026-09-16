namespace MenuOrdering.API.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int MenuItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;

    public MenuItem MenuItem { get; set; } = null!;
}