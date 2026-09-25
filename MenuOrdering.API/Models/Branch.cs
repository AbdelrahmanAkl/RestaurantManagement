namespace MenuOrdering.API.Models;

public class Branch
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public Restaurant Restaurant { get; set; } = null!;

    public ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<RestaurantTable> Tables { get; set; } =
        new List<RestaurantTable>();

    public ICollection<Order> Orders { get; set; } =
        new List<Order>();
}