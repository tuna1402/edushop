using Microsoft.Data.Sqlite;

namespace EduShop.Core.Infrastructure;

public static class DatabaseInitializer
{
    public static void EnsureCreated(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Product (
                product_id       INTEGER PRIMARY KEY AUTOINCREMENT,
                product_code     TEXT    NOT NULL UNIQUE,
                product_name     TEXT    NOT NULL,
                plan_name        TEXT    NULL,
                duration_months  INTEGER NOT NULL DEFAULT 1,
                purchase_price_usd REAL  NULL,
                purchase_price_krw INTEGER NULL,
                sale_price_krw   INTEGER NOT NULL DEFAULT 0,
                monthly_fee_usd  REAL    NULL,
                monthly_fee_krw  INTEGER NOT NULL,
                wholesale_price  INTEGER NOT NULL,
                retail_price     INTEGER NOT NULL,
                purchase_price   INTEGER NOT NULL,
                yearly_available TEXT    NOT NULL,
                min_month        INTEGER NOT NULL,
                max_month        INTEGER NOT NULL,
                status           TEXT    NOT NULL,
                remark           TEXT    NULL,
                created_at       TEXT    DEFAULT (datetime('now')),
                updated_at       TEXT    DEFAULT (datetime('now')),
                created_by       TEXT    NULL,
                updated_by       TEXT    NULL
            );

            CREATE TABLE IF NOT EXISTS AuditLog (
                log_id      INTEGER PRIMARY KEY AUTOINCREMENT,
                event_time  TEXT    NOT NULL,
                user_id     TEXT    NULL,
                user_name   TEXT    NULL,
                action_type TEXT    NOT NULL,
                table_name  TEXT    NOT NULL,
                target_id   INTEGER NULL,
                target_code TEXT    NULL,
                description TEXT    NOT NULL,
                detail_json TEXT    NULL
            );
            ";
        cmd.ExecuteNonQuery();

        // ── 매출 헤더 테이블 ──
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS SaleHeader (
                sale_id       INTEGER PRIMARY KEY AUTOINCREMENT,
                sale_date     TEXT    NOT NULL,
                customer_name TEXT    NULL,
                school_name   TEXT    NULL,
                contact       TEXT    NULL,
                memo          TEXT    NULL,
                total_amount  INTEGER NOT NULL DEFAULT 0,
                total_profit  INTEGER NOT NULL DEFAULT 0,
                created_at    TEXT    NOT NULL DEFAULT (datetime('now')),
                created_by    TEXT    NULL
            );
            ";
        cmd.ExecuteNonQuery();

        // ── 매출 항목 테이블 ──
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS SaleItem (
                sale_item_id  INTEGER PRIMARY KEY AUTOINCREMENT,
                sale_id       INTEGER NOT NULL,
                product_id    INTEGER NULL,
                product_code  TEXT    NOT NULL,
                product_name  TEXT    NOT NULL,
                unit_price    INTEGER NOT NULL,
                quantity      INTEGER NOT NULL,
                line_total    INTEGER NOT NULL,
                line_profit   INTEGER NOT NULL,
                FOREIGN KEY (sale_id) REFERENCES SaleHeader(sale_id)
            );
            ";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Account (
                account_id              INTEGER PRIMARY KEY AUTOINCREMENT,
                email                   TEXT    NOT NULL UNIQUE,
                product_id              INTEGER NOT NULL,
                subscription_start_date TEXT    NOT NULL,
                subscription_end_date   TEXT    NOT NULL,
                status                  TEXT    NOT NULL,
                customer_id             INTEGER NULL,
                order_id                INTEGER NULL,
                card_id                 INTEGER NULL,
                delivery_date           TEXT    NULL,
                last_payment_date       TEXT    NULL,
                memo                    TEXT    NULL,
                is_deleted              INTEGER NOT NULL DEFAULT 0,
                created_at              TEXT    NOT NULL DEFAULT (datetime('now')),
                created_by              TEXT    NULL,
                updated_at              TEXT    NULL,
                updated_by              TEXT    NULL
            );
            ";
        cmd.ExecuteNonQuery();

        EnsureAccountColumn(conn, "card_id", "INTEGER NULL");

        // ─────────────────────────────────────────────────────────────
        // AccountUsageLog (계정 사용 로그)
        // ─────────────────────────────────────────────────────────────
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS AccountUsageLog (
                log_id      INTEGER PRIMARY KEY AUTOINCREMENT,
                account_id  INTEGER NOT NULL,
                customer_id INTEGER NULL,
                product_id  INTEGER NULL,
                action_type TEXT    NOT NULL,
                request_date TEXT   NULL,
                expire_date  TEXT   NULL,
                description  TEXT   NULL,
                created_at   TEXT   NOT NULL DEFAULT (datetime('now')),
                created_by   TEXT   NULL,
                FOREIGN KEY (account_id) REFERENCES Account(account_id)
            );
            ";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Customer (
                customer_id   INTEGER PRIMARY KEY AUTOINCREMENT,
                school_name   TEXT    NOT NULL,
                contact_name  TEXT    NULL,
                phone1        TEXT    NULL,
                phone2        TEXT    NULL,
                email1        TEXT    NULL,
                email2        TEXT    NULL,
                address       TEXT    NULL,
                memo          TEXT    NULL,
                is_deleted    INTEGER NOT NULL DEFAULT 0,
                created_at    TEXT    NOT NULL DEFAULT (datetime('now')),
                created_by    TEXT    NULL,
                updated_at    TEXT    NULL,
                updated_by    TEXT    NULL
            );
            ";
            cmd.ExecuteNonQuery();

        EnsureProductColumn(conn, "duration_months", "INTEGER NOT NULL DEFAULT 1");
        EnsureProductColumn(conn, "purchase_price_usd", "REAL NULL");
        EnsureProductColumn(conn, "purchase_price_krw", "INTEGER NULL");
        EnsureProductColumn(conn, "sale_price_krw", "INTEGER NOT NULL DEFAULT 0");

        EnsureCustomerColumn(conn, "customer_name", "school_name");
        EnsureCustomerColumn(conn, "phone", "phone1");
        EnsureCustomerColumn(conn, "email", "email1");
        EnsureTableColumn(conn, "Customer", "phone2", "TEXT NULL");
        EnsureTableColumn(conn, "Customer", "email2", "TEXT NULL");
        EnsureTableColumn(conn, "Customer", "address", "TEXT NULL");

        using var statusUpdate = conn.CreateCommand();
        statusUpdate.CommandText = "UPDATE Product SET status = 'INACTIVE' WHERE status = 'STOPPED';";
        statusUpdate.ExecuteNonQuery();
    }

    private static void EnsureAccountColumn(SqliteConnection conn, string columnName, string columnDefinition)
    {
        using var check = conn.CreateCommand();
        check.CommandText = "PRAGMA table_info(Account);";
        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE Account ADD COLUMN {columnName} {columnDefinition};";
        alter.ExecuteNonQuery();
    }

    private static bool ColumnExists(SqliteConnection conn, string tableName, string columnName)
    {
        using var check = conn.CreateCommand();
        check.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureTableColumn(SqliteConnection conn, string tableName, string columnName, string columnDefinition)
    {
        if (ColumnExists(conn, tableName, columnName))
        {
            return;
        }

        using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        alter.ExecuteNonQuery();
    }

    private static void EnsureProductColumn(SqliteConnection conn, string columnName, string columnDefinition)
    {
        EnsureTableColumn(conn, "Product", columnName, columnDefinition);
    }

    private static void EnsureCustomerColumn(SqliteConnection conn, string oldColumnName, string newColumnName)
    {
        if (ColumnExists(conn, "Customer", newColumnName))
        {
            return;
        }

        if (!ColumnExists(conn, "Customer", oldColumnName))
        {
            return;
        }

        using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE Customer RENAME COLUMN {oldColumnName} TO {newColumnName};";
        alter.ExecuteNonQuery();
    }
}
