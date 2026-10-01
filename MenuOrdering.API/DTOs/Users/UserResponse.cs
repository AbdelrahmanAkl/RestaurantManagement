namespace MenuOrdering.API.DTOs.Users;

public class UserResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? RestaurantId { get; set; }

    public string? RestaurantName { get; set; }

    public int? BranchId { get; set; }

    public string? BranchName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}