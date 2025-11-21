using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using EduShop.Core.Models;

namespace EduShop.Core.Repositories;

public class AccountUsageLogRepository
{
    private readonly string _connectionString;

    public AccountUsageLogRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private static string? ToDate(DateTime? dt) =>
        dt.HasValue ? dt.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;

    private static DateTime ParseDate(string s) =>
        DateTime.Parse(s, CultureInfo.InvariantCulture);

    private static DateTime? ParseNullableDate(SqliteDataReader r, int index)
        => r.IsDBNull(index) ? null : ParseDate(r.GetString(index));

    public void Insert(AccountUsageLog log, string userName)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO AccountUsageLog
    (account_id,
     customer_id,
     product_id,
     action_type,
     request_date,
     expire_date,
     description,
     created_at,
     created_by)
VALUES
    ($accountId,
     $customerId,
     $productId,
     $actionType,
     $requestDate,
     $expireDate,
     $description,
     datetime('now'),
     $user);
";

        cmd.Parameters.AddWithValue("$accountId", log.AccountId);
        cmd.Parameters.AddWithValue("$customerId", (object?)log.CustomerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$productId",  (object?)log.ProductId  ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$actionType", log.ActionType);
        cmd.Parameters.AddWithValue("$requestDate", (object?)ToDate(log.RequestDate) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$expireDate",  (object?)ToDate(log.ExpireDate)  ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$description", (object?)log.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user", userName);

        cmd.ExecuteNonQuery();
    }

    public List<AccountUsageLog> GetForAccount(
        long accountId,
        DateTime? from = null,
        DateTime? to = null,
        long? customerId = null,
        long? productId = null)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();

        var sb = new StringBuilder();
        sb.Append(@"
SELECT log_id,
       account_id,
       customer_id,
       product_id,
       action_type,
       request_date,
       expire_date,
       description,
       created_at,
       created_by
FROM   AccountUsageLog
WHERE  account_id = $accountId
");

        cmd.Parameters.AddWithValue("$accountId", accountId);

        if (from.HasValue)
        {
            sb.Append("  AND request_date >= $from\n");
            cmd.Parameters.AddWithValue("$from", ToDate(from));
        }

        if (to.HasValue)
        {
            sb.Append("  AND request_date <= $to\n");
            cmd.Parameters.AddWithValue("$to", ToDate(to));
        }

        if (customerId.HasValue)
        {
            sb.Append("  AND customer_id = $customerId\n");
            cmd.Parameters.AddWithValue("$customerId", customerId.Value);
        }

        if (productId.HasValue)
        {
            sb.Append("  AND product_id = $productId\n");
            cmd.Parameters.AddWithValue("$productId", productId.Value);
        }

        sb.Append("ORDER BY created_at DESC, log_id DESC;\n");

        cmd.CommandText = sb.ToString();

        using var reader = cmd.ExecuteReader();
        var list = new List<AccountUsageLog>();

        while (reader.Read())
        {
            var log = new AccountUsageLog
            {
                LogId      = reader.GetInt64(0),
                AccountId  = reader.GetInt64(1),
                CustomerId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                ProductId  = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                ActionType = reader.GetString(4),
                RequestDate = ParseNullableDate(reader, 5),
                ExpireDate  = ParseNullableDate(reader, 6),
                Description = reader.IsDBNull(7) ? null : reader.GetString(7),
                CreatedAt   = DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
                CreatedBy   = reader.IsDBNull(9) ? null : reader.GetString(9)
            };

            list.Add(log);
        }

        return list;
    }
}
