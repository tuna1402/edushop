using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Repositories;
using EduShop.Core.Services;
using EduShop.Core.Infrastructure;

var dbPath = AppPaths.GetDefaultDbPath();
var connectionString = $"Data Source={dbPath}";

DatabaseInitializer.EnsureCreated(connectionString);

var productRepo = new ProductRepository(connectionString);
var logRepo     = new AuditLogRepository(connectionString);
var service     = new ProductService(productRepo, logRepo);
var user        = new UserContext { UserId = "admin", UserName = "사장" };

Console.WriteLine("=== 1단계 기능 테스트 ===");

// 1) 신규 상품 하나 등록
var newProduct = new Product
{
    ProductCode     = "P0001",
    ProductName     = "에듀테크 플랫폼 Basic",
    PlanName        = "Basic",
    MonthlyFeeUsd   = 10.0,
    MonthlyFeeKrw   = 13000,
    WholesalePrice  = 50000,
    RetailPrice     = 70000,
    PurchasePrice   = 40000,
    YearlyAvailable = true,
    MinMonth        = 1,
    MaxMonth        = 12,
    Status          = "ACTIVE",
    Remark          = "테스트 상품"
};

var newId = service.Create(newProduct, user);
Console.WriteLine($"신규 상품 등록 완료. product_id = {newId}");

// 2) 전체 상품 목록 조회
Console.WriteLine();
Console.WriteLine("현재 상품 목록:");
var products = service.GetAll();
foreach (var p in products)
{
    Console.WriteLine($"[{p.ProductId}] {p.ProductCode} / {p.ProductName} / 상태={p.Status}");
}

// 3) 상태 변경 테스트 (ACTIVE → INACTIVE)
Console.WriteLine();
Console.WriteLine($"상품 {newId} 상태를 INACTIVE로 변경합니다.");
service.ChangeStatus(newId, "INACTIVE", user);

// 4) 다시 목록 확인
Console.WriteLine();
Console.WriteLine("상태 변경 후 상품 목록:");
products = service.GetAll();
foreach (var p in products)
{
    Console.WriteLine($"[{p.ProductId}] {p.ProductCode} / {p.ProductName} / 상태={p.Status}");
}

// 5) 로그 조회
Console.WriteLine();
Console.WriteLine($"상품 {newId}에 대한 로그:");
var logs = service.GetLogsForProduct(newId);
foreach (var log in logs)
{
    Console.WriteLine($"{log.EventTime:yyyy-MM-dd HH:mm:ss} / {log.ActionType} / {log.Description}");
}

Console.WriteLine();
Console.WriteLine("테스트 종료.");
