using System.ComponentModel.DataAnnotations;

namespace MenuOrdering.API.DTOs.Tables;

public class CreateTableRequest
{
    [Required]
    [MaxLength(50)]
    public string TableNumber { get; set; } = null!;

    [MaxLength(500)]
    public string? QRCode { get; set; }

    public bool IsActive { get; set; } = true;
}