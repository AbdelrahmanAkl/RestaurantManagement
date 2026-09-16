using System.ComponentModel.DataAnnotations;

namespace MenuOrdering.API.DTOs.MenuItems;

public class CreateMenuItemRequest
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0.01, 999999999)]
    public decimal Price { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;
}