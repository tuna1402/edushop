using EduShop.Core.Models;
using Supabase;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EduShop.Core.Repositories.Remote;

public class RemoteCustomerRepository
{
    private readonly Client _client;

    public RemoteCustomerRepository(Client client)
    {
        _client = client;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        var response = await _client
            .From<SupabaseCustomer>()
            .Filter("is_deleted", Operator.Equals, false)
            .Order("school_name", Ordering.Ascending)
            .Order("customer_id", Ordering.Ascending)
            .Get();

        return response.Models
            .Select(model => new Customer
            {
                CustomerId = model.CustomerId,
                SchoolName = model.SchoolName ?? "",
                ContactName = model.ContactName,
                Phone1 = model.Phone1,
                Phone2 = model.Phone2,
                Email1 = model.Email1,
                Email2 = model.Email2,
                Address = model.Address,
                Memo = model.Memo,
                IsDeleted = model.IsDeleted,
                CreatedAt = model.CreatedAt,
                CreatedBy = model.CreatedBy,
                UpdatedAt = model.UpdatedAt,
                UpdatedBy = model.UpdatedBy
            })
            .ToList();
    }

    [Table("customer")]
    private class SupabaseCustomer : BaseModel
    {
        [PrimaryKey("customer_id", false)]
        public long CustomerId { get; set; }

        [Column("school_name")]
        public string? SchoolName { get; set; }

        [Column("contact_name")]
        public string? ContactName { get; set; }

        [Column("phone1")]
        public string? Phone1 { get; set; }

        [Column("phone2")]
        public string? Phone2 { get; set; }

        [Column("email1")]
        public string? Email1 { get; set; }

        [Column("email2")]
        public string? Email2 { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("memo")]
        public string? Memo { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("created_by")]
        public string? CreatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("updated_by")]
        public string? UpdatedBy { get; set; }
    }
}
