namespace EduShop.Core.Models;

public class Card
{
    public long CardId { get; set; }

    public string CardName { get; set; } = "";
    public string? CardCompany { get; set; }
    public string? Last4Digits { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerType { get; set; }
    public int? BillingDay { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string? Memo { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
