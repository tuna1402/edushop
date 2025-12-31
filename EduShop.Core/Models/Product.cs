namespace EduShop.Core.Models;

public class Product
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string? PlanName { get; set; }
    public int DurationMonths { get; set; }
    public double? PurchasePriceUsd { get; set; }
    public long? PurchasePriceKrw { get; set; }
    public long SalePriceKrw { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE / INACTIVE
    public string? Remark { get; set; }
    public long Profit => SalePriceKrw - (PurchasePriceKrw ?? 0);
    public double? ProfitRate => PurchasePriceKrw.HasValue && PurchasePriceKrw.Value > 0
        ? (double)(SalePriceKrw - PurchasePriceKrw.Value) / PurchasePriceKrw.Value
        : null;
}
