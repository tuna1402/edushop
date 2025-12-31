namespace EduShop.Core.Models;

public class Customer
{
    public long     CustomerId   { get; set; }
    public string   SchoolName { get; set; } = "";
    public string?  ContactName  { get; set; }
    public string?  Phone1       { get; set; }
    public string?  Phone2       { get; set; }
    public string?  Email1       { get; set; }
    public string?  Email2       { get; set; }
    public string?  Address      { get; set; }
    public string?  Memo         { get; set; }

    public bool     IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public string?  CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string?   UpdatedBy { get; set; }
}
