namespace EduShop.Core.Models;

public class AccountUsageLog
{
    public long LogId     { get; set; }
    public long AccountId { get; set; }

    public long? CustomerId { get; set; }
    public long? ProductId  { get; set; }

    public string ActionType { get; set; } = "";

    public DateTime? RequestDate { get; set; }
    public DateTime? ExpireDate  { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public string?  CreatedBy { get; set; }
}
