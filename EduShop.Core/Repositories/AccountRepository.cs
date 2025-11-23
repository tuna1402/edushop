using System.Globalization;
using Microsoft.Data.Sqlite;
using EduShop.Core.Models;

namespace EduShop.Core.Repositories;

public class AccountRepository
{
    private readonly string _connectionString;

    public AccountRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    // 날짜 TEXT <-> DateTime 변환 도우미
    private static string ToDate(DateTime dt) =>
        dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTime ParseDate(string s) =>
        DateTime.Parse(s, CultureInfo.InvariantCulture);

    private static DateTime? ParseNullableDate(SqliteDataReader r, int index)
        => r.IsDBNull(index) ? null : ParseDate(r.GetString(index));

    public List<Account> GetAll()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT account_id,
       email,
       product_id,
       subscription_start_date,
       subscription_end_date,
       status,
       customer_id,
       order_id,
       delivery_date,
       last_payment_date,
       memo,
       is_deleted,
       created_at,
       created_by,
       updated_at,
       updated_by
FROM   Account
WHERE  is_deleted = 0
ORDER BY subscription_end_date ASC, account_id ASC;
";

        using var reader = cmd.ExecuteReader();
        var list = new List<Account>();

        while (reader.Read())
        {
            var acc = new Account
            {
                AccountId             = reader.GetInt64(0),
                Email                 = reader.GetString(1),
                ProductId             = reader.GetInt64(2),
                SubscriptionStartDate = ParseDate(reader.GetString(3)),
                SubscriptionEndDate   = ParseDate(reader.GetString(4)),
                Status                = reader.GetString(5),
                CustomerId            = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                OrderId               = reader.IsDBNull(7) ? null : reader.GetInt64(7),
                DeliveryDate          = ParseNullableDate(reader, 8),
                LastPaymentDate       = ParseNullableDate(reader, 9),
                Memo                  = reader.IsDBNull(10) ? null : reader.GetString(10),
                IsDeleted             = reader.GetInt32(11) != 0,
                CreatedAt             = DateTime.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
                CreatedBy             = reader.IsDBNull(13) ? null : reader.GetString(13),
                UpdatedAt             = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14), CultureInfo.InvariantCulture),
                UpdatedBy             = reader.IsDBNull(15) ? null : reader.GetString(15)
            };

            list.Add(acc);
        }

        return list;
    }

    public List<Account> GetByOrderId(long orderId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT account_id,
       email,
       product_id,
       subscription_start_date,
       subscription_end_date,
       status,
       customer_id,
       order_id,
       delivery_date,
       last_payment_date,
       memo,
       is_deleted,
       created_at,
       created_by,
       updated_at,
       updated_by
FROM   Account
WHERE  is_deleted = 0
  AND  order_id = $orderId
ORDER BY subscription_end_date ASC, account_id ASC;
";
        cmd.Parameters.AddWithValue("$orderId", orderId);

        using var reader = cmd.ExecuteReader();
        var list = new List<Account>();

        while (reader.Read())
        {
            var acc = new Account
            {
                AccountId             = reader.GetInt64(0),
                Email                 = reader.GetString(1),
                ProductId             = reader.GetInt64(2),
                SubscriptionStartDate = ParseDate(reader.GetString(3)),
                SubscriptionEndDate   = ParseDate(reader.GetString(4)),
                Status                = reader.GetString(5),
                CustomerId            = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                OrderId               = reader.IsDBNull(7) ? null : reader.GetInt64(7),
                DeliveryDate          = ParseNullableDate(reader, 8),
                LastPaymentDate       = ParseNullableDate(reader, 9),
                Memo                  = reader.IsDBNull(10) ? null : reader.GetString(10),
                IsDeleted             = reader.GetInt32(11) != 0,
                CreatedAt             = DateTime.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
                CreatedBy             = reader.IsDBNull(13) ? null : reader.GetString(13),
                UpdatedAt             = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14), CultureInfo.InvariantCulture),
                UpdatedBy             = reader.IsDBNull(15) ? null : reader.GetString(15)
            };

            list.Add(acc);
        }

        return list;
    }

    public Account? GetById(long accountId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT account_id,
       email,
       product_id,
       subscription_start_date,
       subscription_end_date,
       status,
       customer_id,
       order_id,
       delivery_date,
       last_payment_date,
       memo,
       is_deleted,
       created_at,
       created_by,
       updated_at,
       updated_by
FROM   Account
WHERE  account_id = $id;
";
        cmd.Parameters.AddWithValue("$id", accountId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new Account
        {
            AccountId             = reader.GetInt64(0),
            Email                 = reader.GetString(1),
            ProductId             = reader.GetInt64(2),
            SubscriptionStartDate = ParseDate(reader.GetString(3)),
            SubscriptionEndDate   = ParseDate(reader.GetString(4)),
            Status                = reader.GetString(5),
            CustomerId            = reader.IsDBNull(6) ? null : reader.GetInt64(6),
            OrderId               = reader.IsDBNull(7) ? null : reader.GetInt64(7),
            DeliveryDate          = ParseNullableDate(reader, 8),
            LastPaymentDate       = ParseNullableDate(reader, 9),
            Memo                  = reader.IsDBNull(10) ? null : reader.GetString(10),
            IsDeleted             = reader.GetInt32(11) != 0,
            CreatedAt             = DateTime.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
            CreatedBy             = reader.IsDBNull(13) ? null : reader.GetString(13),
            UpdatedAt             = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14), CultureInfo.InvariantCulture),
            UpdatedBy             = reader.IsDBNull(15) ? null : reader.GetString(15)
        };
    }

    public Account? GetByEmail(string email)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT account_id,
       email,
       product_id,
       subscription_start_date,
       subscription_end_date,
       status,
       customer_id,
       order_id,
       delivery_date,
       last_payment_date,
       memo,
       is_deleted,
       created_at,
       created_by,
       updated_at,
       updated_by
FROM   Account
WHERE  email = $email;
";
        cmd.Parameters.AddWithValue("$email", email);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new Account
        {
            AccountId             = reader.GetInt64(0),
            Email                 = reader.GetString(1),
            ProductId             = reader.GetInt64(2),
            SubscriptionStartDate = ParseDate(reader.GetString(3)),
            SubscriptionEndDate   = ParseDate(reader.GetString(4)),
            Status                = reader.GetString(5),
            CustomerId            = reader.IsDBNull(6) ? null : reader.GetInt64(6),
            OrderId               = reader.IsDBNull(7) ? null : reader.GetInt64(7),
            DeliveryDate          = ParseNullableDate(reader, 8),
            LastPaymentDate       = ParseNullableDate(reader, 9),
            Memo                  = reader.IsDBNull(10) ? null : reader.GetString(10),
            IsDeleted             = reader.GetInt32(11) != 0,
            CreatedAt             = DateTime.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
            CreatedBy             = reader.IsDBNull(13) ? null : reader.GetString(13),
            UpdatedAt             = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14), CultureInfo.InvariantCulture),
            UpdatedBy             = reader.IsDBNull(15) ? null : reader.GetString(15)
        };
    }

    public long Insert(Account acc, string userName)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Account
    (email,
     product_id,
     subscription_start_date,
     subscription_end_date,
     status,
     customer_id,
     order_id,
     delivery_date,
     last_payment_date,
     memo,
     is_deleted,
     created_at,
     created_by)
VALUES
    ($email,
     $productId,
     $start,
     $end,
     $status,
     $customerId,
     $orderId,
     $delivery,
     $lastPayment,
     $memo,
     0,
     datetime('now'),
     $user);

SELECT last_insert_rowid();
";

        cmd.Parameters.AddWithValue("$email", acc.Email);
        cmd.Parameters.AddWithValue("$productId", acc.ProductId);
        cmd.Parameters.AddWithValue("$start", ToDate(acc.SubscriptionStartDate));
        cmd.Parameters.AddWithValue("$end",   ToDate(acc.SubscriptionEndDate));
        cmd.Parameters.AddWithValue("$status", acc.Status);

        cmd.Parameters.AddWithValue("$customerId", (object?)acc.CustomerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$orderId",    (object?)acc.OrderId    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$delivery",
            acc.DeliveryDate.HasValue ? ToDate(acc.DeliveryDate.Value) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$lastPayment",
            acc.LastPaymentDate.HasValue ? ToDate(acc.LastPaymentDate.Value) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$memo", (object?)acc.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user", userName);

        var idObj = cmd.ExecuteScalar();
        return Convert.ToInt64(idObj);
    }

    public void Update(Account acc, string userName)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE Account
SET email                   = $email,
    product_id              = $productId,
    subscription_start_date = $start,
    subscription_end_date   = $end,
    status                  = $status,
    customer_id             = $customerId,
    order_id                = $orderId,
    delivery_date           = $delivery,
    last_payment_date       = $lastPayment,
    memo                    = $memo,
    updated_at              = datetime('now'),
    updated_by              = $user
WHERE account_id = $id;
";

        cmd.Parameters.AddWithValue("$id", acc.AccountId);
        cmd.Parameters.AddWithValue("$email", acc.Email);
        cmd.Parameters.AddWithValue("$productId", acc.ProductId);
        cmd.Parameters.AddWithValue("$start", ToDate(acc.SubscriptionStartDate));
        cmd.Parameters.AddWithValue("$end",   ToDate(acc.SubscriptionEndDate));
        cmd.Parameters.AddWithValue("$status", acc.Status);

        cmd.Parameters.AddWithValue("$customerId", (object?)acc.CustomerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$orderId",    (object?)acc.OrderId    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$delivery",
            acc.DeliveryDate.HasValue ? ToDate(acc.DeliveryDate.Value) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$lastPayment",
            acc.LastPaymentDate.HasValue ? ToDate(acc.LastPaymentDate.Value) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$memo", (object?)acc.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user", userName);

        cmd.ExecuteNonQuery();
    }

    public void SoftDelete(long accountId, string userName)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE Account
SET is_deleted = 1,
    updated_at = datetime('now'),
    updated_by = $user
WHERE account_id = $id;
";
        cmd.Parameters.AddWithValue("$id", accountId);
        cmd.Parameters.AddWithValue("$user", userName);
        cmd.ExecuteNonQuery();
    }

    // 만료 예정 계정 조회용 (예: 오늘부터 n일 이내)
    public List<Account> GetExpiring(DateTime referenceDate, int days)
    {
        var until = referenceDate.Date.AddDays(days);

        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
SELECT account_id,
       email,
       product_id,
       subscription_start_date,
       subscription_end_date,
       status,
       customer_id,
       order_id,
       delivery_date,
       last_payment_date,
       memo,
       is_deleted,
       created_at,
       created_by,
       updated_at,
       updated_by
FROM   Account
WHERE  is_deleted = 0
  AND  subscription_end_date <= $endDate
  AND  status IN ('SUBS_ACTIVE','DELIVERED','IN_USE','EXPIRING')
ORDER BY subscription_end_date ASC, account_id ASC;
";
        cmd.Parameters.AddWithValue("$endDate", ToDate(until));

        using var reader = cmd.ExecuteReader();
        var list = new List<Account>();

        while (reader.Read())
        {
            var acc = new Account
            {
                AccountId             = reader.GetInt64(0),
                Email                 = reader.GetString(1),
                ProductId             = reader.GetInt64(2),
                SubscriptionStartDate = ParseDate(reader.GetString(3)),
                SubscriptionEndDate   = ParseDate(reader.GetString(4)),
                Status                = reader.GetString(5),
                CustomerId            = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                OrderId               = reader.IsDBNull(7) ? null : reader.GetInt64(7),
                DeliveryDate          = ParseNullableDate(reader, 8),
                LastPaymentDate       = ParseNullableDate(reader, 9),
                Memo                  = reader.IsDBNull(10) ? null : reader.GetString(10),
                IsDeleted             = reader.GetInt32(11) != 0,
                CreatedAt             = DateTime.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
                CreatedBy             = reader.IsDBNull(13) ? null : reader.GetString(13),
                UpdatedAt             = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14), CultureInfo.InvariantCulture),
                UpdatedBy             = reader.IsDBNull(15) ? null : reader.GetString(15)
            };

            list.Add(acc);
        }

        return list;
    }
}
