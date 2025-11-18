using System;
using System.IO;
using System.Windows.Forms;
using EduShop.Core.Infrastructure;
using EduShop.Core.Repositories;
using EduShop.Core.Services;
using QuestPDF.Infrastructure;

namespace EduShop.WinForms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            QuestPDF.Settings.License = LicenseType.Community;

            // 1) Windows 로컬 경로: %LOCALAPPDATA%\EduShop\edushop_winforms.db
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbDir  = Path.Combine(localAppData, "EduShop");
            Directory.CreateDirectory(dbDir);

            var dbPath = Path.Combine(dbDir, "edushop_winforms.db");
            var connectionString = $"Data Source={dbPath}";

            // 2) DB 없으면 테이블 생성
            DatabaseInitializer.EnsureCreated(connectionString);

            // 3) 서비스 구성
            var productRepo     = new ProductRepository(connectionString);
            var logRepo         = new AuditLogRepository(connectionString);
            var productService  = new ProductService(productRepo, logRepo);

            // ★ 매출 리포지토리/서비스 추가
            var salesRepo    = new SalesRepository(connectionString);
            var salesService = new SalesService(salesRepo);

            Application.Run(new MainForm(productService, salesService));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "EduShop.WinForms 에러",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
