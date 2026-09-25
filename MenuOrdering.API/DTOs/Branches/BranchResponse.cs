namespace MenuOrdering.API.DTOs.Branches;

public class BranchResponse
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    public string RestaurantName { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; }

    public int TableCount { get; set; }
}