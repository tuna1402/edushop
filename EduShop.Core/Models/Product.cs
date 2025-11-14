namespace EduShop.Core.Models;

public class Product
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string? PlanName { get; set; }
    public double? MonthlyFeeUsd { get; set; }
    public long MonthlyFeeKrw { get; set; }
    public long WholesalePrice { get; set; }   // 도매가
    public long RetailPrice { get; set; }      // 소매가
    public long PurchasePrice { get; set; }    // 매입가
    public bool YearlyAvailable { get; set; }  // 연 구독 가능 여부
    public int MinMonth { get; set; }
    public int MaxMonth { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE / INACTIVE
    public string? Remark { get; set; }
    public long Profit => RetailPrice - PurchasePrice;
    public double? ProfitRate => PurchasePrice > 0 ? (double)(RetailPrice - PurchasePrice) / PurchasePrice : null;
}
