using Microsoft.Data.Sqlite;
using System.Globalization;
using EduShop.Core.Models;

namespace EduShop.Core.Repositories;

public class AuditLogRepository
{
    private readonly string _connectionString;

    public AuditLogRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public void Insert(AuditLogEntry entry)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO AuditLog (
                event_time, user_id, user_name,
                action_type, table_name, target_id, target_code, description, detail_json
            )
            VALUES (
                datetime('now'), $userId, $userName,
                $actionType, $tableName, $targetId, $targetCode, $description, $detailJson
            );
        ";

        cmd.Parameters.AddWithValue("$userId", (object?)entry.UserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$userName", (object?)entry.UserName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$actionType", entry.ActionType);
        cmd.Parameters.AddWithValue("$tableName", entry.TableName);
        cmd.Parameters.AddWithValue("$targetId", (object?)entry.TargetId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$targetCode", (object?)entry.TargetCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$description", entry.Description);
        cmd.Parameters.AddWithValue("$detailJson", (object?)entry.DetailJson ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public List<AuditLogEntry> GetByDateRange(DateTime from, DateTime to)
    {
        var list = new List<AuditLogEntry>();

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT log_id, event_time, user_id, user_name,
                   action_type, table_name, target_id, target_code, description, detail_json
            FROM AuditLog
            WHERE event_time BETWEEN $from AND $to
            ORDER BY event_time DESC;
        ";

        var fromText = from.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var toText   = to.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        cmd.Parameters.AddWithValue("$from", fromText);
        cmd.Parameters.AddWithValue("$to",   toText);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var log = new AuditLogEntry
            {
                LogId      = reader.GetInt64(0),
                EventTime  = DateTime.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                UserId     = reader.IsDBNull(2) ? null : reader.GetString(2),
                UserName   = reader.IsDBNull(3) ? null : reader.GetString(3),
                ActionType = reader.GetString(4),
                TableName  = reader.GetString(5),
                TargetId   = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                TargetCode = reader.IsDBNull(7) ? null : reader.GetString(7),
                Description= reader.GetString(8),
                DetailJson = reader.IsDBNull(9) ? null : reader.GetString(9)
            };
            list.Add(log);
        }

        return list;
    }

    public List<AuditLogEntry> GetForProduct(long productId)
    {
        var list = new List<AuditLogEntry>();

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT log_id, event_time, user_id, user_name,
                   action_type, table_name, target_id, target_code, description, detail_json
            FROM AuditLog
            WHERE table_name = 'Product'
              AND target_id   = $id
            ORDER BY event_time DESC;
        ";
        cmd.Parameters.AddWithValue("$id", productId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var log = new AuditLogEntry
            {
                LogId      = reader.GetInt64(0),
                EventTime  = DateTime.Parse(reader.GetString(1)),
                UserId     = reader.IsDBNull(2) ? null : reader.GetString(2),
                UserName   = reader.IsDBNull(3) ? null : reader.GetString(3),
                ActionType = reader.GetString(4),
                TableName  = reader.GetString(5),
                TargetId   = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                TargetCode = reader.IsDBNull(7) ? null : reader.GetString(7),
                Description= reader.GetString(8),
                DetailJson = reader.IsDBNull(9) ? null : reader.GetString(9)
            };
            list.Add(log);
        }

        return list;
    }
}
