namespace EduShop.Core.Models;

public static class AccountStatus
{
    public const string Created     = "생성";
    public const string SubsActive  = "구독중";
    public const string Delivered   = "발송완료";
    public const string InUse       = "사용중";
    public const string Expiring    = "만료";
    public const string Canceled    = "취소";
    public const string ResetReady  = "재사용 대기";
}
