using System.ComponentModel.DataAnnotations;

namespace MenuOrdering.API.DTOs.Orders;

public class CreateOrderRequest
{
    [Required]
    public int TableId { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}