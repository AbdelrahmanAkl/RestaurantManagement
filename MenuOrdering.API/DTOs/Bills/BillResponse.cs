namespace MenuOrdering.API.DTOs.Bills;

public class BillResponse
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int TableId { get; set; }
    public string TableNumber { get; set; } = null!;

    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal Total { get; set; }

    public DateTime RequestedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}