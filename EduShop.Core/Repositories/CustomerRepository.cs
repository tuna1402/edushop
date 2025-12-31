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
       school_name,
       contact_name,
       phone1,
       phone2,
       email1,
       email2,
       address,
       memo,
       is_deleted,
       created_at,
       created_by,
       updated_at,
       updated_by
FROM   Customer
WHERE  is_deleted = 0
ORDER BY school_name ASC, customer_id ASC;
";

        using var reader = cmd.ExecuteReader();
        var list = new List<Customer>();

        while (reader.Read())
        {
            var c = new Customer
            {
                CustomerId   = reader.GetInt64(0),
                SchoolName   = reader.GetString(1),
                ContactName  = reader.IsDBNull(2) ? null : reader.GetString(2),
                Phone1       = reader.IsDBNull(3) ? null : reader.GetString(3),
                Phone2       = reader.IsDBNull(4) ? null : reader.GetString(4),
                Email1       = reader.IsDBNull(5) ? null : reader.GetString(5),
                Email2       = reader.IsDBNull(6) ? null : reader.GetString(6),
                Address      = reader.IsDBNull(7) ? null : reader.GetString(7),
                Memo         = reader.IsDBNull(8) ? null : reader.GetString(8),
                IsDeleted    = reader.GetInt32(9) != 0,
                CreatedAt    = DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
                CreatedBy    = reader.IsDBNull(11) ? null : reader.GetString(11),
                UpdatedAt    = reader.IsDBNull(12)
                               ? null
                               : DateTime.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
                UpdatedBy    = reader.IsDBNull(13) ? null : reader.GetString(13)
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
       school_name,
       contact_name,
       phone1,
       phone2,
       email1,
       email2,
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
            SchoolName   = reader.GetString(1),
            ContactName  = reader.IsDBNull(2) ? null : reader.GetString(2),
            Phone1       = reader.IsDBNull(3) ? null : reader.GetString(3),
            Phone2       = reader.IsDBNull(4) ? null : reader.GetString(4),
            Email1       = reader.IsDBNull(5) ? null : reader.GetString(5),
            Email2       = reader.IsDBNull(6) ? null : reader.GetString(6),
            Address      = reader.IsDBNull(7) ? null : reader.GetString(7),
            Memo         = reader.IsDBNull(8) ? null : reader.GetString(8),
            IsDeleted    = reader.GetInt32(9) != 0,
            CreatedAt    = DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
            CreatedBy    = reader.IsDBNull(11) ? null : reader.GetString(11),
            UpdatedAt    = reader.IsDBNull(12)
                           ? null
                           : DateTime.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
            UpdatedBy    = reader.IsDBNull(13) ? null : reader.GetString(13)
        };
    }

    public long Insert(Customer c, string userName)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Customer
    (school_name,
     contact_name,
     phone1,
     phone2,
     email1,
     email2,
     address,
     memo,
     is_deleted,
     created_at,
     created_by)
VALUES
    ($name,
     $contact,
     $phone1,
     $phone2,
     $email1,
     $email2,
     $address,
     $memo,
     0,
     datetime('now'),
     $user);

SELECT last_insert_rowid();
";

        cmd.Parameters.AddWithValue("$name",    c.SchoolName);
        cmd.Parameters.AddWithValue("$contact", (object?)c.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$phone1",  (object?)c.Phone1      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$phone2",  (object?)c.Phone2      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$email1",  (object?)c.Email1      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$email2",  (object?)c.Email2      ?? DBNull.Value);
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
SET school_name = $name,
    contact_name  = $contact,
    phone1        = $phone1,
    phone2        = $phone2,
    email1        = $email1,
    email2        = $email2,
    address       = $address,
    memo          = $memo,
    updated_at    = datetime('now'),
    updated_by    = $user
WHERE customer_id = $id;
";

        cmd.Parameters.AddWithValue("$id",      c.CustomerId);
        cmd.Parameters.AddWithValue("$name",    c.SchoolName);
        cmd.Parameters.AddWithValue("$contact", (object?)c.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$phone1",  (object?)c.Phone1      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$phone2",  (object?)c.Phone2      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$email1",  (object?)c.Email1      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$email2",  (object?)c.Email2      ?? DBNull.Value);
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
