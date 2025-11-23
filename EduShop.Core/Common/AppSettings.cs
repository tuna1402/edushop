namespace EduShop.Core.Common;

public class AppSettings
{
    public int    ExpiringDays        { get; set; } = 30;
    public string DefaultExportFolder { get; set; } = "";

    public string CompanyName    { get; set; } = "";
    public string CompanyContact { get; set; } = "";
    public string CompanyPhone   { get; set; } = "";
    public string CompanyEmail   { get; set; } = "";
    public string CompanyAddress { get; set; } = "";
}
