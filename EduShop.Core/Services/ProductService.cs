using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Repositories;

namespace EduShop.Core.Services;

public class ProductService
{
    private readonly ProductRepository _productRepo;
    private readonly AuditLogRepository _logRepo;

    public ProductService(ProductRepository productRepo, AuditLogRepository logRepo)
    {
        _productRepo = productRepo;
        _logRepo = logRepo;
    }

    public List<Product> GetAll() => _productRepo.GetAll();

    public long Create(Product product, UserContext user)
    {
        var newId = _productRepo.Insert(product, user.UserName);

        _logRepo.Insert(new AuditLogEntry
        {
            UserId      = user.UserId,
            UserName    = user.UserName,
            ActionType  = "PRODUCT_CREATE",
            TableName   = "Product",
            TargetId    = newId,
            TargetCode  = product.ProductCode,
            Description = $"상품 등록 - [{product.ProductCode}] {product.ProductName}"
        });

        return newId;
    }

    public void ChangeStatus(long productId, string newStatus, UserContext user)
    {
        var existing = _productRepo.GetById(productId);
        if (existing == null) return;

        if (existing.Status == newStatus) return;

        _productRepo.UpdateStatus(productId, newStatus, user.UserName);

        _logRepo.Insert(new AuditLogEntry
        {
            UserId      = user.UserId,
            UserName    = user.UserName,
            ActionType  = "PRODUCT_STATUS_CHANGE",
            TableName   = "Product",
            TargetId    = productId,
            TargetCode  = existing.ProductCode,
            Description = $"상태 변경 - {existing.Status} → {newStatus}"
        });
    }

    public void Update(Product product, UserContext user)
    {
        var existing = _productRepo.GetById(product.ProductId);
        if (existing == null)
            throw new InvalidOperationException($"상품(ID={product.ProductId})을(를) 찾을 수 없습니다.");

        _productRepo.Update(product, user.UserName);

        _logRepo.Insert(new AuditLogEntry
        {
            UserId      = user.UserId,
            UserName    = user.UserName,
            ActionType  = "PRODUCT_UPDATE",
            TableName   = "Product",
            TargetId    = product.ProductId,
            TargetCode  = product.ProductCode,
            Description = $"상품 수정 - [{product.ProductCode}] {product.ProductName}"
        });
    }

    public List<AuditLogEntry> GetLogsForProduct(long productId)
    {
        return _logRepo.GetForProduct(productId);
    }
}
