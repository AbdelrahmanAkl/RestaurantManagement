namespace MenuOrdering.API.Models;

public class Restaurant
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}