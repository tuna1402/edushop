namespace EduShop.Core.Models;

public class SaleItem
{
    public long   SaleItemId   { get; set; }
    public long   SaleId       { get; set; }

    public long?  ProductId    { get; set; }
    public string ProductCode  { get; set; } = "";
    public string ProductName  { get; set; } = "";

    public long   UnitPrice    { get; set; }
    public int    Quantity     { get; set; }

    public long   LineTotal    { get; set; }
    public long   LineProfit   { get; set; }
}
