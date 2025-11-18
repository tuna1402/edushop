using System.Globalization;
using Microsoft.Data.Sqlite;
using EduShop.Core.Models;

namespace EduShop.Core.Repositories;

public class SalesRepository
{
    private readonly string _connectionString;

    public SalesRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    // 매출 등록 (헤더 + 아이템 일괄 저장, 트랜잭션)
    public long InsertSale(SaleHeader header, List<SaleItem> items, string userName)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        // totals 재계산
        long totalAmount = items.Sum(i => i.LineTotal);
        long totalProfit = items.Sum(i => i.LineProfit);

        header.TotalAmount = totalAmount;
        header.TotalProfit = totalProfit;

        // 헤더 저장
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
INSERT INTO SaleHeader
    (sale_date, customer_name, school_name, contact, memo,
     total_amount, total_profit, created_at, created_by)
VALUES
    ($date, $customer, $school, $contact, $memo,
     $totalAmount, $totalProfit, datetime('now'), $user);

SELECT last_insert_rowid();
";
            cmd.Parameters.AddWithValue("$date", header.SaleDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$customer", (object?)header.CustomerName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$school",   (object?)header.SchoolName   ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$contact",  (object?)header.Contact      ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$memo",     (object?)header.Memo         ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$totalAmount", totalAmount);
            cmd.Parameters.AddWithValue("$totalProfit", totalProfit);
            cmd.Parameters.AddWithValue("$user", userName);

            var idObj = cmd.ExecuteScalar();
            header.SaleId = Convert.ToInt64(idObj);
        }

        // 아이템 저장
        foreach (var item in items)
        {
            item.SaleId = header.SaleId;

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
INSERT INTO SaleItem
    (sale_id, product_id, product_code, product_name,
     unit_price, quantity, line_total, line_profit)
VALUES
    ($saleId, $productId, $code, $name,
     $unitPrice, $qty, $total, $profit);
";
            cmd.Parameters.AddWithValue("$saleId", header.SaleId);
            cmd.Parameters.AddWithValue("$productId", (object?)item.ProductId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$code", item.ProductCode);
            cmd.Parameters.AddWithValue("$name", item.ProductName);
            cmd.Parameters.AddWithValue("$unitPrice", item.UnitPrice);
            cmd.Parameters.AddWithValue("$qty", item.Quantity);
            cmd.Parameters.AddWithValue("$total", item.LineTotal);
            cmd.Parameters.AddWithValue("$profit", item.LineProfit);

            cmd.ExecuteNonQuery();
        }

        tx.Commit();
        return header.SaleId;
    }

    // 기간별 매출 헤더 조회
    public List<SaleHeader> GetSales(DateTime? from, DateTime? to)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();

        var conditions = new List<string>();
        if (from.HasValue)
        {
            conditions.Add("sale_date >= $from");
            cmd.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        if (to.HasValue)
        {
            conditions.Add("sale_date <= $to");
            cmd.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        var where = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        cmd.CommandText = $@"
SELECT sale_id, sale_date, customer_name, school_name, contact, memo,
       total_amount, total_profit, created_at, created_by
FROM   SaleHeader
{where}
ORDER BY sale_date DESC, sale_id DESC;
";

        using var reader = cmd.ExecuteReader();
        var list = new List<SaleHeader>();

        while (reader.Read())
        {
            var sale = new SaleHeader
            {
                SaleId       = reader.GetInt64(0),
                SaleDate     = DateTime.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                CustomerName = reader.IsDBNull(2) ? null : reader.GetString(2),
                SchoolName   = reader.IsDBNull(3) ? null : reader.GetString(3),
                Contact      = reader.IsDBNull(4) ? null : reader.GetString(4),
                Memo         = reader.IsDBNull(5) ? null : reader.GetString(5),
                TotalAmount  = reader.GetInt64(6),
                TotalProfit  = reader.GetInt64(7),
                CreatedAt    = DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
                CreatedBy    = reader.IsDBNull(9) ? null : reader.GetString(9)
            };
            list.Add(sale);
        }

        return list;
    }

    // 특정 매출의 항목 목록
    public List<SaleItem> GetSaleItems(long saleId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT sale_item_id, sale_id, product_id, product_code, product_name,
       unit_price, quantity, line_total, line_profit
FROM   SaleItem
WHERE  sale_id = $saleId
ORDER BY sale_item_id;
";
        cmd.Parameters.AddWithValue("$saleId", saleId);

        using var reader = cmd.ExecuteReader();
        var list = new List<SaleItem>();

        while (reader.Read())
        {
            var item = new SaleItem
            {
                SaleItemId  = reader.GetInt64(0),
                SaleId      = reader.GetInt64(1),
                ProductId   = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                ProductCode = reader.GetString(3),
                ProductName = reader.GetString(4),
                UnitPrice   = reader.GetInt64(5),
                Quantity    = reader.GetInt32(6),
                LineTotal   = reader.GetInt64(7),
                LineProfit  = reader.GetInt64(8)
            };
            list.Add(item);
        }

        return list;
    }

    // 기간별 합계(매출/마진)
    public SalesSummary GetSummary(DateTime? from, DateTime? to)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();

        var conditions = new List<string>();
        if (from.HasValue)
        {
            conditions.Add("sale_date >= $from");
            cmd.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        if (to.HasValue)
        {
            conditions.Add("sale_date <= $to");
            cmd.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        var where = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        cmd.CommandText = $@"
SELECT IFNULL(SUM(total_amount), 0),
       IFNULL(SUM(total_profit), 0)
FROM   SaleHeader
{where};
";

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new SalesSummary
            {
                TotalAmount = reader.GetInt64(0),
                TotalProfit = reader.GetInt64(1)
            };
        }

        return new SalesSummary();
    }
}
