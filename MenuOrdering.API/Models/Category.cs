namespace MenuOrdering.API.Models;

public class Category
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public Restaurant Restaurant { get; set; } = null!;

    public ICollection<MenuItem> MenuItems { get; set; } =
        new List<MenuItem>();
}