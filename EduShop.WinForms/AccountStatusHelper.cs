using System;
using System.Collections.Generic;
using System.Linq;
using EduShop.Core.Models;

namespace EduShop.WinForms;

public static class AccountStatusHelper
{
    private static readonly Dictionary<string, string> Display = new(StringComparer.OrdinalIgnoreCase)
    {
        [AccountStatus.Created]    = "생성됨",
        [AccountStatus.SubsActive] = "구독 활성",
        [AccountStatus.Delivered]  = "납품 완료",
        [AccountStatus.InUse]      = "사용 중",
        [AccountStatus.Expiring]   = "만료 예정",
        [AccountStatus.Canceled]   = "구독 취소",
        [AccountStatus.ResetReady] = "재사용 준비"
    };

    public static string ToDisplay(string? statusCode)
    {
        if (string.IsNullOrWhiteSpace(statusCode))
            return string.Empty;

        return Display.TryGetValue(statusCode, out var name)
            ? name
            : statusCode;
    }

    public static IEnumerable<(string Code, string Display)> GetAll()
        => Display.Select(kv => (kv.Key, kv.Value));

    public static IEnumerable<(string Code, string Display)> GetAllWithEmpty()
    {
        yield return ("", "");
        foreach (var item in Display)
        {
            yield return (item.Key, item.Value);
        }
    }
}
