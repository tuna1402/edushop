namespace EduShop.Core.Models;

public static class AccountActionType
{
    public const string Create        = "생성";
    public const string Deliver       = "발송";
    public const string Cancel        = "취소";
    public const string Renew         = "재생성";
    public const string Reuse         = "재사용";
    public const string StatusChange  = "상태 변경";
    public const string PasswordReset = "비밀번호 변경";
}
