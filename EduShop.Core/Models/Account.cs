namespace EduShop.Core.Models;

public class Account
{
    public long AccountId { get; set; }

    public string Email { get; set; } = "";

    public long ProductId { get; set; }

    public DateTime SubscriptionStartDate { get; set; }
    public DateTime SubscriptionEndDate   { get; set; }

    public string Status { get; set; } = AccountStatus.SubsActive;

    public long? CustomerId { get; set; }
    public long? OrderId    { get; set; }

    public DateTime? DeliveryDate    { get; set; }
    public DateTime? LastPaymentDate { get; set; }

    public string? Memo { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }
    public string?  CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string?   UpdatedBy { get; set; }

    // 편의 프로퍼티들 (필요하면 WinForms에서 사용)
    public bool IsActive =>
        !IsDeleted && Status != AccountStatus.Canceled;

    public bool IsReusable =>
        !IsDeleted && Status == AccountStatus.ResetReady;
}
