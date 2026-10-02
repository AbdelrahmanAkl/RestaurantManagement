using System.ComponentModel.DataAnnotations;

namespace MenuOrdering.API.DTOs.Tables;

public class CreateTableRequest
{
    [Required]
    public int BranchId { get; set; }

    [Required]
    [MaxLength(50)]
    public string TableNumber { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
