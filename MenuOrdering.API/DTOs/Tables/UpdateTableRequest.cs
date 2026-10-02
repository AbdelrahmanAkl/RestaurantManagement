using System.ComponentModel.DataAnnotations;

namespace MenuOrdering.API.DTOs.Tables;

public class UpdateTableRequest
{
    [Required]
    [MaxLength(50)]
    public string TableNumber { get; set; } = null!;

    public bool IsActive { get; set; }
}
