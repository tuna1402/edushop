using System.Globalization;
using EduShop.Core.Models;
using Microsoft.Data.Sqlite;

namespace EduShop.Core.Repositories;

public class CardRepository
{
    private readonly string _connectionString;

    public CardRepository(string connectionString)
    {
        _connectionString = connectionString;
        EnsureTable();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void EnsureTable()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Card (
                card_id      INTEGER PRIMARY KEY AUTOINCREMENT,
                card_name    TEXT    NOT NULL,
                card_company TEXT    NULL,
                last4_digits TEXT    NULL,
                owner_name   TEXT    NULL,
                owner_type   TEXT    NULL,
                billing_day  INTEGER NULL,
                status       TEXT    NOT NULL,
                memo         TEXT    NULL,
                created_at   TEXT    NOT NULL,
                created_by   TEXT    NOT NULL,
                updated_at   TEXT    NULL,
                updated_by   TEXT    NULL
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public List<Card> GetAll()
    {
        var list = new List<Card>();

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT card_id,
                   card_name,
                   card_company,
                   last4_digits,
                   owner_name,
                   owner_type,
                   billing_day,
                   status,
                   memo,
                   created_at,
                   created_by,
                   updated_at,
                   updated_by
            FROM Card
            ORDER BY created_at DESC, card_id DESC;
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var card = new Card
            {
                CardId      = reader.GetInt64(0),
                CardName    = reader.GetString(1),
                CardCompany = reader.IsDBNull(2) ? null : reader.GetString(2),
                Last4Digits = reader.IsDBNull(3) ? null : reader.GetString(3),
                OwnerName   = reader.IsDBNull(4) ? null : reader.GetString(4),
                OwnerType   = reader.IsDBNull(5) ? null : reader.GetString(5),
                BillingDay  = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                Status      = reader.GetString(7),
                Memo        = reader.IsDBNull(8) ? null : reader.GetString(8),
                CreatedAt   = DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
                CreatedBy   = reader.GetString(10),
                UpdatedAt   = reader.IsDBNull(11) ? null : DateTime.Parse(reader.GetString(11), CultureInfo.InvariantCulture),
                UpdatedBy   = reader.IsDBNull(12) ? null : reader.GetString(12)
            };

            list.Add(card);
        }

        return list;
    }

    public Card? GetById(long cardId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT card_id,
                   card_name,
                   card_company,
                   last4_digits,
                   owner_name,
                   owner_type,
                   billing_day,
                   status,
                   memo,
                   created_at,
                   created_by,
                   updated_at,
                   updated_by
            FROM Card
            WHERE card_id = $id;
        ";
        cmd.Parameters.AddWithValue("$id", cardId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new Card
        {
            CardId      = reader.GetInt64(0),
            CardName    = reader.GetString(1),
            CardCompany = reader.IsDBNull(2) ? null : reader.GetString(2),
            Last4Digits = reader.IsDBNull(3) ? null : reader.GetString(3),
            OwnerName   = reader.IsDBNull(4) ? null : reader.GetString(4),
            OwnerType   = reader.IsDBNull(5) ? null : reader.GetString(5),
            BillingDay  = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            Status      = reader.GetString(7),
            Memo        = reader.IsDBNull(8) ? null : reader.GetString(8),
            CreatedAt   = DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
            CreatedBy   = reader.GetString(10),
            UpdatedAt   = reader.IsDBNull(11) ? null : DateTime.Parse(reader.GetString(11), CultureInfo.InvariantCulture),
            UpdatedBy   = reader.IsDBNull(12) ? null : reader.GetString(12)
        };
    }

    public long Insert(Card card, string userName)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Card (
                card_name,
                card_company,
                last4_digits,
                owner_name,
                owner_type,
                billing_day,
                status,
                memo,
                created_at,
                created_by,
                updated_by
            )
            VALUES (
                $name,
                $company,
                $last4,
                $owner,
                $ownerType,
                $billingDay,
                $status,
                $memo,
                datetime('now'),
                $user,
                $user
            );
            SELECT last_insert_rowid();
        ";

        cmd.Parameters.AddWithValue("$name", card.CardName);
        cmd.Parameters.AddWithValue("$company", (object?)card.CardCompany ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$last4", (object?)card.Last4Digits ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$owner", (object?)card.OwnerName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ownerType", (object?)card.OwnerType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$billingDay", (object?)card.BillingDay ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$status", card.Status);
        cmd.Parameters.AddWithValue("$memo", (object?)card.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user", userName);

        var result = cmd.ExecuteScalar();
        return Convert.ToInt64(result);
    }

    public void Update(Card card, string userName)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Card
            SET card_name    = $name,
                card_company = $company,
                last4_digits = $last4,
                owner_name   = $owner,
                owner_type   = $ownerType,
                billing_day  = $billingDay,
                status       = $status,
                memo         = $memo,
                updated_at   = datetime('now'),
                updated_by   = $user
            WHERE card_id = $id;
        ";

        cmd.Parameters.AddWithValue("$id", card.CardId);
        cmd.Parameters.AddWithValue("$name", card.CardName);
        cmd.Parameters.AddWithValue("$company", (object?)card.CardCompany ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$last4", (object?)card.Last4Digits ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$owner", (object?)card.OwnerName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ownerType", (object?)card.OwnerType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$billingDay", (object?)card.BillingDay ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$status", card.Status);
        cmd.Parameters.AddWithValue("$memo", (object?)card.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user", userName);

        cmd.ExecuteNonQuery();
    }

    public void UpdateStatus(long cardId, string newStatus, string userName)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Card
            SET status     = $status,
                updated_at = datetime('now'),
                updated_by = $user
            WHERE card_id = $id;
        ";
        cmd.Parameters.AddWithValue("$status", newStatus);
        cmd.Parameters.AddWithValue("$user", userName);
        cmd.Parameters.AddWithValue("$id", cardId);
        cmd.ExecuteNonQuery();
    }
}
