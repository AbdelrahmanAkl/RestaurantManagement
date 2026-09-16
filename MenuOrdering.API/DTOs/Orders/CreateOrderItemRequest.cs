using System.ComponentModel.DataAnnotations;

namespace MenuOrdering.API.DTOs.Orders;

public class CreateOrderItemRequest
{
    [Required]
    public int MenuItemId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}