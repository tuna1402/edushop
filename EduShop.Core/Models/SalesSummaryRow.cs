namespace EduShop.Core.Models;

public class SalesSummaryRow
{
    public DateTime Date { get; set; }
    public long? ProductId { get; set; }
    public string Customer { get; set; } = "";
    public string Product { get; set; } = "";
    public long Qty { get; set; }
    public long SalesAmt { get; set; }
    public long CostAmt { get; set; }
    public long ProfitAmt { get; set; }
}
