using EduShop.Core.Models;
using Microsoft.Data.Sqlite;

namespace EduShop.Core.Repositories;

public class ProductRepository
{
    private readonly string _connectionString;

    public ProductRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public List<Product> GetAll()
    {
        var list = new List<Product>();

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT product_id, product_code, product_name,
                   plan_name, monthly_fee_usd, monthly_fee_krw,
                   wholesale_price, retail_price, purchase_price,
                   yearly_available, min_month, max_month, status, remark
            FROM Product
            ORDER BY created_at DESC;
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var p = new Product
            {
                ProductId       = reader.GetInt64(0),
                ProductCode     = reader.GetString(1),
                ProductName     = reader.GetString(2),
                PlanName        = reader.IsDBNull(3) ? null : reader.GetString(3),
                MonthlyFeeUsd   = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                MonthlyFeeKrw   = reader.GetInt64(5),
                WholesalePrice  = reader.GetInt64(6),
                RetailPrice     = reader.GetInt64(7),
                PurchasePrice   = reader.GetInt64(8),
                YearlyAvailable = reader.GetString(9) == "Y",
                MinMonth        = reader.GetInt32(10),
                MaxMonth        = reader.GetInt32(11),
                Status          = reader.GetString(12),
                Remark          = reader.IsDBNull(13) ? null : reader.GetString(13)
            };
            list.Add(p);
        }

        return list;
    }

    public long Insert(Product p, string userName)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Product (
                product_code, product_name, plan_name,
                monthly_fee_usd, monthly_fee_krw,
                wholesale_price, retail_price, purchase_price,
                yearly_available, min_month, max_month,
                status, remark, created_by, updated_by
            )
            VALUES (
                $code, $name, $plan,
                $usd, $krw,
                $wholesale, $retail, $purchase,
                $yearly, $minMonth, $maxMonth,
                $status, $remark, $user, $user
            );
            SELECT last_insert_rowid();
        ";

        cmd.Parameters.AddWithValue("$code", p.ProductCode);
        cmd.Parameters.AddWithValue("$name", p.ProductName);
        cmd.Parameters.AddWithValue("$plan", (object?)p.PlanName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$usd", (object?)p.MonthlyFeeUsd ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$krw", p.MonthlyFeeKrw);
        cmd.Parameters.AddWithValue("$wholesale", p.WholesalePrice);
        cmd.Parameters.AddWithValue("$retail", p.RetailPrice);
        cmd.Parameters.AddWithValue("$purchase", p.PurchasePrice);
        cmd.Parameters.AddWithValue("$yearly", p.YearlyAvailable ? "Y" : "N");
        cmd.Parameters.AddWithValue("$minMonth", p.MinMonth);
        cmd.Parameters.AddWithValue("$maxMonth", p.MaxMonth);
        cmd.Parameters.AddWithValue("$status", p.Status);
        cmd.Parameters.AddWithValue("$remark", (object?)p.Remark ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user", userName);

        var result = cmd.ExecuteScalar();
        return (long)(result ?? 0);
    }

    public void UpdateStatus(long productId, string newStatus, string userName)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Product
            SET status     = $status,
                updated_at = datetime('now'),
                updated_by = $user
            WHERE product_id = $id;
        ";
        cmd.Parameters.AddWithValue("$status", newStatus);
        cmd.Parameters.AddWithValue("$user", userName);
        cmd.Parameters.AddWithValue("$id", productId);
        cmd.ExecuteNonQuery();
    }

    public Product? GetById(long productId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT product_id, product_code, product_name,
                   plan_name, monthly_fee_usd, monthly_fee_krw,
                   wholesale_price, retail_price, purchase_price,
                   yearly_available, min_month, max_month, status, remark
            FROM Product
            WHERE product_id = $id;
        ";
        cmd.Parameters.AddWithValue("$id", productId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new Product
        {
            ProductId       = reader.GetInt64(0),
            ProductCode     = reader.GetString(1),
            ProductName     = reader.GetString(2),
            PlanName        = reader.IsDBNull(3) ? null : reader.GetString(3),
            MonthlyFeeUsd   = reader.IsDBNull(4) ? null : reader.GetDouble(4),
            MonthlyFeeKrw   = reader.GetInt64(5),
            WholesalePrice  = reader.GetInt64(6),
            RetailPrice     = reader.GetInt64(7),
            PurchasePrice   = reader.GetInt64(8),
            YearlyAvailable = reader.GetString(9) == "Y",
            MinMonth        = reader.GetInt32(10),
            MaxMonth        = reader.GetInt32(11),
            Status          = reader.GetString(12),
            Remark          = reader.IsDBNull(13) ? null : reader.GetString(13)
        };
    }
    public Product? GetByCode(string productCode)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT product_id, product_code, product_name,
                plan_name, monthly_fee_usd, monthly_fee_krw,
                wholesale_price, retail_price, purchase_price,
                yearly_available, min_month, max_month, status, remark
            FROM Product
            WHERE product_code = $code;
        ";
        cmd.Parameters.AddWithValue("$code", productCode);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new Product
        {
            ProductId       = reader.GetInt64(0),
            ProductCode     = reader.GetString(1),
            ProductName     = reader.GetString(2),
            PlanName        = reader.IsDBNull(3) ? null : reader.GetString(3),
            MonthlyFeeUsd   = reader.IsDBNull(4) ? null : reader.GetDouble(4),
            MonthlyFeeKrw   = reader.GetInt64(5),
            WholesalePrice  = reader.GetInt64(6),
            RetailPrice     = reader.GetInt64(7),
            PurchasePrice   = reader.GetInt64(8),
            YearlyAvailable = reader.GetString(9) == "Y",
            MinMonth        = reader.GetInt32(10),
            MaxMonth        = reader.GetInt32(11),
            Status          = reader.GetString(12),
            Remark          = reader.IsDBNull(13) ? null : reader.GetString(13)
        };
    }
        public void Update(Product p, string userName)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            UPDATE Product
            SET
                product_code     = $code,
                product_name     = $name,
                plan_name        = $plan,
                monthly_fee_usd  = $usd,
                monthly_fee_krw  = $krw,
                wholesale_price  = $wholesale,
                retail_price     = $retail,
                purchase_price   = $purchase,
                yearly_available = $yearly,
                min_month        = $minMonth,
                max_month        = $maxMonth,
                status           = $status,
                remark           = $remark,
                updated_at       = datetime('now'),
                updated_by       = $user
            WHERE product_id      = $id;
            ";

        cmd.Parameters.AddWithValue("$code",      p.ProductCode);
        cmd.Parameters.AddWithValue("$name",      p.ProductName);
        cmd.Parameters.AddWithValue("$plan",      (object?)p.PlanName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$usd",       (object?)p.MonthlyFeeUsd ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$krw",       p.MonthlyFeeKrw);
        cmd.Parameters.AddWithValue("$wholesale", p.WholesalePrice);
        cmd.Parameters.AddWithValue("$retail",    p.RetailPrice);
        cmd.Parameters.AddWithValue("$purchase",  p.PurchasePrice);
        cmd.Parameters.AddWithValue("$yearly",    p.YearlyAvailable ? "Y" : "N");
        cmd.Parameters.AddWithValue("$minMonth",  p.MinMonth);
        cmd.Parameters.AddWithValue("$maxMonth",  p.MaxMonth);
        cmd.Parameters.AddWithValue("$status",    p.Status);
        cmd.Parameters.AddWithValue("$remark",    (object?)p.Remark ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user",      userName);
        cmd.Parameters.AddWithValue("$id",        p.ProductId);

        cmd.ExecuteNonQuery();
    }
}
