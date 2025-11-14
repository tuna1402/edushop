namespace EduShop.Core.Models;

public class AuditLogEntry
{
    public long LogId { get; set; }
    public DateTime EventTime { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string ActionType { get; set; } = "";
    public string TableName { get; set; } = "";
    public long? TargetId { get; set; }
    public string? TargetCode { get; set; }
    public string Description { get; set; } = "";
    public string? DetailJson { get; set; }    // ← 요거 추가
}
