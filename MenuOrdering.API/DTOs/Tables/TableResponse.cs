namespace MenuOrdering.API.DTOs.Tables;

public class TableResponse
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string TableNumber { get; set; } = null!;
    public string? QRCode { get; set; }
    public bool IsActive { get; set; }
}
