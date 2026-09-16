namespace MenuOrdering.API.Models;

public class RestaurantTable
{
    public int Id { get; set; }

    public string TableNumber { get; set; } = null!;

    public string? QRCode { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}