namespace EduShop.Core.Models;

public class SaleHeader
{
    public long   SaleId       { get; set; }
    public DateTime SaleDate   { get; set; }

    public string? CustomerName { get; set; }
    public string? SchoolName   { get; set; }
    public string? Contact      { get; set; }
    public string? Memo         { get; set; }

    public long   TotalAmount   { get; set; }
    public long   TotalProfit   { get; set; }

    public DateTime CreatedAt   { get; set; }
    public string?  CreatedBy   { get; set; }
}
