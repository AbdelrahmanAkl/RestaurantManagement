using System.ComponentModel.DataAnnotations;
using MenuOrdering.API.Enums;

namespace MenuOrdering.API.DTOs.Users;

public class UpdateUserRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public int? RestaurantId { get; set; }

    public int? BranchId { get; set; }

    public bool IsActive { get; set; } = true;
}