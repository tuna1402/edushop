using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using EduShop.Core.Common;
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
            var appSettings = SettingsStorage.Load();
            QuestPDF.Settings.License = LicenseType.Community;

            // 1) Windows 로컬 경로: %LOCALAPPDATA%\EduShop\edushop.db
            var dbPath = AppPaths.GetDefaultDbPath();

            if (!File.Exists(dbPath))
            {
                string? legacyPath = null;
                var baseLegacyPath = Path.Combine(AppContext.BaseDirectory, "edushop.db");
                if (File.Exists(baseLegacyPath))
                {
                    legacyPath = baseLegacyPath;
                }
                else
                {
                    var currentLegacyPath = Path.Combine(Directory.GetCurrentDirectory(), "edushop.db");
                    if (File.Exists(currentLegacyPath))
                    {
                        legacyPath = currentLegacyPath;
                    }
                }

                if (legacyPath is not null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                    File.Copy(legacyPath, dbPath);
                }
            }

            Debug.WriteLine($"EduShop DB Path: {dbPath}");
            var connectionString = $"Data Source={dbPath}";

            // 2) DB 없으면 테이블 생성
            DatabaseInitializer.EnsureCreated(connectionString);

            // 3) 서비스 구성
            var productRepo     = new ProductRepository(connectionString);
            var logRepo         = new AuditLogRepository(connectionString);
            var productService  = new ProductService(productRepo, logRepo);
            var cardRepo        = new CardRepository(connectionString);
            var accountRepo      = new AccountRepository(connectionString);
            var accountLogRepo   = new AccountUsageLogRepository(connectionString);
            var customerRepo   = new CustomerRepository(connectionString);

            // 4) 매출 리포지토리/서비스 추가
            var salesRepo    = new SalesRepository(connectionString);
            var salesService = new SalesService(salesRepo);
            var accountService   = new AccountService(accountRepo, accountLogRepo);
            var customerService = new CustomerService(customerRepo);
            var cardService     = new CardService(cardRepo, logRepo);

            var currentUser = new UserContext { UserId = "admin", UserName = "관리자" };

            Application.Run(new MainForm(
                productService,
                salesService,
                accountService,
                customerService,
                cardService,
                logRepo,
                accountLogRepo,
                currentUser,
                appSettings));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "EduShop.WinForms 에러",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
