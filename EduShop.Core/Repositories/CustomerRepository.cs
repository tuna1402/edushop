using System.Globalization;
using Microsoft.Data.Sqlite;
using EduShop.Core.Models;

namespace EduShop.Core.Repositories;

public class CustomerRepository
{
    private readonly string _connectionString;

    public CustomerRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public List<Customer> GetAll()
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
SELECT customer_id,
       customer_name,
       contact_name,
       phone,
       email,
       address,
       memo,
       is_deleted,
       created_at,
       created_by,
       updated_at,
       updated_by
FROM   Customer
WHERE  is_deleted = 0
ORDER BY customer_name ASC, customer_id ASC;
";

        using var reader = cmd.ExecuteReader();
        var list = new List<Customer>();

        while (reader.Read())
        {
            var c = new Customer
            {
                CustomerId   = reader.GetInt64(0),
                CustomerName = reader.GetString(1),
                ContactName  = reader.IsDBNull(2) ? null : reader.GetString(2),
                Phone        = reader.IsDBNull(3) ? null : reader.GetString(3),
                Email        = reader.IsDBNull(4) ? null : reader.GetString(4),
                Address      = reader.IsDBNull(5) ? null : reader.GetString(5),
                Memo         = reader.IsDBNull(6) ? null : reader.GetString(6),
                IsDeleted    = reader.GetInt32(7) != 0,
                CreatedAt    = DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
                CreatedBy    = reader.IsDBNull(9) ? null : reader.GetString(9),
                UpdatedAt    = reader.IsDBNull(10)
                               ? null
                               : DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
                UpdatedBy    = reader.IsDBNull(11) ? null : reader.GetString(11)
            };

            list.Add(c);
        }

        return list;
    }

    public Customer? GetById(long id)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
SELECT customer_id,
       customer_name,
       contact_name,
       phone,
       email,
       address,
       memo,
       is_deleted,
       created_at,
       created_by,
       updated_at,
       updated_by
FROM   Customer
WHERE  customer_id = $id;
";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new Customer
        {
            CustomerId   = reader.GetInt64(0),
            CustomerName = reader.GetString(1),
            ContactName  = reader.IsDBNull(2) ? null : reader.GetString(2),
            Phone        = reader.IsDBNull(3) ? null : reader.GetString(3),
            Email        = reader.IsDBNull(4) ? null : reader.GetString(4),
            Address      = reader.IsDBNull(5) ? null : reader.GetString(5),
            Memo         = reader.IsDBNull(6) ? null : reader.GetString(6),
            IsDeleted    = reader.GetInt32(7) != 0,
            CreatedAt    = DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
            CreatedBy    = reader.IsDBNull(9) ? null : reader.GetString(9),
            UpdatedAt    = reader.IsDBNull(10)
                           ? null
                           : DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
            UpdatedBy    = reader.IsDBNull(11) ? null : reader.GetString(11)
        };
    }

    public long Insert(Customer c, string userName)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Customer
    (customer_name,
     contact_name,
     phone,
     email,
     address,
     memo,
     is_deleted,
     created_at,
     created_by)
VALUES
    ($name,
     $contact,
     $phone,
     $email,
     $address,
     $memo,
     0,
     datetime('now'),
     $user);

SELECT last_insert_rowid();
";

        cmd.Parameters.AddWithValue("$name",    c.CustomerName);
        cmd.Parameters.AddWithValue("$contact", (object?)c.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$phone",   (object?)c.Phone       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$email",   (object?)c.Email       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$address", (object?)c.Address     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$memo",    (object?)c.Memo        ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user",    userName);

        var idObj = cmd.ExecuteScalar();
        return Convert.ToInt64(idObj);
    }

    public void Update(Customer c, string userName)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE Customer
SET customer_name = $name,
    contact_name  = $contact,
    phone         = $phone,
    email         = $email,
    address       = $address,
    memo          = $memo,
    updated_at    = datetime('now'),
    updated_by    = $user
WHERE customer_id = $id;
";

        cmd.Parameters.AddWithValue("$id",      c.CustomerId);
        cmd.Parameters.AddWithValue("$name",    c.CustomerName);
        cmd.Parameters.AddWithValue("$contact", (object?)c.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$phone",   (object?)c.Phone       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$email",   (object?)c.Email       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$address", (object?)c.Address     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$memo",    (object?)c.Memo        ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user",    userName);

        cmd.ExecuteNonQuery();
    }

    public void SoftDelete(long id, string userName)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE Customer
SET is_deleted = 1,
    updated_at = datetime('now'),
    updated_by = $user
WHERE customer_id = $id;
";
        cmd.Parameters.AddWithValue("$id",   id);
        cmd.Parameters.AddWithValue("$user", userName);
        cmd.ExecuteNonQuery();
    }
}
