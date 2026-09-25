using System.ComponentModel.DataAnnotations;

namespace MenuOrdering.API.DTOs.Branches;

public class UpdateBranchRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; }
}